using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IEnvironmentProvider
{
    string EnvironmentName { get;  }
    int Port { get; }
    bool EnableStatistics { get; }
    LogEventLevel LogLevel { get; }
    bool IsDevelopment { get; }
}