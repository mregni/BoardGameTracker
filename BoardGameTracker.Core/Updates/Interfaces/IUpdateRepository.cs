namespace BoardGameTracker.Core.Updates.Interfaces;

public interface IUpdateRepository
{
    Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default!);
    Task SetConfigValueAsync<T>(string key, T value);
    Task<Dictionary<string, string>> GetUpdateConfigAsync();
}
