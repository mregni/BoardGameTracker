using BoardGameTracker.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BoardGameTracker.Api.Infrastructure;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireSessionHeaderAttribute : ActionFilterAttribute
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "XMLHttpRequest";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (string.Equals(context.HttpContext.Request.Headers[HeaderName], HeaderValue, StringComparison.Ordinal))
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = Constants.Errors.MissingSessionHeader,
        })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
