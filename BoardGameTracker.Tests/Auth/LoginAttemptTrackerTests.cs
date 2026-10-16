using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class LoginAttemptTrackerTests
{
    private readonly LoginAttemptTracker _tracker = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void IsLockedOut_ShouldBecomeTrue_AfterMaxFailuresFromTheSameClient()
    {
        for (var i = 0; i < LoginAttemptTracker.MaxFailures - 1; i++)
        {
            _tracker.RecordFailure("Admin", "10.0.0.1");
        }

        _tracker.IsLockedOut("admin", "10.0.0.1").Should().BeFalse();

        _tracker.RecordFailure("admin", "10.0.0.1");

        _tracker.IsLockedOut("ADMIN", "10.0.0.1").Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_ShouldNotAffectOtherClientsOrAccounts()
    {
        for (var i = 0; i < LoginAttemptTracker.MaxFailures; i++)
        {
            _tracker.RecordFailure("admin", "10.0.0.1");
        }

        _tracker.IsLockedOut("admin", "10.0.0.2").Should().BeFalse();
        _tracker.IsLockedOut("alice", "10.0.0.1").Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_ShouldThrottleTheAccount_AfterManyFailuresFromDifferentAddresses()
    {
        var clock = new ManualTimeProvider();
        var tracker = new LoginAttemptTracker(new MemoryCache(new MemoryCacheOptions()), clock);

        for (var i = 0; i < LoginAttemptTracker.AccountFailureThreshold; i++)
        {
            tracker.RecordFailure("admin", $"2001:db8:{i}::/64");
        }

        tracker.IsLockedOut("admin", "203.0.113.50").Should().BeTrue();
        tracker.IsLockedOut("alice", "203.0.113.50").Should().BeFalse();

        clock.Advance(LoginAttemptTracker.AccountThrottleInterval);

        tracker.IsLockedOut("admin", "203.0.113.50").Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_ShouldNotThrottleTheAccount_BelowTheThreshold()
    {
        var tracker = new LoginAttemptTracker(new MemoryCache(new MemoryCacheOptions()), new ManualTimeProvider());

        for (var i = 0; i < LoginAttemptTracker.AccountFailureThreshold - 1; i++)
        {
            tracker.RecordFailure("admin", $"10.0.{i}.1");
        }

        tracker.IsLockedOut("admin", "203.0.113.50").Should().BeFalse();
    }

    [Fact]
    public void Reset_ShouldClearTheFailures()
    {
        for (var i = 0; i < LoginAttemptTracker.MaxFailures; i++)
        {
            _tracker.RecordFailure("admin", "10.0.0.1");
        }

        _tracker.Reset("admin", "10.0.0.1");

        _tracker.IsLockedOut("admin", "10.0.0.1").Should().BeFalse();
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
