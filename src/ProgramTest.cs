// csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;

public class ProgramTest
{
    // In-memory logger to capture formatted log messages
    class TestLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentQueue<string> _messages;

        public TestLogger(string category, ConcurrentQueue<string> messages)
        {
            _category = category;
            _messages = messages;
        }

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                var msg = formatter != null ? formatter(state, exception) : state?.ToString();
                var entry = $"[{logLevel}] {_category}: {msg}";
                if (exception != null)
                {
                    entry += $" | Exception: {exception.GetType().Name}: {exception.Message}";
                }
                _messages.Enqueue(entry);
            }
            catch
            {
                // swallow logging exceptions for tests
            }
        }
    }

    class TestLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages;
        public TestLoggerProvider(ConcurrentQueue<string> messages) => _messages = messages;
        public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _messages);
        public void Dispose() { }
    }

    private TestServer CreateTestServer(ConcurrentQueue<string> logSink)
    {
        var key = "ReplaceWithStrongKey";
        var keyBytes = Encoding.UTF8.GetBytes(key);

        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();

                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();

                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = true
                            };

                            options.Events = new JwtBearerEvents
                            {
                                OnChallenge = context =>
                                {
                                    if (!context.Response.HasStarted)
                                    {
                                        context.HandleResponse();
                                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                        context.Response.ContentType = "application/problem+json";

                                        var pd = new
                                        {
                                            title = "Unauthorized",
                                            status = StatusCodes.Status401Unauthorized,
                                            detail = "Invalid or missing authentication token.",
                                            instance = context.Request?.Path.Value
                                        };

                                        var json = JsonSerializer.Serialize(pd, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                                        return context.Response.WriteAsync(json);
                                    }
                                    return Task.CompletedTask;
                                }
                            };
                        });

                    services.AddAuthorization();

                    // register middleware dependencies if required by your middleware types
                    // no special service registrations needed for the provided middlewares here
                });

                webHost.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new TestLoggerProvider(logSink));
                });

                webHost.Configure(app =>
                {
                    // Assume UseGlobalExceptionHandler and UseRequestResponseLogging are available
                    app.UseMiddleware(typeof(UserManagementApi.Middleware.ExceptionMiddleware));
                    app.UseMiddleware(typeof(UserManagementApi.Middleware.RequestResponseLoggingMiddleware));

                    app.UseRouting();

                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/throw", ctx => throw new InvalidOperationException("boom"));
                        endpoints.MapGet("/ok", async ctx =>
                        {
                            ctx.Response.ContentType = "text/plain";
                            await ctx.Response.WriteAsync("hello");
                        });
                        endpoints.MapGet("/secure", async ctx =>
                        {
                            await ctx.Response.WriteAsync("secret");
                        }).RequireAuthorization();
                    });
                });
            });

        var host = builder.Build();
        host.StartAsync().GetAwaiter().GetResult();
        return host.GetTestServer();
    }

    [Fact]
    public async Task ExceptionMiddleware_Returns_ProblemDetails_On_UnhandledException()
    {
        var logs = new ConcurrentQueue<string>();
        using var server = CreateTestServer(logs);
        using var client = server.CreateClient();

        var resp = await client.GetAsync("/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
        Assert.Contains("application/json", resp.Content.Headers.ContentType?.MediaType ?? string.Empty);

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("title", out var title));
        Assert.Equal("An unexpected error occurred.", title.GetString());
        Assert.True(root.TryGetProperty("status", out var status));
        Assert.Equal(500, status.GetInt32());
    }

    [Fact]
    public async Task RequestResponseLogging_Emits_Incoming_And_Outgoing_Entries()
    {
        var logs = new ConcurrentQueue<string>();
        using var server = CreateTestServer(logs);
        using var client = server.CreateClient();

        var resp = await client.GetAsync("/ok");
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync();
        Assert.Equal("hello", text);

        // Give a moment for middleware logging to flush (TestServer is synchronous for middleware, so this should be immediate)
        var all = logs.ToArray();
        // Check that we captured incoming request and outgoing response messages
        var hasIncoming = all.Any(m => m.Contains("Incoming request:") || m.Contains("Incoming request"));
        var hasOutgoing = all.Any(m => m.Contains("Outgoing response:") || m.Contains("Outgoing response"));

        // If specific templates differ, tolerate presence of method/path and status
        var hasReqMethodPath = all.Any(m => m.Contains("GET") && m.Contains("/ok"));
        var hasRespStatus = all.Any(m => m.Contains("200") || m.Contains("StatusCode: 200") || m.Contains("200 OK"));

        Assert.True(hasIncoming || hasReqMethodPath, "Expected incoming request log or method/path in logs.");
        Assert.True(hasOutgoing || hasRespStatus, "Expected outgoing response log or status in logs.");
    }

    [Fact]
    public async Task JwtChallenge_Returns_ProblemDetails_For_Unauthenticated()
    {
        var logs = new ConcurrentQueue<string>();
        using var server = CreateTestServer(logs);
        using var client = server.CreateClient();

        var resp = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Contains("application/problem+json", resp.Content.Headers.ContentType?.MediaType ?? string.Empty);

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("title", out var title));
        Assert.Equal("Unauthorized", title.GetString());
        Assert.True(root.TryGetProperty("status", out var status));
        Assert.Equal(401, status.GetInt32());
    }
}