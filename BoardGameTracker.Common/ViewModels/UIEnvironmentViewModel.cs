using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Common.ViewModels;

public class UIEnvironmentViewModel
{
    public string EnvironmentName { get; set; }
    public int Port { get; set; }
    public bool EnableStatistics { get; set; }
    public LogLevel LogLevel { get; set; }
}