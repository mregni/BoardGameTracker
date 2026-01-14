namespace BoardGameTracker.Common.Models.Updates;

public class UpdateStatus
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string? LatestVersion { get; set; }
    public bool UpdateAvailable { get; set; }
    public DateTime? LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}
