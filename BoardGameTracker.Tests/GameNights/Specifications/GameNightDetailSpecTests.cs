using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights.Specifications;

public class GameNightDetailSpecTests
{
    private static readonly DateTime Start = new(2030, 1, 1, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GameNightDetailSpec_ShouldMatchOnlyTheRequestedId()
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        gameNight.Id = 5;

        new GameNightDetailSpec(5).IsSatisfiedBy(gameNight).Should().BeTrue();
        new GameNightDetailSpec(6).IsSatisfiedBy(gameNight).Should().BeFalse();
    }

    [Fact]
    public void GameNightDetailSpec_ShouldIncludeSessionsAndRunningGames_WithoutTracking()
    {
        var spec = new GameNightDetailSpec(1);

        spec.ShouldIncludeExactly(
            "Host",
            "Location",
            "SuggestedGames",
            "InvitedPlayers",
            "InvitedPlayers.Player",
            "Sessions",
            "Sessions.PlayerSessions",
            "Sessions",
            "Sessions.Expansions",
            "RunningGames",
            "RunningGames.Players",
            "Attendance");
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void GameNightAttendanceSpec_ShouldMatchOnlyTheRequestedId_AndTrackWhoPlayedAndTheOverrides()
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        gameNight.Id = 5;

        var spec = new GameNightAttendanceSpec(5);

        spec.IsSatisfiedBy(gameNight).Should().BeTrue();
        new GameNightAttendanceSpec(6).IsSatisfiedBy(gameNight).Should().BeFalse();
        spec.ShouldIncludeExactly("Sessions", "Sessions.PlayerSessions", "RunningGames", "RunningGames.Players", "Attendance");
        spec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void GameNightByIdSpec_ShouldMatchOnlyTheRequestedId_WithoutTracking()
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        gameNight.Id = 5;

        new GameNightByIdSpec(5).IsSatisfiedBy(gameNight).Should().BeTrue();
        new GameNightByIdSpec(6).IsSatisfiedBy(gameNight).Should().BeFalse();
        new GameNightByIdSpec(5).AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void GameNightWithRunningGamesSpec_ShouldMatchOnlyTheRequestedId_AndTrackItsRunningGames()
    {
        var gameNight = GameNight.Create("Night", "", Start, 1, 1);
        gameNight.Id = 5;

        var spec = new GameNightWithRunningGamesSpec(5);

        spec.IsSatisfiedBy(gameNight).Should().BeTrue();
        new GameNightWithRunningGamesSpec(6).IsSatisfiedBy(gameNight).Should().BeFalse();
        spec.ShouldIncludeExactly("RunningGames");
        spec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void RunningGameByIdSpec_ShouldMatchOnlyTheRequestedId_AndIncludeTheNight()
    {
        var runningGame = RunningGame.Create(3, 9, Start, [1]);
        runningGame.Id = 12;

        var spec = new RunningGameByIdSpec(12);

        spec.IsSatisfiedBy(runningGame).Should().BeTrue();
        new RunningGameByIdSpec(13).IsSatisfiedBy(runningGame).Should().BeFalse();
        spec.ShouldIncludeExactly("GameNight");
        spec.AsNoTracking.Should().BeFalse();
    }
}
