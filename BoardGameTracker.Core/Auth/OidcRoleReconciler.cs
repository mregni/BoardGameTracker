using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class OidcRoleReconciler : IOidcRoleReconciler
{
    public static readonly IReadOnlySet<string> DefaultRoles = new HashSet<string> { Constants.AuthRoles.User, Constants.AuthRoles.Reader };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OidcRoleReconciler> _logger;

    public OidcRoleReconciler(UserManager<ApplicationUser> userManager, ILogger<OidcRoleReconciler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task AssignInitialRoleAsync(ApplicationUser user, OidcProvider provider, OidcClaims claims)
    {
        var role = TargetRole(provider, claims);
        await _userManager.AddToRoleAsync(user, role);
        user.ManageRoleByIdentityProvider();
        _logger.LogInformation("Assigned role {Role} to new OIDC user {UserId}", role, user.Id);
    }

    public async Task ReconcileAsync(ApplicationUser user, OidcProvider provider, OidcClaims claims)
    {
        if (!user.RoleManagedByIdentityProvider)
        {
            _logger.LogDebug("The role of user {UserId} is managed locally; skipping OIDC role reconciliation", user.Id);
            return;
        }

        var target = TargetRole(provider, claims);
        await AdminRoleGate.Lock.WaitAsync();
        try
        {
            var current = await _userManager.GetRolesAsync(user);
            if (current.Count == 1 && current[0] == target)
            {
                return;
            }

            if (current.Contains(Constants.AuthRoles.Admin) && target != Constants.AuthRoles.Admin && !await HasAnotherActiveAdminAsync(user))
            {
                _logger.LogWarning("The identity provider no longer grants user {UserId} the Admin role, but they are the last admin; keeping the role", user.Id);
                return;
            }

            if (!current.Contains(target))
            {
                await _userManager.AddToRoleAsync(user, target);
            }

            var obsolete = current.Where(r => r != target).ToList();
            if (obsolete.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, obsolete);
            }

            _logger.LogInformation("Reconciled the role of OIDC user {UserId} from {Previous} to {Role}", user.Id, string.Join(",", current), target);
        }
        finally
        {
            AdminRoleGate.Lock.Release();
        }
    }

    public static string TargetRole(OidcProvider provider, OidcClaims claims)
    {
        var adminGroup = OidcClaimTypes.AdminGroup(provider);
        if (claims.GetValues(OidcClaimTypes.Roles(provider)).Any(g => string.Equals(g, adminGroup, StringComparison.OrdinalIgnoreCase)))
        {
            return Constants.AuthRoles.Admin;
        }

        return DefaultRoles.Contains(provider.DefaultRole) ? provider.DefaultRole : Constants.AuthRoles.User;
    }

    private async Task<bool> HasAnotherActiveAdminAsync(ApplicationUser user)
    {
        var admins = await _userManager.GetUsersInRoleAsync(Constants.AuthRoles.Admin);
        return admins.Any(a => a.Id != user.Id && !a.IsDisabled);
    }
}
