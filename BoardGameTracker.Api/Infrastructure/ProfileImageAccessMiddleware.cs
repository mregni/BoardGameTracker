using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Api.Infrastructure;

public class ProfileImageAccessMiddleware
{
    private readonly RequestDelegate _next;

    public ProfileImageAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IProfileImageTicketService tickets)
    {
        if (context.User.Identity?.IsAuthenticated == true || tickets.IsValid(context.Request.Cookies[ProfileImageCookie.Name]))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.CacheControl = "no-store";
    }
}
