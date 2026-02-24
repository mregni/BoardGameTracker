using System.Security.Claims;
using BoardGameTracker.Common;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Api.Infrastructure;

public class AuthBypassMiddleware
{
    private readonly RequestDelegate _next;

    public AuthBypassMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "bypass-admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, Constants.AuthRoles.Admin),
                new Claim("display_name", "Admin (Bypass)")
            };

            var identity = new ClaimsIdentity(claims, "AuthBypass");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
