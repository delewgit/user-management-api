using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using System;
using System.IO;

namespace UserManagementApi.Services;

public static class ConfigurationLoader
{
    /// <summary>
    /// Adds parent appsettings.json, user-secrets (DEV) and optional Azure Key Vault to the provided config builder.
    /// Call before you read Jwt:Key or other secrets.
    /// </summary>
    public static void ConfigureExternalSources(IConfigurationBuilder configBuilder, IHostEnvironment env)
    {
        // 1) Parent appsettings.json (one folder up)
        var parentAppSettings = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "appsettings.json"));
        if (File.Exists(parentAppSettings))
        {
            configBuilder.AddJsonFile(parentAppSettings, optional: true, reloadOnChange: true);
        }

        // 2) In development, add user secrets
        if (env.IsDevelopment())
        {
            try
            {
                configBuilder.AddUserSecrets(typeof(ConfigurationLoader).Assembly, optional: true);
            }
            catch
            {
                // ignore user secrets load failures (dev only)
            }
        }

        // 3) If a KeyVault name is present after loading the above, wire Key Vault
        try
        {
            var interim = configBuilder.Build();
            var keyVaultName = interim["KeyVault:Name"];
            if (!string.IsNullOrWhiteSpace(keyVaultName))
            {
                var kvUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
                configBuilder.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
            }
        }
        catch
        {
            // ignore KeyVault configuration failures here
        }
    }
}