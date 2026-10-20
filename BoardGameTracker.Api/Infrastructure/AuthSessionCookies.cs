using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BoardGameTracker.Api.Infrastructure;

public sealed class AuthSessionCookies
{
    public const string RefreshCookieName = "bgt_refresh";
    public const string RefreshCookiePath = "/api/auth";

    private readonly IProfileImageTicketService _imageTickets;
    private readonly TimeSpan _refreshLifetime;

    public AuthSessionCookies(IProfileImageTicketService imageTickets, IOptions<JwtOptions> options)
    {
        _imageTickets = imageTickets;
        _refreshLifetime = TimeSpan.FromDays(options.Value.RefreshTokenExpiryDays);
    }

    public void Issue(HttpContext context, string refreshToken, string userId)
    {
        context.Response.Cookies.Append(RefreshCookieName, refreshToken, RefreshOptions(context, _refreshLifetime));
        ProfileImageCookie.Issue(context, _imageTickets.Issue(userId), _imageTickets.Lifetime);
    }

    public static void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(RefreshCookieName, RefreshOptions(context, null));
        ProfileImageCookie.Clear(context);
    }

    public static string? ReadRefreshToken(HttpRequest request)
    {
        var token = request.Cookies[RefreshCookieName];
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static CookieOptions RefreshOptions(HttpContext context, TimeSpan? lifetime) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = RefreshCookiePath,
        MaxAge = lifetime,
        IsEssential = true,
    };
}
