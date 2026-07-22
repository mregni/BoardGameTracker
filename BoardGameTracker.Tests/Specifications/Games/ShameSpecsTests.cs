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

    private static Game GameWithSession(int id, string title, GameState state, DateTime? sessionStart)
    {
        var game = new Game(title, false, state) { Id = id };
        if (sessionStart.HasValue)
        {
            game.Sessions.Add(new Session(id, sessionStart.Value, sessionStart.Value.AddHours(2), string.Empty));
        }

        return game;
    }

    private static List<Game> Fixture() =>
    [
        GameWithSession(1, "Owned no sessions", GameState.Owned, null),
        GameWithSession(2, "Owned recent", GameState.Owned, Cutoff.AddDays(1)),
        GameWithSession(3, "Owned old", GameState.Owned, Cutoff.AddDays(-10)),
        GameWithSession(4, "Wanted", GameState.Wanted, null)
    ];

    [Fact]
    public void GamesWithNoRecentSessionsSpec_ShouldReturnOnlyOwnedGamesWithoutRecentSessions()
    {
        var result = new GamesWithNoRecentSessionsSpec(Cutoff).Evaluate(Fixture()).ToList();

        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 3 });
    }

    [Fact]
    public void ShameGamesSpec_ShouldProjectWithLatestSessionDate()
    {
        var result = new ShameGamesSpec(Cutoff).Evaluate(Fixture()).ToList();

        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 3 });
        result.Single(x => x.Id == 1).LastSessionDate.Should().BeNull();
        result.Single(x => x.Id == 3).LastSessionDate.Should().Be(Cutoff.AddDays(-10));
    }
}
