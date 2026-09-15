using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace BoardGameTracker.Common.Extensions;

public static class WebHostBuilderExtensions
{
    private static readonly Type[] IgnoredExceptionTypes =
    [
        typeof(ValidationException),
        typeof(DomainException),
        typeof(EntityNotFoundException),
        typeof(AuthenticationFailedException),
        typeof(KeyNotFoundException),
        typeof(ArgumentException),
        typeof(BadHttpRequestException),
        typeof(OperationCanceledException),
        typeof(BggRateLimitException),
        typeof(BggCollectionPreparingException),
        typeof(BggFeatureDisabledException),
        typeof(BoardGameGeekHttpException),
        typeof(ConfigMissingException),
        typeof(DbUpdateConcurrencyException)
    ];

    public static IWebHostBuilder UseConfiguredSentry(this IWebHostBuilder builder)
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("STATISTICS_ENABLED"), out var statisticsEnabled) &&  statisticsEnabled)
        {
            builder.UseSentry(o =>
            {
                o.Environment = EnvironmentExtensions.GetEnvironmentName();
                o.Debug = EnvironmentExtensions.IsDevelopment() && LogLevelExtensions.GetEnvironmentLogLevel() == LogEventLevel.Debug;
                o.TracesSampleRate = 0.1;
                o.SendDefaultPii = false;
                o.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN") is { Length: > 0 } dsn
                    ? dsn
                    : "https://3d89aa9317b0a7b3108edbafd31da95a@o4506121302573056.ingest.us.sentry.io/4506121326559232";

                o.SetBeforeSend((@event, _) =>
                {
                    if (@event.Exception != null && IsIgnored(@event.Exception))
                    {
                        return null;
                    }

                    @event.ServerName = null;
                    return @event;
                });
            });
        }

        return builder;
    }

    private static bool IsIgnored(Exception exception)
    {
        return IgnoredExceptionTypes.Any(type => type.IsInstanceOfType(exception));
    }
}
