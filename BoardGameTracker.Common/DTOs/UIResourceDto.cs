using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class UIResourceDto
{
    public string TimeFormat { get; set; } = string.Empty;
    public string DateFormat { get; set; } = string.Empty;
    public string UiLanguage { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool Statistics { get; set; }
    public bool UpdateCheckEnabled { get; set; }
    public VersionTrack VersionTrack { get; set; } = VersionTrack.Stable;
    public bool ShelfOfShameEnabled { get; set; }
    public int ShelfOfShameMonthsLimit { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public bool GameNightsEnabled { get; set; }
    public bool RsvpAuthenticationEnabled { get; set; }
    public BggConfigStatusDto BggStatus { get; set; } = new();
    public string? BggApiKey { get; set; } = string.Empty;
}
