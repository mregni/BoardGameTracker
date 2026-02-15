using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models.Updates;

namespace BoardGameTracker.Common.Extensions;

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
