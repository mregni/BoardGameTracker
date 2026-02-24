using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public static class DbSeeder
{
    public static async Task SeedAuthData(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        await SeedRoles(roleManager, logger);
        await SeedDefaultAdmin(userManager, logger);
    }

    private static async Task SeedRoles(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = [Constants.AuthRoles.Admin, Constants.AuthRoles.Reader];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task SeedDefaultAdmin(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminUsername = "admin";
        var existingAdmin = await userManager.FindByNameAsync(adminUsername);
        if (existingAdmin != null)
        {
            return;
        }

        var admin = new ApplicationUser(adminUsername, "admin@boardgametracker.local", "Administrator");
        var result = await userManager.CreateAsync(admin, "admin");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Constants.AuthRoles.Admin);
            logger.LogInformation("Created default admin user (username: admin, password: admin)");
        }
        else
        {
            logger.LogWarning("Failed to create default admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
