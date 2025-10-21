using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace UserManagementApi.Services;

public interface IJwtSettingsService
{
    string JwtKey { get; }
    string Issuer { get; }
    string Audience { get; }
    bool IsUsingEnvironmentSecret { get; }
}
public class JwtSettingsService : IJwtSettingsService
{
    public string JwtKey { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public bool IsUsingEnvironmentSecret { get; }

    public JwtSettingsService(IConfiguration configuration, IHostEnvironment env)
    {
        // Priority: environment variable -> configuration providers (user secrets / key vault / appsettings)
        var envKey = Environment.GetEnvironmentVariable("JWT_SECRET")
                     ?? Environment.GetEnvironmentVariable("ASPNETCORE_Configuration__Jwt__Key");

        var jwtKeyFromConfig = configuration["Jwt:Key"];

        if (!string.IsNullOrWhiteSpace(envKey))
        {
            JwtKey = envKey;
            IsUsingEnvironmentSecret = true;
        }
        else if (!string.IsNullOrWhiteSpace(jwtKeyFromConfig))
        {
            JwtKey = jwtKeyFromConfig;
            IsUsingEnvironmentSecret = false;
        }
        else
        {
            // DEV fallback only
            JwtKey = "ReplaceWithStrongKey";
            IsUsingEnvironmentSecret = false;
        }

        Issuer = configuration["Jwt:Issuer"];
        Audience = configuration["Jwt:Audience"];
    }
}