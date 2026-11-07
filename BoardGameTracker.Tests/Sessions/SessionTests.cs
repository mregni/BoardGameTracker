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

        session.PlayerSessions.Should().ContainSingle().Which.Score.Should().Be(10);
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

    [Fact]
    public void SetGameNight_ShouldLinkAndUnlinkTheNight()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        session.SetGameNight(4);
        session.GameNightId.Should().Be(4);

        session.SetGameNight(null);
        session.GameNightId.Should().BeNull();
    }

    [Fact]
    public void SetGameNight_ShouldRejectAnIdThatIsNotPositive()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        var act = () => session.SetGameNight(0);

        act.Should().Throw<ArgumentException>();
        session.GameNightId.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldStartAsACompetitiveSession()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        session.IsCoop.Should().BeFalse();
        session.TeamWon.Should().BeNull();
        session.TeamScore.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MarkAsCoop_ShouldGiveEveryPlayerTheTeamResult_AndClearTheirScores(bool teamWon)
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        session.AddPlayerSession(1, 10, true, !teamWon);
        session.AddPlayerSession(2, 20, false, teamWon);

        session.MarkAsCoop(teamWon, 55);

        session.IsCoop.Should().BeTrue();
        session.TeamWon.Should().Be(teamWon);
        session.TeamScore.Should().Be(55);
        session.PlayerSessions.Should().OnlyContain(ps => ps.Won == teamWon && ps.Score == null);
        session.PlayerSessions.Single(ps => ps.PlayerId == 1).FirstPlay.Should().BeTrue();
    }

    [Fact]
    public void AddPlayerSession_ShouldFollowTheTeamResult_WhenCoop()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        session.MarkAsCoop(true, null);

        session.AddPlayerSession(3, 12, true, false);

        var playerSession = session.PlayerSessions.Single();
        playerSession.Won.Should().BeTrue();
        playerSession.Score.Should().BeNull();
        playerSession.FirstPlay.Should().BeTrue();
    }

    [Fact]
    public void MarkAsCompetitive_ShouldClearTheTeamResult_AndKeepThePlayerResults()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        session.AddPlayerSession(1, null, false, false);
        session.MarkAsCoop(true, 30);

        session.MarkAsCompetitive();

        session.IsCoop.Should().BeFalse();
        session.TeamWon.Should().BeNull();
        session.TeamScore.Should().BeNull();
        session.PlayerSessions.Single().Won.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldStoreTheGivenValues()
    {
        var session = new Session(3, Start, Start.AddMinutes(90), "Fun");

        session.GameId.Should().Be(3);
        session.Start.Should().Be(Start);
        session.End.Should().Be(Start.AddMinutes(90));
        session.Comment.Should().Be("Fun");
        session.GetDuration().Should().Be(TimeSpan.FromMinutes(90));
        session.PlayerSessions.Should().BeEmpty();
        session.Expansions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCommentIsNull()
    {
        var act = () => new Session(1, Start, Start.AddHours(1), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateComment_ShouldStoreTheNewComment()
    {
        var session = new Session(1, Start, Start.AddHours(1), "before");

        session.UpdateComment("after");

        session.Comment.Should().Be("after");
    }

    [Fact]
    public void UpdateTimes_ShouldAcceptAZeroLengthSession()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        session.UpdateTimes(Start, Start);

        session.GetDuration().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AddPlayerSession_ShouldStoreTheGivenResult_WhenCompetitive()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        session.AddPlayerSession(4, 15, true, true);

        var playerSession = session.PlayerSessions.Single();
        playerSession.PlayerId.Should().Be(4);
        playerSession.Score.Should().Be(15);
        playerSession.FirstPlay.Should().BeTrue();
        playerSession.Won.Should().BeTrue();
    }

    [Fact]
    public void AddPlayerSession_ShouldFollowTheTeamLoss_WhenCoop()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        session.MarkAsCoop(false, null);

        session.AddPlayerSession(3, 12, false, true);

        session.PlayerSessions.Single().Won.Should().BeFalse();
    }

    [Fact]
    public void AddExpansion_ShouldKeepDifferentExpansions()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        var first = new Expansion("First", 1, 1);
        var second = new Expansion("Second", 2, 1);

        session.AddExpansion(first);
        session.AddExpansion(second);

        session.Expansions.Should().Equal(first, second);
    }

    [Fact]
    public void RemoveExpansion_ShouldOnlyRemoveThatExpansion()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        var first = new Expansion("First", 1, 1);
        var second = new Expansion("Second", 2, 1);
        session.AddExpansion(first);
        session.AddExpansion(second);

        session.RemoveExpansion(first);

        session.Expansions.Should().Equal(second);
    }

    [Fact]
    public void AddExpansion_And_RemoveExpansion_ShouldRejectNull()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        var add = () => session.AddExpansion(null!);
        var remove = () => session.RemoveExpansion(null!);

        add.Should().Throw<ArgumentNullException>();
        remove.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPlayerSession_ShouldAcceptThePlayerAgain_AfterRemoval()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);
        session.AddPlayerSession(7, 10, false, false);

        session.RemovePlayerSession(7);
        session.AddPlayerSession(7, 20, false, true);

        var playerSession = session.PlayerSessions.Single();
        playerSession.Score.Should().Be(20);
        playerSession.Won.Should().BeTrue();
    }

    [Fact]
    public void SetGameNight_ShouldRejectANegativeId()
    {
        var session = new Session(1, Start, Start.AddHours(1), string.Empty);

        var act = () => session.SetGameNight(-1);

        act.Should().Throw<ArgumentException>();
    }
}
