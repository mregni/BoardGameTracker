using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights.Specifications;

public class UpcomingGameNightsSpecTests
{
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static GameNight Night(int id, string title, DateTime start, params int[] gameIds)
    {
        var night = GameNight.Create(title, "", start, 1, 1);
        night.Id = id;
        night.SetSuggestedGames(gameIds.Select(gameId => new Game($"Game {gameId}") { Id = gameId }));
        return night;
    }

    [Fact]
    public void Evaluate_ShouldReturnNightsFromNowOn_SoonestFirst()
    {
        var nights = new List<GameNight>
        {
            Night(1, "Later", Now.AddDays(10), 3),
            Night(2, "Past", Now.AddDays(-1), 4),
            Night(3, "Soon", Now.AddDays(2), 5, 6),
            Night(4, "Starts now", Now)
        };
        var spec = new UpcomingGameNightsSpec(Now);

        var result = spec.Evaluate(nights).ToList();

        result.Select(x => x.Title).Should().Equal("Starts now", "Soon", "Later");
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ShouldKeepANightThatIsStillBeingPlayed_AndDropOnesThatEnded()
    {
        var playing = Night(5, "Playing", Now.AddHours(-2));
        var ended = Night(6, "Ended", Now.AddHours(2));
        ended.End(Now);
        var outsideWindow = Night(7, "Yesterday", Now - GameNight.PlayWindow);
        var spec = new UpcomingGameNightsSpec(Now);

        var result = spec.Evaluate([playing, ended, outsideWindow]).ToList();

        result.Select(x => x.Title).Should().Equal("Playing");
    }

    [Fact]
    public void Evaluate_ShouldProjectTheNightAndItsSuggestedGameIds()
    {
        var spec = new UpcomingGameNightsSpec(Now);

        var result = spec.Evaluate([Night(3, "Soon", Now.AddDays(2), 5, 6)]).Single();

        result.Id.Should().Be(3);
        result.Title.Should().Be("Soon");
        result.StartDate.Should().Be(Now.AddDays(2));
        result.SuggestedGameIds.Should().Equal(5, 6);
    }

    [Fact]
    public void Evaluate_ShouldBreakTiesById()
    {
        var spec = new UpcomingGameNightsSpec(Now);

        var result = spec.Evaluate([Night(8, "B", Now.AddDays(1)), Night(2, "A", Now.AddDays(1))]).ToList();

        result.Select(x => x.Id).Should().Equal(2, 8);
    }
}
