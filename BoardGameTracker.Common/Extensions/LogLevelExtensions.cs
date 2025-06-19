using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace BoardGameTracker.Common.Extensions;

public static class LogLevelExtensions
{
    public static LogEventLevel  GetEnvironmentLogLevel()
    {
        var logLevelString = Environment.GetEnvironmentVariable("LOGLEVEL") ?? "WARNING";
        logLevelString = logLevelString.ToUpper().Trim();
        return logLevelString switch
        {
            "ERROR" => LogEventLevel.Error,
            "INFO" => LogEventLevel.Information,
            "DEBUG" => LogEventLevel.Debug,
            _ => LogEventLevel.Warning
        };
    }
}