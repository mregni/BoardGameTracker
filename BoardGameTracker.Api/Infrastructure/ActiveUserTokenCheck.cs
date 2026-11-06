using System.Security.Claims;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;

namespace BoardGameTracker.Api.Infrastructure;

public static class ActiveUserTokenCheck
{
    public const string FailureMessage = "The account of this access token is disabled or no longer exists, or its session has ended";

    public static async Task<bool> IsActiveAsync(ClaimsPrincipal? principal, IUserAccessCheck accessCheck, CancellationToken cancellationToken)
    {
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !await accessCheck.CanSignInAsync(userId, cancellationToken))
        {
            return false;
        }

        var sessionId = principal?.FindFirstValue(TokenService.SessionIdClaim);
        return sessionId == null || (Guid.TryParse(sessionId, out var id) && await accessCheck.IsSessionActiveAsync(id, cancellationToken));
    }
}
