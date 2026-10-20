using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BoardGameTracker.Core.Auth;

public class UserAccessCheck : IUserAccessCheck
{
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(30);

    private readonly MainDbContext _context;
    private readonly IMemoryCache _cache;

    public UserAccessCheck(MainDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> CanSignInAsync(string userId, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(userId);
        if (_cache.TryGetValue(key, out bool allowed))
        {
            return allowed;
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.LockoutEnd })
            .FirstOrDefaultAsync(cancellationToken);

        allowed = user != null && (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow);
        _cache.Set(key, allowed, CacheLifetime);
        return allowed;
    }

    public void Forget(string userId)
    {
        _cache.Remove(CacheKey(userId));
    }

    private static string CacheKey(string userId) => $"user-access:{userId}";
}
