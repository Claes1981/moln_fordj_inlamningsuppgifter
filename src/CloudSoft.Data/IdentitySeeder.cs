using CloudSoft.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudSoft.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Ensure roles exist
        foreach (var role in Constants.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                    logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Seed admin user from configuration
        var adminUsername = configuration["AdminSeed:Username"] ?? "admin";
        var adminPassword = configuration["AdminSeed:Password"] ?? "Admin123!";
        var adminEmail = configuration["AdminSeed:Email"] ?? "admin@cloudsoft.com";

        var admin = await userManager.FindByNameAsync(adminUsername);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Constants.AdministratorRole);
                logger.LogInformation("Seeded admin user: {Username}", adminUsername);
            }
            else
            {
                logger.LogError("Failed to seed admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
