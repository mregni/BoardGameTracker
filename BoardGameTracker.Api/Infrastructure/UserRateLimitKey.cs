using System.Security.Claims;
using BoardGameTracker.Core.Common;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Api.Infrastructure;

public static class UserRateLimitKey
{
    public static string From(HttpContext context)
    {
        var identity = context.User.Identity;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (identity is { IsAuthenticated: true } && identity.AuthenticationType != AuthDisabledMiddleware.AuthenticationType && userId != null)
        {
            return $"user:{userId}";
        }

        return ClientAddressKey.From(context.Connection.RemoteIpAddress);
    }
}
