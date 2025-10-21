using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UserManagementApi.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private const int BodyReadLimit = 4096; // bytes to log

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;

        // Copy headers but redact sensitive ones
        var headers = context.Request.Headers
            .Where(h => !string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(k => k.Key, v => v.Value.ToString());

        _logger.LogInformation("Incoming request {Method} {Path} Headers={Headers}", method, path, headers);

        await _next(context);

        var statusCode = context.Response?.StatusCode;
        _logger.LogInformation("Outgoing response {Method} {Path} {StatusCode}", method, path, statusCode);
    }
}

public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder) =>
        builder.UseMiddleware<RequestResponseLoggingMiddleware>();
}
    