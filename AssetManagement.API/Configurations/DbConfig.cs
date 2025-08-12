using AssetManagement.API.Data;
using AssetManagement.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssetManagement.API.Configurations;

public static class DbConfig
{
    public static async Task InitializeDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var passwordService = services.GetRequiredService<IPasswordService>();
            if (!await context.Users.AnyAsync())
            {
                await DbSeeder.SeedData(context, passwordService);
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }
}