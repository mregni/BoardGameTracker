using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
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

    [Fact]
    public void Reset_ShouldNotClearTheFailuresOfTheAccountOrOfOtherClients()
    {
        var clock = new ManualTimeProvider();
        var tracker = new LoginAttemptTracker(new MemoryCache(new MemoryCacheOptions()), clock);
        for (var i = 0; i < LoginAttemptTracker.MaxFailures; i++)
        {
            tracker.RecordFailure("admin", "10.0.0.1");
        }

        for (var i = 0; i < LoginAttemptTracker.AccountFailureThreshold; i++)
        {
            tracker.RecordFailure("admin", $"10.0.{i}.2");
        }

        tracker.Reset("admin", "10.0.0.2");

        tracker.IsLockedOut("admin", "10.0.0.1").Should().BeTrue();
        tracker.IsLockedOut("admin", "203.0.113.50").Should().BeTrue();
    }

    [Fact]
    public void Limits_ShouldKeepTheirValues()
    {
        LoginAttemptTracker.MaxFailures.Should().Be(5);
        LoginAttemptTracker.AccountFailureThreshold.Should().Be(20);
        LoginAttemptTracker.LockoutDuration.Should().Be(TimeSpan.FromMinutes(15));
        LoginAttemptTracker.AccountThrottleInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void IsLockedOut_ShouldIgnoreCaseAndSurroundingSpaces_InTheUsername()
    {
        for (var i = 0; i < LoginAttemptTracker.MaxFailures; i++)
        {
            _tracker.RecordFailure(" admin ", "10.0.0.1");
        }

        _tracker.IsLockedOut("ADMIN", "10.0.0.1").Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_ShouldEnd_WhenTheLockoutDurationHasPassed()
    {
        var clock = new ManualCacheClock();
        var tracker = new LoginAttemptTracker(new MemoryCache(new MemoryCacheOptions { Clock = clock }));
        for (var i = 0; i < LoginAttemptTracker.MaxFailures; i++)
        {
            tracker.RecordFailure("admin", "10.0.0.1");
        }

        clock.UtcNow += LoginAttemptTracker.LockoutDuration - TimeSpan.FromSeconds(1);
        tracker.IsLockedOut("admin", "10.0.0.1").Should().BeTrue();

        clock.UtcNow += TimeSpan.FromSeconds(2);
        tracker.IsLockedOut("admin", "10.0.0.1").Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_ShouldRestartTheThrottle_WithEveryNewFailure()
    {
        var clock = new ManualTimeProvider();
        var tracker = new LoginAttemptTracker(new MemoryCache(new MemoryCacheOptions()), clock);
        for (var i = 0; i < LoginAttemptTracker.AccountFailureThreshold; i++)
        {
            tracker.RecordFailure("admin", $"10.0.{i}.1");
        }

        clock.Advance(LoginAttemptTracker.AccountThrottleInterval);
        tracker.RecordFailure("admin", "10.0.99.1");
        clock.Advance(LoginAttemptTracker.AccountThrottleInterval - TimeSpan.FromSeconds(1));

        tracker.IsLockedOut("admin", "203.0.113.50").Should().BeTrue();
    }

    private sealed class ManualCacheClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
