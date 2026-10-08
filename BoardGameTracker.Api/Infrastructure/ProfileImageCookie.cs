using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Api.Infrastructure;

public static class ProfileImageCookie
{
    public const string Name = "bgt_images";
    public const string Path = "/images/profile";

    public static void Issue(HttpContext context, string ticket, TimeSpan lifetime)
    {
        context.Response.Cookies.Append(Name, ticket, Options(context, lifetime));
    }

    public static void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(Name, Options(context, null));
    }

    private static CookieOptions Options(HttpContext context, TimeSpan? lifetime) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = Path,
        MaxAge = lifetime,
        IsEssential = true,
    };
}
