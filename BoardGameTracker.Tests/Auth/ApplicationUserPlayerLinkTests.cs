using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class ApplicationUserPlayerLinkTests
{
    [Fact]
    public void ANewAccount_ShouldNotNeedAPlayerLink()
    {
        var user = new ApplicationUser("alice", "alice@example.com");

        user.PlayerLinkPending.Should().BeFalse();
        user.NeedsPlayerLink.Should().BeFalse();
        user.ToUserInfo([]).NeedsPlayerLink.Should().BeFalse();
    }

    [Fact]
    public void AwaitPlayerLink_ShouldMakeTheAccountNeedAPlayerLink()
    {
        var user = new ApplicationUser("alice", "alice@example.com");

        user.AwaitPlayerLink();

        user.NeedsPlayerLink.Should().BeTrue();
        user.ToUserInfo([]).NeedsPlayerLink.Should().BeTrue();
        user.ToProfileDto([]).NeedsPlayerLink.Should().BeTrue();
    }

    [Fact]
    public void LinkPlayer_ShouldClearThePendingLink()
    {
        var user = new ApplicationUser("alice", "alice@example.com");
        user.AwaitPlayerLink();

        user.LinkPlayer(3);

        user.PlayerLinkPending.Should().BeFalse();
        user.NeedsPlayerLink.Should().BeFalse();
    }

    [Fact]
    public void SkipPlayerLink_ShouldClearThePendingLink()
    {
        var user = new ApplicationUser("alice", "alice@example.com");
        user.AwaitPlayerLink();

        user.SkipPlayerLink();

        user.PlayerLinkPending.Should().BeFalse();
        user.NeedsPlayerLink.Should().BeFalse();
    }

    [Fact]
    public void UnlinkPlayer_ShouldClearThePlayer_WithoutAskingForANewLink()
    {
        var user = new ApplicationUser("alice", "alice@example.com");
        user.LinkPlayer(3);

        user.UnlinkPlayer();

        user.PlayerId.Should().BeNull();
        user.NeedsPlayerLink.Should().BeFalse();
    }

    [Fact]
    public void NeedsPlayerLink_ShouldBeFalse_WhileAPlayerIsLinked()
    {
        var user = new ApplicationUser("alice", "alice@example.com");
        user.LinkPlayer(3);

        user.AwaitPlayerLink();

        user.PlayerLinkPending.Should().BeTrue();
        user.NeedsPlayerLink.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    public void IsDisabled_ShouldFollowTheLockoutEnd(int hoursFromNow, bool expected)
    {
        var user = new ApplicationUser("alice", "alice@example.com") { LockoutEnd = DateTimeOffset.UtcNow.AddHours(hoursFromNow) };

        user.IsDisabled.Should().Be(expected);
        user.ToDto([]).IsDisabled.Should().Be(expected);
    }

    [Fact]
    public void IsDisabled_ShouldBeFalse_WithoutALockout()
    {
        new ApplicationUser("alice", "alice@example.com").IsDisabled.Should().BeFalse();
    }
}
