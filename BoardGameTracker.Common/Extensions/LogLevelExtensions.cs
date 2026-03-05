using Serilog.Events;

namespace BoardGameTracker.Common.Extensions;

public static class LogLevelExtensions
{
    public static LogEventLevel  GetEnvironmentLogLevel()
    {
        var logLevelString = Environment.GetEnvironmentVariable("LOGLEVEL") ?? "warn";
        logLevelString = logLevelString.ToLower().Trim();
        return logLevelString switch
        {
            "error" => LogEventLevel.Error,
            "info" => LogEventLevel.Information,
            "debug" => LogEventLevel.Debug,
            _ => LogEventLevel.Warning
        };
    }
}