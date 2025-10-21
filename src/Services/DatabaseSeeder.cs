using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using UserManagementApi.Data;
using UserManagementApi.Models;

namespace UserManagementApi.Services;

public static class DatabaseSeeder
{
    /// <summary>
    /// Extension to run DB seeding once at startup. Uses IPasswordHasher from DI to hash passwords.
    /// Call: await app.SeedDatabaseAsync();
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var db = provider.GetRequiredService<AppDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();

        try
        {
            // Use async check to avoid blocking
            if (!await db.Users.AnyAsync())
            {
                db.Users.AddRange(new[]
                {
                        new User
                        {
                            FirstName = "Alice",
                            LastName = "Johnson",
                            Email = "alice@example.com",
                            Password = hasher.Hash("Alice!23") // seeded password (change in prod)
                        },
                        new User
                        {
                            FirstName = "Bob",
                            LastName = "Smith",
                            Email = "bob@example.com",
                            Password = hasher.Hash("Bob!23")
                        }
                    });

                await db.SaveChangesAsync();
                logger.LogInformation("Database seeded with initial users.");
            }
            else
            {
                logger.LogDebug("Database already contains users; seeding skipped.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}

