using System.Threading.RateLimiting;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Common;

namespace BoardGameTracker.Host.Startup;

internal static class RateLimitingSetup
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.AddPolicy(RateLimitPolicies.Auth, RateLimitPolicies.AuthPartition);
            options.AddPolicy(RateLimitPolicies.Oidc, RateLimitPolicies.OidcPartition);
            options.AddPolicy("refresh", context => PerMinute(ClientAddressKey.From(context.Connection.RemoteIpAddress), 60));
            options.AddPolicy("changedetection", context => PerMinute(ClientAddressKey.From(context.Connection.RemoteIpAddress), 30));
            options.AddPolicy("integrations", context => PerMinute(ClientAddressKey.From(context.Connection.RemoteIpAddress), 20));
            options.AddPolicy("gamenight-link", context => PerMinute(ClientAddressKey.From(context.Connection.RemoteIpAddress), 30));
            options.AddPolicy("rag", context => PerMinute(UserRateLimitKey.From(context), 10));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var request = context.HttpContext.Request;
                if (HttpMethods.IsGet(request.Method) && request.Path.StartsWithSegments("/api/auth/oidc"))
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status302Found;
                    context.HttpContext.Response.Headers.Location =
                        $"{OidcController.CallbackPage}?error={Uri.EscapeDataString(BoardGameTracker.Common.Constants.Errors.TooManyRequests)}";
                }

                return ValueTask.CompletedTask;
            };
        });
    }

    private static RateLimitPartition<string> PerMinute(string key, int permitLimit)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    }
}
