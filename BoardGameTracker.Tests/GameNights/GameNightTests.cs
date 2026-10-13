using System;
using BoardGameTracker.Common.Entities;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class GameNightTests
{
    private static readonly DateTime Start = new(2026, 10, 16, 20, 0, 0, DateTimeKind.Utc);

    private static GameNight CreateNight() => GameNight.Create("Night", "", Start, 1, 1);

    private static Session CreateSession(int minutes, params (int PlayerId, bool Won)[] players)
    {
        var session = new Session(1, Start, Start.AddMinutes(minutes), string.Empty);
        foreach (var (playerId, won) in players)
        {
            session.AddPlayerSession(playerId, null, false, won);
        }

        return session;
    }

    [Fact]
    public void Summary_ShouldBeEmpty_WhenNothingWasPlayed()
    {
        var gameNight = CreateNight();

        gameNight.GetTotalPlayMinutes().Should().Be(0);
        gameNight.GetMostWins().Should().Be(0);
        gameNight.GetNightWinnerIds().Should().BeEmpty();
    }

    [Fact]
    public void End_ShouldRecordWhenTheNightEnded_AndReopenShouldClearIt()
    {
        var gameNight = CreateNight();
        var endedAt = Start.AddHours(4);

        gameNight.End(endedAt);
        gameNight.EndedAt.Should().Be(endedAt);

        gameNight.Reopen();
        gameNight.EndedAt.Should().BeNull();
    }

    [Fact]
    public void End_ShouldRejectAMissingTime()
    {
        var gameNight = CreateNight();

        var act = () => gameNight.End(default);

        act.Should().Throw<ArgumentException>();
        gameNight.EndedAt.Should().BeNull();
    }

    [Fact]
    public void GetTotalPlayMinutes_ShouldAddUpEverySession()
    {
        var gameNight = CreateNight();
        gameNight.Sessions.Add(CreateSession(80, (1, true)));
        gameNight.Sessions.Add(CreateSession(25, (1, true)));

        gameNight.GetTotalPlayMinutes().Should().Be(105);
    }

    [Fact]
    public void GetNightWinnerIds_ShouldReturnThePlayerWithTheMostWins()
    {
        var gameNight = CreateNight();
        gameNight.Sessions.Add(CreateSession(30, (1, true), (2, false)));
        gameNight.Sessions.Add(CreateSession(30, (1, false), (2, true)));
        gameNight.Sessions.Add(CreateSession(30, (1, true), (2, false)));

        gameNight.GetNightWinnerIds().Should().Equal(1);
        gameNight.GetMostWins().Should().Be(2);
    }

    [Fact]
    public void GetNightWinnerIds_ShouldShareTheWin_WhenPlayersAreTied()
    {
        var gameNight = CreateNight();
        gameNight.Sessions.Add(CreateSession(30, (3, false), (2, true)));
        gameNight.Sessions.Add(CreateSession(30, (3, true), (2, false)));

        gameNight.GetNightWinnerIds().Should().Equal(2, 3);
        gameNight.GetMostWins().Should().Be(1);
    }

    [Fact]
    public void GetNightWinnerIds_ShouldBeEmpty_WhenNobodyWon()
    {
        var gameNight = CreateNight();
        gameNight.Sessions.Add(CreateSession(30, (1, false), (2, false)));

        gameNight.GetNightWinnerIds().Should().BeEmpty();
        gameNight.GetMostWins().Should().Be(0);
    }
}
