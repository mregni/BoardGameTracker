using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights.Specifications;

public class NextGameNightSpecTests
{
    private static readonly DateTime From = new(2030, 1, 10, 0, 0, 0, DateTimeKind.Utc);

    private static GameNight Night(int id, string title, DateTime start)
    {
        var night = GameNight.Create(title, string.Empty, start, 1, 1);
        night.Id = id;
        typeof(GameNight).GetProperty(nameof(GameNight.Host))!.SetValue(night, new Player("Kathleen") { Id = 1 });
        typeof(GameNight).GetProperty(nameof(GameNight.Location))!.SetValue(night, new Location("Home") { Id = 1 });
        return night;
    }

    [Fact]
    public void Evaluate_ShouldReturnUpcomingAndStillPlayingNightsInStartOrder_WithIdAsTieBreaker()
    {
        var past = Night(1, "Past", From - GameNight.PlayWindow - TimeSpan.FromMinutes(1));
        var later = Night(2, "Later", From.AddDays(5));
        var sameStartHigherId = Night(4, "Same start, higher id", From.AddDays(1));
        var next = Night(3, "Next", From.AddDays(1));
        var playing = Night(5, "Playing", From.AddHours(-1));
        var ended = Night(6, "Ended", From.AddHours(-1));
        ended.End(From);

        var spec = new NextGameNightSpec(From);
        var result = spec.Evaluate(new List<GameNight> { past, later, sameStartHigherId, next, playing, ended }).ToList();

        result.Select(x => x.Id).Should().Equal(5, 3, 4, 2);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ShouldProjectHostLocationRsvpCountsAndSortedSuggestions()
    {
        var night = Night(1, "Strategy night", From.AddDays(2));
        night.SetSuggestedGames([new Game("Brass") { Id = 2 }, new Game("Azul") { Id = 1 }]);
        night.SetInvitedPlayers([
            GameNightRsvp.Create(1, GameNightRsvpState.Accepted),
            GameNightRsvp.Create(2, GameNightRsvpState.Accepted),
            GameNightRsvp.Create(3, GameNightRsvpState.Pending),
            GameNightRsvp.Create(4, GameNightRsvpState.Declined),
            GameNightRsvp.Create(5, GameNightRsvpState.Maybe),
            GameNightRsvp.Create(6, GameNightRsvpState.Maybe)
        ]);

        var result = new NextGameNightSpec(From).Evaluate([night]).Single();

        result.Id.Should().Be(1);
        result.Title.Should().Be("Strategy night");
        result.StartDate.Should().Be(From.AddDays(2));
        result.HostId.Should().Be(1);
        result.HostName.Should().Be("Kathleen");
        result.LocationName.Should().Be("Home");
        result.AcceptedCount.Should().Be(2);
        result.MaybeCount.Should().Be(2);
        result.PendingCount.Should().Be(1);
        result.DeclinedCount.Should().Be(1);
        result.SuggestedGames.Select(x => x.Title).Should().Equal("Azul", "Brass");
    }
}
