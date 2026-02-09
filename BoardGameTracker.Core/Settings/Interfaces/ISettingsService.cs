using BoardGameTracker.Common.DTOs;

namespace BoardGameTracker.Core.Settings.Interfaces;

public interface ISettingsService
{
    Task<UIResourceDto> GetSettingsAsync();
    Task UpdateSettingsAsync(UIResourceDto model);
}
