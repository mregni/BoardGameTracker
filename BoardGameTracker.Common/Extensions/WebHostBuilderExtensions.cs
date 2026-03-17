using BoardGameTracker.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Sentry.AspNetCore;
using Serilog.Events;

namespace BoardGameTracker.Common.Extensions;

public static class WebHostBuilderExtensions
{
    private static readonly HashSet<Type> IgnoredExceptionTypes =
    [
        typeof(ValidationException),
        typeof(DomainException),
        typeof(EntityNotFoundException),
        typeof(UnauthorizedAccessException),
        typeof(KeyNotFoundException),
        typeof(ArgumentException)
    ];

    public static IWebHostBuilder UseConfiguredSentry(this IWebHostBuilder builder)
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("STATISTICS_ENABLED"), out var statisticsEnabled) &&  statisticsEnabled)
        {
            builder.UseSentry(o =>
            {
                o.Environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "development";
                o.Debug = LogLevelExtensions.GetEnvironmentLogLevel() == LogEventLevel.Debug;
                o.TracesSampleRate = 0.1;
                o.SendDefaultPii = false;
                o.Dsn = "https://3d89aa9317b0a7b3108edbafd31da95a@o4506121302573056.ingest.us.sentry.io/4506121326559232";
                
                o.SetBeforeSend((@event, _) =>
                {
                    if (@event.Exception != null && IgnoredExceptionTypes.Contains(@event.Exception.GetType()))
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
}