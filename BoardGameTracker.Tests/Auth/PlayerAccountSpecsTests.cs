using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class PlayerAccountSpecsTests
{
    private static ApplicationUser User(string id, Player? player, string? image)
    {
        var user = new ApplicationUser(id, null, $"{id} display") { Id = id };
        if (player != null)
        {
            user.LinkPlayer(player.Id);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Player))!.SetValue(user, player);
        }

        if (image != null)
        {
            user.UseUploadedProfileImage(image);
        }

        return user;
    }

    [Fact]
    public void LinkedAccountByPlayerIdSpec_ShouldProjectTheAccountLinkedToThePlayer()
    {
        var linked = User("jane", new Player("Jane") { Id = 7 }, "/images/profile/jane.webp");
        var other = User("john", new Player("John") { Id = 8 }, null);
        var spec = new LinkedAccountByPlayerIdSpec(7);

        var result = spec.Evaluate([linked, other]).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be("jane");
        result[0].Username.Should().Be("jane");
        result[0].DisplayName.Should().Be("jane display");
        result[0].ProfileImage.Should().Be("/images/profile/jane.webp");
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void AccountsAwaitingPlayerImageSpec_ShouldOnlyMatchLinkedAccountsWithAPicture_WhosePlayerHasNone()
    {
        var waiting = User("waiting", new Player("Waiting") { Id = 1 }, "/images/profile/a.webp");
        var ownPicture = User("own", new Player("Own", "/images/profile/own.webp") { Id = 2 }, "/images/profile/b.webp");
        var noPicture = User("none", new Player("None") { Id = 3 }, null);
        var unlinked = User("unlinked", null, "/images/profile/c.webp");
        var spec = new AccountsAwaitingPlayerImageSpec();

        var result = spec.Evaluate([waiting, ownPicture, noPicture, unlinked]).ToList();

        result.Should().ContainSingle().Which.Should().BeSameAs(waiting);
        spec.AsNoTracking.Should().BeTrue();
    }
}
