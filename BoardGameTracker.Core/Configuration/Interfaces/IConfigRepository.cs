using BoardGameTracker.Common.Configuration;

namespace BoardGameTracker.Core.Configuration.Interfaces;

public interface IConfigRepository
{
    Task<T> GetConfigValueAsync<T>(string key);
    Task SetConfigValueAsync<T>(string key, T value);
    Task<Dictionary<string, string>> GetConfigsByPrefixAsync(string prefix);
    Task SeedConfigAsync(IReadOnlyList<ConfigDefault> defaults);
}
