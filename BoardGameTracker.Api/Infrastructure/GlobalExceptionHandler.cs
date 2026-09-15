using System.Data.Common;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        var (statusCode, message) = MapException(exception);

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

    private static (int StatusCode, string Message) MapException(Exception exception) => exception switch
    {
        BggFeatureDisabledException => (StatusCodes.Status503ServiceUnavailable, exception.Message),
        ConfigMissingException => (StatusCodes.Status503ServiceUnavailable, "The requested feature is not configured."),
        BggRateLimitException => (StatusCodes.Status429TooManyRequests, exception.Message),
        BggCollectionPreparingException => (StatusCodes.Status504GatewayTimeout, exception.Message),
        BoardGameGeekHttpException => (StatusCodes.Status502BadGateway, "The BoardGameGeek service is currently unavailable. Please try again later."),
        BadHttpRequestException badRequest => (badRequest.StatusCode, badRequest.Message),
        ValidationException or DomainException => (StatusCodes.Status400BadRequest, exception.Message),
        AuthenticationFailedException => (StatusCodes.Status401Unauthorized, exception.Message),
        EntityNotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request."),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The resource was modified by another request. Please retry."),
        DbUpdateException { InnerException: DbException innerException } when IsClientDataError(innerException) =>
            (StatusCodes.Status400BadRequest, "The request references data that does not exist or conflicts with existing data."),
        DbException dbException when IsClientDataError(dbException) =>
            (StatusCodes.Status400BadRequest, "The request contains invalid data."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
    };

    private static bool IsClientDataError(DbException exception) =>
        exception.SqlState?.StartsWith("22", StringComparison.Ordinal) == true ||
        exception.SqlState?.StartsWith("23", StringComparison.Ordinal) == true;
}
