using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Games.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Games;

public class GameSpecsTests
{
    [Fact]
    public void GameByBggIdSpec_ShouldMatchOnlyTheRequestedBggId()
    {
        var game = new Game("Catan") { Id = 1 };
        game.UpdateBggId(13);

        new GameByBggIdSpec(13).IsSatisfiedBy(game).Should().BeTrue();
        new GameByBggIdSpec(99).IsSatisfiedBy(game).Should().BeFalse();
    }

    [Fact]
    public void GamesByIdsSpec_ShouldMatchOnlyGivenIds()
    {
        var a = new Game("A") { Id = 1 };
        var b = new Game("B") { Id = 2 };
        var c = new Game("C") { Id = 3 };

        var result = new GamesByIdsSpec(new[] { 1, 3 }).Evaluate(new[] { a, b, c }).ToList();

        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 3 });
    }

    [Fact]
    public void ExpansionsByIdsSpec_ShouldMatchOnlyGivenIds()
    {
        var e1 = new Expansion("Seafarers", 100, 1) { Id = 1 };
        var e2 = new Expansion("Cities", 101, 1) { Id = 2 };

        var result = new ExpansionsByIdsSpec(new[] { 2 }).Evaluate(new[] { e1, e2 }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [Fact]
    public void GamesOverviewSpec_ShouldOrderByTitle_NotTrack_AndInclude()
    {
        var banana = new Game("Banana") { Id = 1 };
        var apple = new Game("Apple") { Id = 2 };
        var spec = new GamesOverviewSpec();

        spec.Evaluate(new[] { banana, apple }).Select(x => x.Title).Should().ContainInOrder("Apple", "Banana");
        spec.AsNoTracking.Should().BeTrue();
        spec.IncludeExpressions.Should().HaveCount(2);
    }

    [Fact]
    public void GameByIdWithDetailsSpec_ShouldMatchId_IncludeGraph_AndTrack()
    {
        var game = new Game("Catan") { Id = 5 };
        var spec = new GameByIdWithDetailsSpec(5);

        spec.IsSatisfiedBy(game).Should().BeTrue();
        new GameByIdWithDetailsSpec(6).IsSatisfiedBy(game).Should().BeFalse();
        spec.IncludeExpressions.Should().HaveCount(5);
        spec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void GameByIdWithDetailsForReadSpec_ShouldMatchId_IncludeGraph_AndNotTrack()
    {
        var game = new Game("Catan") { Id = 5 };
        var spec = new GameByIdWithDetailsForReadSpec(5);

        spec.IsSatisfiedBy(game).Should().BeTrue();
        new GameByIdWithDetailsForReadSpec(6).IsSatisfiedBy(game).Should().BeFalse();
        spec.IncludeExpressions.Should().HaveCount(5);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void GameHasScoringSpec_ShouldProjectHasScoringForRequestedId_AndNotTrack()
    {
        var scoringGame = new Game("Scoring", true) { Id = 5 };
        var nonScoringGame = new Game("NoScoring", false) { Id = 6 };
        var games = new[] { scoringGame, nonScoringGame };

        new GameHasScoringSpec(5).Evaluate(games).Should().ContainSingle().Which.Should().BeTrue();
        new GameHasScoringSpec(6).Evaluate(games).Should().ContainSingle().Which.Should().BeFalse();
        new GameHasScoringSpec(999).Evaluate(games).Should().BeEmpty();
        new GameHasScoringSpec(5).AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void RecentlyAddedGamesSpec_ShouldFilterNullDates_OrderDescending_AndTake()
    {
        var newest = new Game("Newest") { Id = 1 };
        newest.UpdateAdditionDate(new DateTime(2030, 3, 1));
        var oldest = new Game("Oldest") { Id = 2 };
        oldest.UpdateAdditionDate(new DateTime(2030, 1, 1));
        var noDate = new Game("NoDate") { Id = 3 };
        noDate.UpdateAdditionDate(null);

        var result = new RecentlyAddedGamesSpec(1).Evaluate(new[] { oldest, newest, noDate }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }
}
