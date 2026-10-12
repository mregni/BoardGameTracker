using System.Data.Common;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Common.Exceptions;

public static class ExceptionStatusMapper
{
    private static readonly Type[] ExpectedUpstreamFailures =
    [
        typeof(BggFeatureDisabledException),
        typeof(ConfigMissingException),
        typeof(BggCollectionPreparingException),
        typeof(BoardGameGeekHttpException)
    ];

    public static (int StatusCode, string Message) Map(Exception exception) => exception switch
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
        FeatureDisabledException => (StatusCodes.Status404NotFound, exception.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request."),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The resource was modified by another request. Please retry."),
        DbUpdateException { InnerException: DbException innerException } when IsClientDataError(innerException) =>
            (StatusCodes.Status400BadRequest, "The request references data that does not exist or conflicts with existing data."),
        DbException dbException when IsClientDataError(dbException) =>
            (StatusCodes.Status400BadRequest, "The request contains invalid data."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
    };

    public static bool ShouldReport(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return exception.InnerException is TimeoutException;
        }

        if (ExpectedUpstreamFailures.Contains(exception.GetType()))
        {
            return false;
        }

        return Map(exception).StatusCode >= StatusCodes.Status500InternalServerError;
    }

    private static bool IsClientDataError(DbException exception) =>
        exception.SqlState?.StartsWith("22", StringComparison.Ordinal) == true ||
        exception.SqlState?.StartsWith("23", StringComparison.Ordinal) == true;
}
