using Serilog.Events;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IEnvironmentProvider
{
    string EnvironmentName { get;  }
    int Port { get; }
    bool StatisticsEnabled { get; }
    LogEventLevel LogLevel { get; }
    bool IsDevelopment { get; }
    bool AuthEnabled { get; }
    string? JwtSecret { get; }
}