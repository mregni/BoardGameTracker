using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Configuration;

public class ConfigRepository : IConfigRepository
{
    private readonly MainDbContext _context;
    private readonly ILogger<ConfigRepository> _logger;

    public ConfigRepository(MainDbContext context, ILogger<ConfigRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<T> GetConfigValueAsync<T>(string key)
    {
        var variable = key.ToUpperInvariant();
        var envValue = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            if (TypeConverter.TryConvertFromString<T>(envValue.Trim(), out var result))
            {
                return Task.FromResult(result);
            }

            _logger.LogWarning("Environment variable {Variable} has a value that cannot be read as {Type}; using the stored setting instead", variable, typeof(T).Name);
        }

        return GetConfigValueFromDbAsync<T>(key);
    }

    public async Task<T> GetConfigValueOrDefaultAsync<T>(string key, T fallback)
    {
        try
        {
            return await GetConfigValueAsync<T>(key);
        }
        catch (ConfigMissingException)
        {
            return fallback;
        }
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
            try
            {
                await _context.SaveChangesAsync();
                _context.Entry(config).State = EntityState.Detached;
            }
            catch (DbUpdateException)
            {
                _context.Entry(config).State = EntityState.Detached;
                await _context.Config
                    .Where(c => c.Key == normalizedKey)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Value, stringValue));
            }
        }
    }

    public async Task<Dictionary<string, string>> GetAllConfigsAsync()
    {
        return await _context.Config
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Key, c => c.Value);
    }

    public async Task<Dictionary<string, string>> GetConfigsByPrefixAsync(string prefix)
    {
        var configs = await _context.Config
            .AsNoTracking()
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
        var normalizedKey = key.ToLowerInvariant();
        var config = await _context.Config
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == normalizedKey);

        if (config?.Value is null)
        {
            throw new ConfigMissingException(key);
        }

        if (TypeConverter.TryConvertFromString<T>(config.Value, out var result))
        {
            return result;
        }

        throw new ConfigMissingException(key);
    }

    private static string ConvertToString<T>(T value)
    {
        return value switch
        {
            null => string.Empty,
            bool b => b.ToString().ToLowerInvariant(),
            Enum e => e.ToString().ToLowerInvariant(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
