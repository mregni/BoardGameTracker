using System.Security.Claims;
using BoardGameTracker.Core.Auth.Interfaces;

namespace BoardGameTracker.Api.Infrastructure;

public static class ActiveUserTokenCheck
{
    public const string FailureMessage = "The account of this access token is disabled or no longer exists";

    public static async Task<bool> IsActiveAsync(ClaimsPrincipal? principal, IUserAccessCheck accessCheck, CancellationToken cancellationToken)
    {
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userId) && await accessCheck.CanSignInAsync(userId, cancellationToken);
    }
}
