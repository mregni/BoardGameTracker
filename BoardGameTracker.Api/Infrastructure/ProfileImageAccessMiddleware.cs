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

    public async Task InvokeAsync(HttpContext context, IProfileImageTicketService tickets, IUserAccessCheck accessCheck)
    {
        if (context.User.Identity?.IsAuthenticated == true || await HasActiveTicketAsync(context, tickets, accessCheck))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.CacheControl = "no-store";
    }

    private static async Task<bool> HasActiveTicketAsync(HttpContext context, IProfileImageTicketService tickets, IUserAccessCheck accessCheck)
    {
        var userId = tickets.ReadUserId(context.Request.Cookies[ProfileImageCookie.Name]);
        return userId != null && await accessCheck.CanSignInAsync(userId, context.RequestAborted);
    }
}
