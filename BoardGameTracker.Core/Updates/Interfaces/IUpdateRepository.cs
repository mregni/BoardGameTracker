namespace BoardGameTracker.Core.Updates.Interfaces;

public interface IUpdateRepository
{
    Task<string?> GetConfigValueAsync(string key);
    Task SetConfigValueAsync(string key, string value);
    Task<Dictionary<string, string>> GetUpdateConfigAsync();
}
