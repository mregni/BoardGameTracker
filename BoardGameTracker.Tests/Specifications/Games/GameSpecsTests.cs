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
    public void GamesByIdsSpec_ShouldReturnNothing_WhenIdsAreEmpty()
    {
        var a = new Game("A") { Id = 1 };

        new GamesByIdsSpec(Array.Empty<int>()).Evaluate(new[] { a }).Should().BeEmpty();
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
    public void ExpansionsByIdsSpec_ShouldReturnNothing_WhenIdsAreEmpty()
    {
        var e1 = new Expansion("Seafarers", 100, 1) { Id = 1 };

        new ExpansionsByIdsSpec(Array.Empty<int>()).Evaluate(new[] { e1 }).Should().BeEmpty();
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

    public static TheoryData<Specification<Game>, Specification<Game>, bool> DetailSpecs => new()
    {
        { new GameByIdWithDetailsSpec(5), new GameByIdWithDetailsSpec(6), false },
        { new GameByIdWithDetailsForReadSpec(5), new GameByIdWithDetailsForReadSpec(6), true }
    };

    [Theory]
    [MemberData(nameof(DetailSpecs))]
    public void GameByIdDetailSpecs_ShouldMatchOnlyRequestedId_IncludeGraph_AndSetTracking(
        Specification<Game> matching, Specification<Game> nonMatching, bool asNoTracking)
    {
        var game = new Game("Catan") { Id = 5 };

        matching.IsSatisfiedBy(game).Should().BeTrue();
        nonMatching.IsSatisfiedBy(game).Should().BeFalse();
        matching.IncludeExpressions.Should().HaveCount(6);
        matching.AsNoTracking.Should().Be(asNoTracking);
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
    public void RecentlyAddedGamesSpec_ShouldFilterNullDates_OrderDescending_Take_AndNotTrack()
    {
        var newest = new Game("Newest") { Id = 1 };
        newest.UpdateAdditionDate(new DateTime(2030, 3, 1));
        var oldest = new Game("Oldest") { Id = 2 };
        oldest.UpdateAdditionDate(new DateTime(2030, 1, 1));
        var middle = new Game("Middle") { Id = 4 };
        middle.UpdateAdditionDate(new DateTime(2030, 2, 1));
        var noDate = new Game("NoDate") { Id = 3 };
        noDate.UpdateAdditionDate(null);
        var spec = new RecentlyAddedGamesSpec(2);

        var result = spec.Evaluate(new[] { oldest, newest, noDate, middle }).ToList();

        result.Select(x => x.Id).Should().Equal(1, 4);
        spec.AsNoTracking.Should().BeTrue();
    }
}
