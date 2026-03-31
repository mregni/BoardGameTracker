using Serilog.Events;

namespace BoardGameTracker.Common.DTOs;

public class UIEnvironmentDto
{
    public bool EnableStatistics { get; set; }
    public LogEventLevel LogLevel { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Version { get; set; } = string.Empty;
}
