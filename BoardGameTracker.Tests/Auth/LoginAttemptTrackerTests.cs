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
    public void Reset_ShouldClearTheFailures()
    {
        for (var i = 0; i < LoginAttemptTracker.MaxFailures; i++)
        {
            _tracker.RecordFailure("admin", "10.0.0.1");
        }

        _tracker.Reset("admin", "10.0.0.1");

        _tracker.IsLockedOut("admin", "10.0.0.1").Should().BeFalse();
    }
}
