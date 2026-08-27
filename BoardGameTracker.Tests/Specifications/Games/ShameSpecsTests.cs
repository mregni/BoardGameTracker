using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Games.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Games;

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

    private static List<Game> Fixture() =>
    [
        GameWithSessions(1, "Zebra", GameState.Owned),
        GameWithSessions(2, "Owned recent", GameState.Owned, Cutoff.AddDays(1)),
        GameWithSessions(3, "Alpha", GameState.Owned, Cutoff.AddDays(-20), Cutoff.AddDays(-10)),
        GameWithSessions(4, "Wanted", GameState.Wanted),
        GameWithSessions(5, "Both old and recent", GameState.Owned, Cutoff.AddDays(-10), Cutoff.AddDays(1))
    ];

    [Fact]
    public void GamesWithNoRecentSessionsSpec_ShouldReturnOnlyOwnedGamesWithoutRecentSessions_OrderedByTitle()
    {
        var spec = new GamesWithNoRecentSessionsSpec(Cutoff);

        var result = spec.Evaluate(Fixture()).ToList();

        result.Select(x => x.Id).Should().Equal(3, 1);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void ShameGamesSpec_ShouldReturnOnlyOwnedGamesWithoutRecentSessions_OrderedByTitle()
    {
        var spec = new ShameGamesSpec(Cutoff);

        var result = spec.Evaluate(Fixture()).ToList();

        result.Select(x => x.Id).Should().Equal(3, 1);
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
}
