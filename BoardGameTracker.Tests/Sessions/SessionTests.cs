using System;
using System.Linq;
using BoardGameTracker.Common.Entities;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Sessions;

public class SessionTests
{
    private static readonly DateTime Start = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_ShouldThrow_WhenEndIsBeforeStart()
    {
        var act = () => new Session(1, Start, Start.AddMinutes(-1), string.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("End time cannot be before start time.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_ShouldThrow_WhenGameIdIsNotPositive(int gameId)
    {
        var act = () => new Session(gameId, Start, Start.AddHours(1), string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldAcceptAZeroLengthSession()
    {
        var session = new Session(1, Start, Start, string.Empty);

        session.GetDuration().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void UpdateTimes_ShouldThrow_AndKeepTheOldTimes_WhenEndIsBeforeStart()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        var act = () => session.UpdateTimes(Start.AddDays(1), Start);

        act.Should().Throw<ArgumentException>();
        session.Start.Should().Be(Start);
        session.End.Should().Be(Start.AddHours(1));
    }

    [Fact]
    public void UpdateTimes_ShouldMoveBothEnds()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        session.UpdateTimes(Start.AddDays(1), Start.AddDays(1).AddMinutes(45));

        session.Start.Should().Be(Start.AddDays(1));
        session.GetDuration().Should().Be(TimeSpan.FromMinutes(45));
    }

    [Fact]
    public void AddPlayerSession_ShouldIgnoreTheSamePlayerTwice()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        session.AddPlayerSession(7, 10, true, true);
        session.AddPlayerSession(7, 99, false, false);

        session.GetPlayerCount().Should().Be(1);
        session.PlayerSessions.Single().Score.Should().Be(10);
    }

    [Fact]
    public void AddPlayerSession_ShouldThrow_WhenPlayerIdIsNotPositive()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        var act = () => session.AddPlayerSession(0, null, false, false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemovePlayerSession_ShouldRemoveOnlyThatPlayer()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        session.AddPlayerSession(1, null, false, true);
        session.AddPlayerSession(2, null, false, false);

        session.RemovePlayerSession(1);
        session.RemovePlayerSession(42);

        session.PlayerSessions.Select(ps => ps.PlayerId).Should().Equal(2);
    }

    [Fact]
    public void AddExpansion_ShouldNotDuplicateTheSameExpansion()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        var expansion = new Expansion("Expansion", 123, 1);

        session.AddExpansion(expansion);
        session.AddExpansion(expansion);
        session.RemoveExpansion(expansion);

        session.Expansions.Should().BeEmpty();
    }

    [Fact]
    public void SetLocation_ShouldSyncTheForeignKey()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        var location = new Location("Kitchen") { Id = 5 };

        session.SetLocation(location);
        var withLocation = session.LocationId;
        session.SetLocation(null);

        withLocation.Should().Be(5);
        session.LocationId.Should().BeNull();
        session.Location.Should().BeNull();
    }

    [Fact]
    public void UpdateComment_ShouldRejectNull()
    {
        var session = new Session(1, Start, Start.AddHours(1), "before");

        var act = () => session.UpdateComment(null!);

        act.Should().Throw<ArgumentNullException>();
        session.Comment.Should().Be("before");
    }
}
