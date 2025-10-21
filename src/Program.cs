using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UserManagementApi.Data;
using UserManagementApi.Repositories;
using UserManagementApi.Services;
using UserManagementApi.Models;
using UserManagementApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load external configuration sources (parent appsettings, user-secrets, optional Key Vault)
ConfigurationLoader.ConfigureExternalSources(builder.Configuration, builder.Environment);

// Register JwtSettingsService early so it can be injected
var jwtSettings = new JwtSettingsService(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<IJwtSettingsService>(jwtSettings);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-memory EF provider
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("UsersDb"));

builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// Add password hasher service
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// Configure authentication using the centralized JwtSettingsService
var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.JwtKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.Issuer),
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = !string.IsNullOrEmpty(jwtSettings.Audience),
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
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

                    var pd = new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = "Invalid or missing authentication token.",
                        Instance = context.Request?.Path
                    };

                    var json = JsonSerializer.Serialize(pd, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    return context.Response.WriteAsync(json);
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Use centralized exception handler early in the pipeline
app.UseGlobalExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Use HTTP activity logging middleware
app.UseRequestResponseLogging();

// Map controllers and require authentication by default
app.MapControllers().RequireAuthorization();

// Seed DB
await app.SeedDatabaseAsync();

app.Run();