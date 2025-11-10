using System.Reflection;
using BoardGameTracker.Common.Extensions;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace BoardGameTracker.Common.ViewModels;

public class UIEnvironmentViewModel
{
    public required string EnvironmentName { get; set; }
    public int Port { get; set; }
    public bool EnableStatistics { get; set; }
    public LogEventLevel LogLevel { get; set; }
    public string Version => Assembly.GetEntryAssembly().GetName().Version.ToVersionString();
}