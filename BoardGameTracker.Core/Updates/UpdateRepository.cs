using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Updates;

public class UpdateRepository : IUpdateRepository
{
    private readonly MainDbContext _context;

    public UpdateRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default!)
    {
        var config = await _context.Config
            .FirstOrDefaultAsync(c => c.Key == key.ToLowerInvariant());

        if (config?.Value is null)
            return defaultValue;

        return ConvertFromString(config.Value, defaultValue);
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

    public async Task<Dictionary<string, string>> GetUpdateConfigAsync()
    {
        var configs = await _context.Config
            .Where(c => c.Key.StartsWith(Constants.UpdateConfig.Prefix))
            .ToDictionaryAsync(c => c.Key, c => c.Value);
        return configs;
    }

    private static T ConvertFromString<T>(string value, T defaultValue)
    {
        var type = typeof(T);

        if (type == typeof(string))
            return (T)(object)value;

        if (type == typeof(int))
            return int.TryParse(value, out var intResult) ? (T)(object)intResult : defaultValue;

        if (type == typeof(bool))
            return bool.TryParse(value, out var boolResult) ? (T)(object)boolResult : defaultValue;

        if (type.IsEnum)
            return Enum.TryParse(type, value, true, out var enumResult) ? (T)enumResult! : defaultValue;

        return defaultValue;
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
