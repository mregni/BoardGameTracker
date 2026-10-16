using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class LinkedPlayerByUserIdSpecTests
{
    private static ApplicationUser User(string id, Player? player)
    {
        var user = new ApplicationUser(id, null) { Id = id };
        if (player != null)
        {
            user.LinkPlayer(player.Id);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Player))!.SetValue(user, player);
        }

        return user;
    }

    [Fact]
    public void Evaluate_ShouldProjectTheLinkedPlayerOfTheUser()
    {
        var linked = User("linked", new Player("Mikhaël") { Id = 7 });
        var other = User("other", new Player("Kathleen") { Id = 8 });
        var spec = new LinkedPlayerByUserIdSpec("linked");

        var result = spec.Evaluate([linked, other]).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(7);
        result[0].Name.Should().Be("Mikhaël");
        result[0].SessionsThisMonth.Should().Be(0);
        result[0].WinsThisMonth.Should().Be(0);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ShouldReturnNothing_WhenTheUserHasNoLinkedPlayer()
    {
        var unlinked = User("unlinked", null);

        var result = new LinkedPlayerByUserIdSpec("unlinked").Evaluate([unlinked]);

        result.Should().BeEmpty();
    }
}
