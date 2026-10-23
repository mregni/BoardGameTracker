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
}
