using Microsoft.AspNetCore.Builder;

namespace BoardGameTracker.Api.Infrastructure;

public static class AuthBypassExtensions
{
    public static IApplicationBuilder UseAuthBypassIfEnabled(this IApplicationBuilder app)
    {
        var authBypass = Environment.GetEnvironmentVariable("AUTH_BYPASS")?.ToLower() == "true";
        if (authBypass)
        {
            app.UseMiddleware<AuthBypassMiddleware>();
        }

        return app;
    }
}
