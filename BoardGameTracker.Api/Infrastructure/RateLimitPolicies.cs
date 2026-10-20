using System.Threading.RateLimiting;
using BoardGameTracker.Core.Common;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Api.Infrastructure;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Oidc = "oidc";

    public const int AuthPermitsPerMinute = 10;
    public const int OidcPermitsPerMinute = 60;

    public static RateLimitPartition<string> AuthPartition(HttpContext context) =>
        PerClientAddress(context, AuthPermitsPerMinute);

    public static RateLimitPartition<string> OidcPartition(HttpContext context) =>
        PerClientAddress(context, OidcPermitsPerMinute);

    private static RateLimitPartition<string> PerClientAddress(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientAddressKey.From(context.Connection.RemoteIpAddress),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
}
