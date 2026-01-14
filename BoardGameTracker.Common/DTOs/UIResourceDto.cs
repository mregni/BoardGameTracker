namespace BoardGameTracker.Common.DTOs;

public class UIResourceDto
{
    public string TimeFormat { get; set; } = string.Empty;
    public string DateFormat { get; set; } = string.Empty;
    public string UILanguage { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool Statistics { get; set; }
    public bool UpdateCheckEnabled { get; set; }
    public int UpdateCheckIntervalHours { get; set; }
}
