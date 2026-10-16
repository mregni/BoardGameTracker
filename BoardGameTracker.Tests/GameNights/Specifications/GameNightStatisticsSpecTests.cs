using System.Linq;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights.Specifications;

public class GameNightStatisticsSpecTests
{
    private static readonly DateTime Start = new(2026, 3, 6, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GameNightStatisticsSpec_ShouldProjectTheTableThePlanAndTheResults()
    {
        var gameNight = GameNight.Create("Strategy night", "", Start, 1, 4);
        gameNight.Id = 9;
        gameNight.End(Start.AddHours(4));
        gameNight.SetInvitedPlayers([
            GameNightRsvp.Create(1, GameNightRsvpState.Accepted),
            GameNightRsvp.Create(2, GameNightRsvpState.Declined),
            GameNightRsvp.Create(3, GameNightRsvpState.Pending),
            GameNightRsvp.Create(5, GameNightRsvpState.Accepted)
        ]);
        gameNight.SetSuggestedGames([new Game("Azul") { Id = 12 }]);
        var session = new Session(12, Start, Start.AddMinutes(45), string.Empty);
        session.AddPlayerSession(1, 30, false, true);
        session.AddPlayerSession(5, 20, false, false);
        gameNight.Sessions.Add(session);
        var coop = new Session(12, Start.AddHours(1), Start.AddHours(2), string.Empty);
        coop.AddPlayerSession(1, null, false, false);
        coop.MarkAsCoop(true, null);
        gameNight.Sessions.Add(coop);

        var result = new GameNightStatisticsSpec(null, Start.AddDays(1)).Evaluate([gameNight]).Single();

        result.Id.Should().Be(9);
        result.Title.Should().Be("Strategy night");
        result.StartDate.Should().Be(Start);
        result.EndedAt.Should().Be(Start.AddHours(4));
        result.HostId.Should().Be(1);
        result.LocationId.Should().Be(4);
        result.AttendeeIds.Should().Equal(1, 5);
        result.PlannedGameIds.Should().Equal(12);
        result.Sessions.Should().HaveCount(2);
        result.Sessions[0].IsCoop.Should().BeFalse();
        result.Sessions[1].IsCoop.Should().BeTrue();
        result.Sessions[1].Results.Should().Equal(new NightResult(1, true));
        result.Sessions[0].GameId.Should().Be(12);
        result.Sessions[0].Start.Should().Be(Start);
        result.Sessions[0].End.Should().Be(Start.AddMinutes(45));
        result.Sessions[0].Results.Should().Equal(new NightResult(1, true), new NightResult(5, false));
    }

    [Fact]
    public void GameNightStatisticsSpec_ShouldKeepTheNightsInThePeriod_OldestFirst_WithoutTracking()
    {
        var late = GameNight.Create("Late", "", Start.AddDays(7), 1, 1);
        late.Id = 2;
        var early = GameNight.Create("Early", "", Start, 1, 1);
        early.Id = 1;
        var before = GameNight.Create("Before", "", Start.AddDays(-1), 1, 1);
        before.Id = 3;
        var after = GameNight.Create("After", "", Start.AddDays(30), 1, 1);
        after.Id = 4;

        var spec = new GameNightStatisticsSpec(Start, Start.AddDays(7));

        spec.Evaluate([late, early, before, after]).Select(x => x.Id).Should().Equal(1, 2);
        spec.AsNoTracking.Should().BeTrue();
    }
}
