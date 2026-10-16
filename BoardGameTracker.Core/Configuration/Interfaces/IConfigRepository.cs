using BoardGameTracker.Common.Configuration;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IConfigRepository
{
    Task<T> GetConfigValueAsync<T>(string key);
    Task<T> GetConfigValueOrDefaultAsync<T>(string key, T fallback);
    Task SetConfigValueAsync<T>(string key, T value);
    Task<bool> ReplaceConfigValueAsync(string key, string expected, string value);
    Task<Dictionary<string, string>> GetAllConfigsAsync();
    Task<Dictionary<string, string>> GetConfigsByPrefixAsync(string prefix);
    Task SeedConfigAsync(IReadOnlyList<ConfigDefault> defaults);
}
