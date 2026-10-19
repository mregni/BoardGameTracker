using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Infrastructure;

public enum OidcHttpsRefusal
{
    LoginPage,
    CallbackPage,
    BadRequest,
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireOidcHttpsAttribute : TypeFilterAttribute
{
    public RequireOidcHttpsAttribute(OidcHttpsRefusal refusal) : base(typeof(RequireOidcHttpsFilter))
    {
        Arguments = [refusal];
    }
}

public sealed class RequireOidcHttpsFilter : IAsyncActionFilter
{
    public const string LoginPageRefusal = "/login?error=" + Constants.Errors.OidcHttpsRequired;
    public const string CallbackPageRefusal = "/auth-callback?error=" + Constants.Errors.OidcHttpsRequired;

    private readonly OidcHttpsRefusal _refusal;
    private readonly IPublicUrlBuilder _publicUrlBuilder;
    private readonly ILogger<RequireOidcHttpsFilter> _logger;

    public RequireOidcHttpsFilter(OidcHttpsRefusal refusal, IPublicUrlBuilder publicUrlBuilder, ILogger<RequireOidcHttpsFilter> logger)
    {
        _refusal = refusal;
        _publicUrlBuilder = publicUrlBuilder;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await OidcTransportPolicy.IsAllowedAsync(context.HttpContext.Request, _publicUrlBuilder))
        {
            await next();
            return;
        }

        _logger.LogWarning("OIDC {Action} refused because the connection or the public URL is not HTTPS", context.ActionDescriptor.DisplayName);
        context.Result = _refusal switch
        {
            OidcHttpsRefusal.LoginPage => new LocalRedirectResult(LoginPageRefusal),
            OidcHttpsRefusal.CallbackPage => new LocalRedirectResult(CallbackPageRefusal),
            _ => throw new ValidationException(Constants.Errors.OidcHttpsRequired),
        };
    }
}
