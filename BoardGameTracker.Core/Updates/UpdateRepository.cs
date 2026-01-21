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

    public async Task<string?> GetConfigValueAsync(string key)
    {
        var config = await _context.Config
            .FirstOrDefaultAsync(c => c.Key == key.ToLowerInvariant());
        return config?.Value;
    }

    public async Task SetConfigValueAsync(string key, string value)
    {
        var normalizedKey = key.ToLowerInvariant();

        var rowsAffected = await _context.Config
            .Where(c => c.Key == normalizedKey)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Value, value));

        if (rowsAffected == 0)
        {
            var config = new Config { Key = normalizedKey, Value = value };
            await _context.Config.AddAsync(config);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<Dictionary<string, string>> GetUpdateConfigAsync()
    {
        var configs = await _context.Config
            .Where(c => c.Key.StartsWith("update_"))
            .ToDictionaryAsync(c => c.Key, c => c.Value);
        return configs;
    }
}
