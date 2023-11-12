using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IEnvironmentProvider
{
    string EnvironmentName { get;  }
    int Port { get; }
    bool EnableStatistics { get; }
    LogLevel LogLevel { get; }
    bool IsDevelopment { get; }
}