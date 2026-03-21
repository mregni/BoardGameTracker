using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BoardGameTracker.Api.Infrastructure;

public class AuthDisabledFilter : IActionFilter
{
    private readonly IEnvironmentProvider _environmentProvider;

    public AuthDisabledFilter(IEnvironmentProvider environmentProvider)
    {
        _environmentProvider = environmentProvider;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!_environmentProvider.AuthEnabled)
        {
            var path = context.HttpContext.Request.Path.Value ?? "";
            if (path.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
                return;

            context.Result = new ConflictObjectResult("Authentication is disabled. This endpoint is not available.");
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
