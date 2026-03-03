using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BoardGameTracker.Api.Infrastructure;

public class ValidateIdFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var (key, value) in context.ActionArguments)
        {
            if (value is int id && (key.Equals("id", StringComparison.OrdinalIgnoreCase) || key.EndsWith("Id", StringComparison.OrdinalIgnoreCase)))
            {
                if (id <= 0)
                {
                    context.Result = new BadRequestObjectResult(new ProblemDetails
                    {
                        Status = 400,
                        Title = $"Invalid {key}. Must be greater than 0."
                    });
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
