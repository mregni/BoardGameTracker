using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BoardGameTracker.Core.Auth;

public class LoginAttemptTracker : ILoginAttemptTracker
{
    public const int MaxFailures = 5;
    public const int AccountFailureThreshold = 20;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan AccountThrottleInterval = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    public LoginAttemptTracker(IMemoryCache cache, TimeProvider? timeProvider = null)
    {
        _cache = cache;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsLockedOut(string username, string clientAddress)
    {
        if (_cache.TryGetValue<int>(Key(username, clientAddress), out var failures) && failures >= MaxFailures)
        {
            return true;
        }

        return _cache.TryGetValue<AccountFailures>(AccountKey(username), out var account) &&
               account!.Count >= AccountFailureThreshold &&
               _timeProvider.GetUtcNow() - account.LastFailure < AccountThrottleInterval;
    }

    public void RecordFailure(string username, string clientAddress)
    {
        var key = Key(username, clientAddress);
        var failures = _cache.TryGetValue<int>(key, out var current) ? current + 1 : 1;
        _cache.Set(key, failures, LockoutDuration);

        var accountKey = AccountKey(username);
        var count = _cache.TryGetValue<AccountFailures>(accountKey, out var account) ? account!.Count + 1 : 1;
        _cache.Set(accountKey, new AccountFailures(count, _timeProvider.GetUtcNow()), LockoutDuration);
    }

    public void Reset(string username, string clientAddress)
    {
        _cache.Remove(Key(username, clientAddress));
    }

    private static string Normalize(string username) => username.Trim().ToUpperInvariant();

    private static string Key(string username, string clientAddress) =>
        $"login-failures:{Normalize(username)}|{clientAddress}";

    private static string AccountKey(string username) => $"login-failures-account:{Normalize(username)}";

    private sealed record AccountFailures(int Count, DateTimeOffset LastFailure);
}
