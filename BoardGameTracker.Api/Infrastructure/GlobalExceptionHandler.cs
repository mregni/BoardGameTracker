using BoardGameTracker.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = message
        }, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Message) MapException(Exception exception) => exception switch
    {
        ValidationException or DomainException => (StatusCodes.Status400BadRequest, exception.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),
        EntityNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
        ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
    };
}
