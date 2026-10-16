using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public static class DbSeeder
{
    private const string AdminUsername = "admin";

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

    public static async Task<IReadOnlyList<string>> GetAdminPasswordErrorsAsync(
        UserManager<ApplicationUser> userManager,
        string? adminPassword)
    {
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            return [];
        }

        var probe = new ApplicationUser(AdminUsername, null, "Administrator");
        var errors = new List<string>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, probe, adminPassword);
            errors.AddRange(result.Errors.Select(e => e.Description));
        }

        return errors;
    }

    private static async Task SeedDefaultAdmin(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string? adminPassword)
    {
        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var passwordErrors = await GetAdminPasswordErrorsAsync(userManager, adminPassword);
        if (passwordErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"ADMIN_PASSWORD does not meet the password rules: {string.Join(" ", passwordErrors)} Choose a longer password or unset ADMIN_PASSWORD to start with admin/admin.");
        }

        const string defaultPassword = "admin";
        var useDefault = string.IsNullOrWhiteSpace(adminPassword);
        var password = useDefault ? defaultPassword : adminPassword!;

        var admin = new ApplicationUser(AdminUsername, null, "Administrator");
        IdentityResult result;
        if (useDefault)
        {
            admin.PasswordHash = userManager.PasswordHasher.HashPassword(admin, password);
            result = await userManager.CreateAsync(admin);
        }
        else
        {
            result = await userManager.CreateAsync(admin, password);
        }

        if (!result.Succeeded)
        {
            logger.LogError("Failed to create default admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Constants.AuthRoles.Admin);

        if (useDefault)
        {
            logger.LogWarning(
                "Created default admin user '{Username}' with the default password. Change it after the first login or set ADMIN_PASSWORD",
                AdminUsername);
        }
        else
        {
            logger.LogInformation("Created default admin user '{Username}' using ADMIN_PASSWORD", AdminUsername);
        }
    }
}
