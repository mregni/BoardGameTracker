using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BoardGameTracker.Core.Auth;

public class LoginAttemptTracker : ILoginAttemptTracker
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public LoginAttemptTracker(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsLockedOut(string username, string clientAddress)
    {
        return _cache.TryGetValue<int>(Key(username, clientAddress), out var failures) && failures >= MaxFailures;
    }

    public void RecordFailure(string username, string clientAddress)
    {
        var key = Key(username, clientAddress);
        var failures = _cache.TryGetValue<int>(key, out var current) ? current + 1 : 1;
        _cache.Set(key, failures, LockoutDuration);
    }

    public void Reset(string username, string clientAddress)
    {
        _cache.Remove(Key(username, clientAddress));
    }

    private static string Key(string username, string clientAddress) =>
        $"login-failures:{username.Trim().ToUpperInvariant()}|{clientAddress}";
}
