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
        ILogger logger,
        string? adminPassword = null)
    {
        await SeedRoles(roleManager, logger);
        await SeedDefaultAdmin(userManager, logger, adminPassword);
    }

    private static async Task SeedRoles(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = [Constants.AuthRoles.Admin, Constants.AuthRoles.User, Constants.AuthRoles.Reader];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task SeedDefaultAdmin(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string? adminPassword)
    {
        const string adminUsername = "admin";
        var existingAdmin = await userManager.FindByNameAsync(adminUsername);
        if (existingAdmin != null)
        {
            return;
        }

        const string defaultPassword = "admin";
        var useDefault = string.IsNullOrWhiteSpace(adminPassword);
        var password = useDefault ? defaultPassword : adminPassword!;

        var admin = new ApplicationUser(adminUsername, null, "Administrator");
        admin.PasswordHash = userManager.PasswordHasher.HashPassword(admin, password);
        var result = await userManager.CreateAsync(admin);

        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to create default admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Constants.AuthRoles.Admin);

        if (!useDefault)
        {
            logger.LogInformation("Created default admin user '{Username}' using ADMIN_PASSWORD", adminUsername);
        }
    }
}
