using BoardGameTracker.Common.Entities.Auth;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IOidcRoleReconciler
{
    Task AssignInitialRoleAsync(ApplicationUser user, OidcProvider provider, OidcClaims claims);
    Task ReconcileAsync(ApplicationUser user, OidcProvider provider, OidcClaims claims);
}
