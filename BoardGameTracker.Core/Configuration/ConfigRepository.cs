using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Configuration;

public class ConfigRepository : IConfigRepository
{
    private readonly MainDbContext _context;

    public ConfigRepository(MainDbContext context)
    {
        _context = context;
    }

    public Task<T> GetConfigValueAsync<T>(string key)
    {
        var envValue = Environment.GetEnvironmentVariable(key.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return Task.FromResult(ConvertFromString<T>(envValue.Trim(), key));
        }

        return GetConfigValueFromDbAsync<T>(key);
    }

    public async Task SetConfigValueAsync<T>(string key, T value)
    {
        var stringValue = ConvertToString(value);
        var normalizedKey = key.ToLowerInvariant();

        var rowsAffected = await _context.Config
            .Where(c => c.Key == normalizedKey)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Value, stringValue));

        if (rowsAffected == 0)
        {
            var config = new Config { Key = normalizedKey, Value = stringValue };
            await _context.Config.AddAsync(config);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<string, string>> GetConfigsByPrefixAsync(string prefix)
    {
        var configs = await _context.Config
            .Where(c => c.Key.StartsWith(prefix))
            .ToDictionaryAsync(c => c.Key, c => c.Value);
        return configs;
    }

    public async Task SeedConfigAsync(IReadOnlyList<ConfigDefault> defaults)
    {
        var existingKeys = await _context.Config
            .Select(c => c.Key)
            .ToListAsync();

        var existingKeySet = new HashSet<string>(existingKeys);

        var missingConfigs = defaults
            .Where(d => !existingKeySet.Contains(d.Key.ToLowerInvariant()))
            .Select(d => new Config { Key = d.Key, Value = d.Value })
            .ToList();

        if (missingConfigs.Count > 0)
        {
            await _context.Config.AddRangeAsync(missingConfigs);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<T> GetConfigValueFromDbAsync<T>(string key)
    {
        var config = await _context.Config
            .FirstOrDefaultAsync(c => c.Key == key.ToLowerInvariant());

        if (config?.Value is null)
            throw new ConfigMissingException(key);

        return ConvertFromString<T>(config.Value, key);
    }

    private static T ConvertFromString<T>(string value, string key)
    {
        var type = typeof(T);

        if (type == typeof(string))
            return (T)(object)value;

        if (type == typeof(int))
        {
            if (int.TryParse(value, out var intResult))
                return (T)(object)intResult;
            throw new ConfigMissingException(key);
        }

        if (type == typeof(bool))
        {
            if (bool.TryParse(value, out var boolResult))
                return (T)(object)boolResult;
            throw new ConfigMissingException(key);
        }

        if (type.IsEnum)
        {
            if (Enum.TryParse(type, value, true, out var enumResult))
                return (T)enumResult!;
            throw new ConfigMissingException(key);
        }

        throw new ConfigMissingException(key);
    }

    private static string ConvertToString<T>(T value)
    {
        return value switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            Enum e => e.ToString().ToLowerInvariant(),
            _ => value?.ToString() ?? string.Empty
        };
    }
}
