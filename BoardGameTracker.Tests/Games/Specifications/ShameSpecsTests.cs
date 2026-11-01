using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Games.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games.Specifications;

public class ShameSpecsTests
{
    private static readonly DateTime Cutoff = new(2030, 1, 1);

    private static Game GameWithSessions(int id, string title, GameState state, params DateTime[] sessionStarts)
    {
        var game = new Game(title, false, state) { Id = id };
        foreach (var start in sessionStarts)
        {
            game.Sessions.Add(new Session(id, start, start.AddHours(2), string.Empty));
        }

        return game;
    }

    private static Game BoughtOn(int id, string title, DateTime additionDate)
    {
        var game = new Game(title, false, GameState.Owned) { Id = id };
        game.UpdateAdditionDate(additionDate);
        return game;
    }

    private static List<Game> Fixture() =>
    [
        GameWithSessions(1, "Zebra", GameState.Owned),
        GameWithSessions(2, "Owned recent", GameState.Owned, Cutoff.AddDays(1)),
        GameWithSessions(3, "Alpha", GameState.Owned, Cutoff.AddDays(-20), Cutoff.AddDays(-10)),
        GameWithSessions(4, "Wanted", GameState.Wanted),
        GameWithSessions(5, "Both old and recent", GameState.Owned, Cutoff.AddDays(-10), Cutoff.AddDays(1)),
        BoughtOn(6, "Bought after cutoff", Cutoff.AddDays(1)),
        BoughtOn(7, "Bought before cutoff", Cutoff.AddDays(-1))
    ];

    [Fact]
    public void ShameGamesSpec_ShouldReturnOnlyOwnedGamesWithoutRecentSessions_OrderedByTitle()
    {
        var spec = new ShameGamesSpec(Cutoff);

        var result = spec.Evaluate(Fixture()).ToList();

        result.Select(x => x.Id).Should().Equal(3, 7, 1);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void ShameGamesSpec_ShouldProjectAllFields_WithLatestSessionDate()
    {
        var fixture = Fixture();
        var alpha = fixture.Single(x => x.Id == 3);
        alpha.UpdateImage("alpha.png");
        alpha.UpdateAdditionDate(new DateTime(2029, 6, 1));
        alpha.UpdateBuyingPrice(42.5m);

        var result = new ShameGamesSpec(Cutoff).Evaluate(fixture).ToList();

        var projected = result.Single(x => x.Id == 3);
        projected.Title.Should().Be("Alpha");
        projected.Image.Should().Be("alpha.png");
        projected.AdditionDate.Should().Be(new DateTime(2029, 6, 1));
        projected.Price.Should().Be(42.5m);
        projected.LastSessionDate.Should().Be(Cutoff.AddDays(-10));

        var zebra = result.Single(x => x.Id == 1);
        zebra.Price.Should().BeNull();
        zebra.LastSessionDate.Should().BeNull();
    }

    [Fact]
    public void ShameGamesSpec_ShouldProjectZeroPriceAsMissing()
    {
        var fixture = Fixture();
        fixture.Single(x => x.Id == 3).UpdateBuyingPrice(0m);

        var result = new ShameGamesSpec(Cutoff).Evaluate(fixture).ToList();

        result.Single(x => x.Id == 3).Price.Should().BeNull();
    }

    [Fact]
    public void ShameGamesSpec_ShouldProjectPlayCountPlayersAndPlayTime()
    {
        var fixture = Fixture();
        var alpha = fixture.Single(x => x.Id == 3);
        alpha.UpdatePlayerCount(2, 4);
        alpha.UpdatePlayTime(60, 90);

        var result = new ShameGamesSpec(Cutoff).Evaluate(fixture).ToList();

        var projected = result.Single(x => x.Id == 3);
        projected.PlayCount.Should().Be(2);
        projected.MinPlayers.Should().Be(2);
        projected.MaxPlayers.Should().Be(4);
        projected.MinPlayTime.Should().Be(60);
        projected.MaxPlayTime.Should().Be(90);
        projected.PlannedGameNightId.Should().BeNull();
        projected.PlannedFor.Should().BeNull();

        var zebra = result.Single(x => x.Id == 1);
        zebra.PlayCount.Should().Be(0);
        zebra.MinPlayers.Should().BeNull();
        zebra.MaxPlayers.Should().BeNull();
        zebra.MinPlayTime.Should().BeNull();
        zebra.MaxPlayTime.Should().BeNull();
    }
}
