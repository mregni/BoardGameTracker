using BoardGameTracker.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;
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
        builder.UseSentry(o =>
        {
            o.Environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "development";
            o.Debug = LogLevelExtensions.GetEnvironmentLogLevel() == LogEventLevel.Debug;
            o.TracesSampleRate = 1.0;
            o.SendDefaultPii = false;

            o.SetBeforeSend((@event, hint) =>
            {
                if (hint.Exception != null && IgnoredExceptionTypes.Contains(hint.Exception.GetType()))
                {
                    return null;
                }

                @event.ServerName = null;
                return @event;
            });

            var stateString = Environment.GetEnvironmentVariable("STATISTICS") ?? "0";
            if (stateString == "1")
            {
                o.Dsn = "https://3d89aa9317b0a7b3108edbafd31da95a@o4506121302573056.ingest.sentry.io/4506121326559232";
                return;
            }
            
            o.Dsn = string.Empty;
            o.DisableDiagnosticSourceIntegration();
            o.DisableDuplicateEventDetection();
            o.DisableUnobservedTaskExceptionCapture();
            o.DisableAppDomainProcessExitFlush();
            o.DisableAppDomainUnhandledExceptionCapture();
            o.TracesSampleRate = 0;
        });

        return builder;
    }
}