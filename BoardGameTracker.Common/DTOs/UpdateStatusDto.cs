using BoardGameTracker.Common.Models.Updates;

namespace BoardGameTracker.Common.DTOs;

public class UpdateStatusDto
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string? LatestVersion { get; set; }
    public bool UpdateAvailable { get; set; }
    public DateTime? LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class UpdateStatusDtoExtensions
{
    public static UpdateStatusDto ToDto(this UpdateStatus status)
    {
        return new UpdateStatusDto
        {
            CurrentVersion = status.CurrentVersion,
            LatestVersion = status.LatestVersion,
            UpdateAvailable = status.UpdateAvailable,
            LastChecked = status.LastChecked,
            ErrorMessage = status.ErrorMessage
        };
    }
}
