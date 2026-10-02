using BoardGameTracker.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning(exception, "Exception after the response started for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            return false;
        }

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var (statusCode, message) = ExceptionStatusMapper.Map(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogDebug(exception, "Request {Method} {Path} failed with {StatusCode}", httpContext.Request.Method, httpContext.Request.Path, statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = message
            }
        });

        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails { Status = statusCode, Title = message }, cancellationToken);
        }

        return true;
    }

}
