using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class NightAttendanceRuleTests
{
    private static readonly DateTime Start = new(2026, 10, 16, 19, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Derive_ShouldBeJustTheHost_BeforeAnybodyPlayed()
    {
        NightAttendanceRule.Derive(4, [], []).Should().Equal(4);
    }

    [Fact]
    public void Derive_ShouldAddEveryoneWhoPlayed_HostFirst()
    {
        NightAttendanceRule.Derive(4, [7, 2, 7, 4, 3], []).Should().Equal(4, 2, 3, 7);
    }

    [Fact]
    public void Derive_ShouldApplyOverrides_ButNeverDropSomeoneWhoPlayed()
    {
        var result = NightAttendanceRule.Derive(
            4,
            [2],
            [new AttendanceOverride(9, true), new AttendanceOverride(4, false), new AttendanceOverride(2, false)]);

        result.Should().Equal(2, 9);
    }

    [Fact]
    public void OverridesFor_ShouldOnlyStoreWhatPlayDoesNotAlreadyTell()
    {
        var result = NightAttendanceRule.OverridesFor(4, [2, 3], [2, 3, 8, 6]);

        result.Should().Equal(new AttendanceOverride(4, false), new AttendanceOverride(6, true), new AttendanceOverride(8, true));
    }

    [Fact]
    public void OverridesFor_ShouldStoreNothing_WhenThePickerMatchesWhoPlayed()
    {
        NightAttendanceRule.OverridesFor(4, [4, 2], [2, 4]).Should().BeEmpty();
    }

    [Fact]
    public void GameNight_ShouldDeriveAttendeesFromSessionsRunningGamesHostAndOverrides()
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        var session = new Session(9, Start, Start.AddHours(1), string.Empty);
        session.AddPlayerSession(2, null, false, true);
        gameNight.Sessions.Add(session);
        gameNight.RunningGames.Add(RunningGame.Create(3, 9, Start.AddHours(1), [3, 2]));

        gameNight.GetPlayedPlayerIds().Should().BeEquivalentTo([2, 3]);
        gameNight.GetAttendeeIds().Should().Equal(1, 2, 3);

        gameNight.SetAttendance([2, 3, 5]);

        gameNight.GetAttendeeIds().Should().Equal(2, 3, 5);
        gameNight.Attendance.Should().HaveCount(2);

        gameNight.SetAttendance([1, 2, 3, 5]);

        gameNight.Attendance.Should().ContainSingle(x => x.PlayerId == 5 && x.Present);
        gameNight.GetAttendeeIds().Should().Equal(1, 2, 3, 5);
    }

    [Fact]
    public void GameNight_ShouldUpdateAnExistingOverride_InsteadOfAddingAnother()
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        gameNight.SetAttendance([5]);
        gameNight.SetAttendance([1, 5]);
        gameNight.SetAttendance([5]);

        gameNight.Attendance.Should().HaveCount(2);
        gameNight.Attendance.Should().ContainSingle(x => x.PlayerId == 1 && !x.Present);
    }

    [Fact]
    public void ToSummaryDto_ShouldCarryTheRunningGamesAndTheDerivedTable()
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        gameNight.RunningGames.Add(RunningGame.Create(3, 9, Start, [3]));
        gameNight.RunningGames.Add(RunningGame.Create(3, 8, Start, [4]));

        var summary = gameNight.ToSummaryDto();

        summary.RunningGames.Should().Be(2);
        summary.AttendeeIds.Should().Equal(1, 3, 4);
        summary.AttendanceRecorded.Should().BeFalse();

        gameNight.SetAttendance([1, 3, 4, 6]);

        gameNight.ToSummaryDto().AttendanceRecorded.Should().BeTrue();
    }

    [Fact]
    public void ToUpdateDto_ShouldTellWhetherTheHostWasNotified()
    {
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Maybe);

        var dto = rsvp.ToUpdateDto(true);

        dto.PlayerId.Should().Be(3);
        dto.State.Should().Be(GameNightRsvpState.Maybe);
        dto.HostNotified.Should().BeTrue();
    }

    [Theory]
    [InlineData(11, false, false)]
    [InlineData(12, false, true)]
    [InlineData(1, true, true)]
    public void IsOver_ShouldBeTrue_OnceEndedOrPastThePlayWindow(int hoursSinceStart, bool ended, bool expected)
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        if (ended)
        {
            gameNight.End(Start.AddMinutes(30));
        }

        gameNight.IsOver(Start.AddHours(hoursSinceStart)).Should().Be(expected);
        GameNight.IsOverAt(Start, gameNight.EndedAt, Start.AddHours(hoursSinceStart)).Should().Be(expected);
    }
}
