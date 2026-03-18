using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameTracker.Api.Infrastructure;

public static class AuthDisabledExtensions
{
    public static IApplicationBuilder UseAuthDisabledMiddleware(this IApplicationBuilder app)
    {
        var environmentProvider = app.ApplicationServices.GetRequiredService<IEnvironmentProvider>();
        if (!environmentProvider.AuthEnabled)
        {
            app.UseMiddleware<AuthDisabledMiddleware>();
        }

        return app;
    }
}
