using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class GameDtoExtensionsTests
{
    private const string WatchId = "e0808154-28da-4b85-9a71-24a409e694f1";
    private static readonly DateTime Added = new(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);

    private static Game FullGame()
    {
        var game = new Game("Brass", true, GameState.Sold) { Id = 7 };
        game.UpdateCoopMode(true, true);
        game.UpdateYearPublished(2018);
        game.UpdateDescription("Canal era");
        game.UpdateImage("/images/cover/brass.webp");
        game.UpdateShopUrl("https://shop.example.com/brass");
        game.UpdateChangeDetectionWatchId(WatchId);
        game.UpdateLanguage("nl");
        game.UpdatePlayerCount(2, 4);
        game.UpdatePlayTime(60, 120);
        game.UpdateMinAge(14);
        game.UpdateRating(8.6);
        game.UpdateWeight(3.9);
        game.UpdateBggId(224517);
        game.UpdateBuyingPrice(60.5m);
        game.UpdateSoldPrice(45.25m);
        game.UpdateAdditionDate(Added);
        game.AddExpansion(new Expansion("Promo", 11, 7) { Id = 21 });
        game.AddCategory(new GameCategory("Economic") { Id = 31 });
        game.AddMechanic(new GameMechanic("Hand Management") { Id = 41 });
        game.AddPerson(new Person("Martin Wallace", PersonType.Designer) { Id = 51 });
        return game;
    }

    [Fact]
    public void ToDto_ShouldMapEveryField()
    {
        var dto = FullGame().ToDto();

        dto.Id.Should().Be(7);
        dto.Title.Should().Be("Brass");
        dto.YearPublished.Should().Be(2018);
        dto.Description.Should().Be("Canal era");
        dto.Image.Should().Be("/images/cover/brass.webp");
        dto.ShopUrl.Should().Be("https://shop.example.com/brass");
        dto.ChangeDetectionWatchId.Should().Be(WatchId);
        dto.Language.Should().Be("nl");
        dto.MinPlayers.Should().Be(2);
        dto.MaxPlayers.Should().Be(4);
        dto.MinPlayTime.Should().Be(60);
        dto.MaxPlayTime.Should().Be(120);
        dto.MinAge.Should().Be(14);
        dto.Rating.Should().Be(8.6);
        dto.Weight.Should().Be(3.9);
        dto.BggId.Should().Be(224517);
        dto.State.Should().Be(GameState.Sold);
        dto.HasScoring.Should().BeTrue();
        dto.HasCoopMode.Should().BeTrue();
        dto.CoopByDefault.Should().BeTrue();
        dto.BuyingPrice.Should().Be(60.5m);
        dto.SoldPrice.Should().Be(45.25m);
        dto.AdditionDate.Should().Be(Added);
        dto.IsLoaned.Should().BeFalse();
        dto.Expansions!.Select(e => (e.Id, e.Title, e.BggId)).Should().Equal((21, "Promo", 11));
        dto.Categories!.Select(c => (c.Id, c.Name)).Should().Equal((31, "Economic"));
        dto.Mechanics!.Select(m => (m.Id, m.Name)).Should().Equal((41, "Hand Management"));
        dto.People!.Select(p => (p.Id, p.Name, p.Type)).Should().Equal((51, "Martin Wallace", PersonType.Designer));
    }

    [Fact]
    public void ToDto_ShouldLeaveOptionalFieldsEmpty_WhenTheGameHasNone()
    {
        var dto = new Game("Bare").ToDto();

        dto.MinPlayers.Should().BeNull();
        dto.MaxPlayers.Should().BeNull();
        dto.MinPlayTime.Should().BeNull();
        dto.MaxPlayTime.Should().BeNull();
        dto.Rating.Should().BeNull();
        dto.Weight.Should().BeNull();
        dto.BuyingPrice.Should().BeNull();
        dto.SoldPrice.Should().BeNull();
        dto.Expansions.Should().BeEmpty();
        dto.Categories.Should().BeEmpty();
        dto.Mechanics.Should().BeEmpty();
        dto.People.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_ShouldSayTheGameIsLoaned_WhileItIsOut()
    {
        var game = new Game("Brass") { Id = 7 };
        game.LoanToPlayer(3, DateTime.UtcNow.AddDays(-1));

        game.ToDto().IsLoaned.Should().BeTrue();
    }

    [Fact]
    public void ToPublicDto_ShouldMapThePublicFields()
    {
        var dto = FullGame().ToPublicDto();

        dto.Id.Should().Be(7);
        dto.Title.Should().Be("Brass");
        dto.YearPublished.Should().Be(2018);
        dto.Image.Should().Be("/images/cover/brass.webp");
        dto.MinPlayers.Should().Be(2);
        dto.MaxPlayers.Should().Be(4);
        dto.MinPlayTime.Should().Be(60);
        dto.MaxPlayTime.Should().Be(120);
        dto.MinAge.Should().Be(14);
        dto.HasScoring.Should().BeTrue();
    }

    [Fact]
    public void ToPublicDto_ShouldNotLeakPricesOrPrivateFields()
    {
        var dto = FullGame().ToPublicDto();

        dto.BuyingPrice.Should().BeNull();
        dto.SoldPrice.Should().BeNull();
        dto.ShopUrl.Should().BeNull();
        dto.ChangeDetectionWatchId.Should().BeNull();
        dto.AdditionDate.Should().BeNull();
        dto.Expansions.Should().BeNull();
        dto.People.Should().BeNull();
    }

    [Fact]
    public void ToPublicDto_ShouldLeaveTheRangesEmpty_WhenTheGameHasNone()
    {
        var dto = new Game("Bare").ToPublicDto();

        dto.MinPlayers.Should().BeNull();
        dto.MaxPlayers.Should().BeNull();
        dto.MinPlayTime.Should().BeNull();
        dto.MaxPlayTime.Should().BeNull();
    }

    [Fact]
    public void ToListDto_ShouldMapEveryGameInOrder()
    {
        var games = new[] { new Game("One") { Id = 1 }, new Game("Two") { Id = 2 } };

        games.ToListDto().Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void ToListDto_ShouldAddThePlayStatsOfTheMatchingGameOnly()
    {
        var lastPlayed = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var games = new[] { new Game("One") { Id = 1 }, new Game("Two") { Id = 2 } };
        var stats = new Dictionary<int, GamePlayStats> { [2] = new(4, lastPlayed) };

        var result = games.ToListDto(stats);

        result[0].PlayCount.Should().Be(0);
        result[0].LastPlayed.Should().BeNull();
        result[1].PlayCount.Should().Be(4);
        result[1].LastPlayed.Should().Be(lastPlayed);
    }
}
