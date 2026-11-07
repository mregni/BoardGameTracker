using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
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

    [Theory]
    [InlineData(-24 * 7, false)]
    [InlineData(-13, false)]
    [InlineData(-12, true)]
    [InlineData(-1, true)]
    [InlineData(30, true)]
    public void HasPlayOpened_ShouldOpenTwelveHoursBeforeTheStart(int hoursFromStart, bool expected)
    {
        CreateNight().HasPlayOpened(Start.AddHours(hoursFromStart)).Should().Be(expected);
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

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    public void HasPlayOpened_ShouldOpenOnTheExactSecond(int secondsFromOpening, bool expected)
    {
        var opening = Start - GameNight.PlayOpensBefore;

        CreateNight().HasPlayOpened(opening.AddSeconds(secondsFromOpening)).Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void IsOver_ShouldTurnTrue_ExactlyWhenThePlayWindowCloses(int secondsFromClosing, bool expected)
    {
        var closing = Start + GameNight.PlayWindow;

        CreateNight().IsOver(closing.AddSeconds(secondsFromClosing)).Should().Be(expected);
    }

    [Fact]
    public void IsOver_ShouldBeTrue_AsSoonAsTheNightWasEnded()
    {
        var gameNight = CreateNight();

        gameNight.End(Start.AddMinutes(1));

        gameNight.IsOver(Start.AddMinutes(2)).Should().BeTrue();
    }

    [Fact]
    public void IsOverAt_ShouldLookAtTheEndAndTheWindow()
    {
        GameNight.IsOverAt(Start, Start.AddHours(1), Start).Should().BeTrue();
        GameNight.IsOverAt(Start, null, Start).Should().BeFalse();
        GameNight.IsOverAt(Start, null, Start + GameNight.PlayWindow).Should().BeTrue();
    }

    [Fact]
    public void PlayWindows_ShouldBeTwelveHours()
    {
        GameNight.PlayWindow.Should().Be(TimeSpan.FromHours(12));
        GameNight.PlayOpensBefore.Should().Be(TimeSpan.FromHours(12));
    }

    [Fact]
    public void Create_ShouldStoreTheValues_AndGiveEveryNightItsOwnLink()
    {
        var gameNight = GameNight.Create("Night", "Bring snacks", Start, 2, 3);
        var other = GameNight.Create("Night", "Bring snacks", Start, 2, 3);

        gameNight.Title.Should().Be("Night");
        gameNight.Notes.Should().Be("Bring snacks");
        gameNight.StartDate.Should().Be(Start);
        gameNight.HostId.Should().Be(2);
        gameNight.LocationId.Should().Be(3);
        gameNight.LinkId.Should().NotBe(Guid.Empty).And.NotBe(other.LinkId);
    }

    [Fact]
    public void Create_ShouldTreatMissingNotesAsEmpty()
    {
        GameNight.Create("Night", null!, Start, 1, 1).Notes.Should().BeEmpty();
    }

    public static TheoryData<string, DateTime, int, int> InvalidNights => new()
    {
        { "", Start, 1, 1 },
        { "  ", Start, 1, 1 },
        { "Night", default, 1, 1 },
        { "Night", Start, 0, 1 },
        { "Night", Start, -1, 1 },
        { "Night", Start, 1, 0 },
        { "Night", Start, 1, -1 }
    };

    [Theory]
    [MemberData(nameof(InvalidNights))]
    public void Create_ShouldRejectInvalidValues(string title, DateTime start, int hostId, int locationId)
    {
        var act = () => GameNight.Create(title, "", start, hostId, locationId);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(nameof(InvalidNights))]
    public void Update_ShouldRejectInvalidValues(string title, DateTime start, int hostId, int locationId)
    {
        var gameNight = CreateNight();

        var act = () => gameNight.Update(title, "", start, hostId, locationId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ShouldReplaceTheValues_AndKeepTheLink()
    {
        var gameNight = CreateNight();
        var linkId = gameNight.LinkId;

        gameNight.Update("Renamed", null!, Start.AddDays(1), 4, 5);

        gameNight.Title.Should().Be("Renamed");
        gameNight.Notes.Should().BeEmpty();
        gameNight.StartDate.Should().Be(Start.AddDays(1));
        gameNight.HostId.Should().Be(4);
        gameNight.LocationId.Should().Be(5);
        gameNight.LinkId.Should().Be(linkId);
    }

    [Fact]
    public void SetInvitedPlayers_ShouldReplaceTheList_AndDropDuplicatePlayers()
    {
        var gameNight = CreateNight();
        gameNight.SetInvitedPlayers([GameNightRsvp.Create(9, GameNightRsvpState.Pending)]);
        var first = GameNightRsvp.Create(1, GameNightRsvpState.Accepted);

        gameNight.SetInvitedPlayers([first, GameNightRsvp.Create(1, GameNightRsvpState.Declined), GameNightRsvp.Create(2, GameNightRsvpState.Pending)]);

        gameNight.InvitedPlayers.Select(x => x.PlayerId).Should().Equal(1, 2);
        gameNight.InvitedPlayers.First().Should().BeSameAs(first);
    }

    [Fact]
    public void RemoveInvitedPlayers_ShouldRemoveOnlyTheGivenRsvps()
    {
        var gameNight = CreateNight();
        var stays = GameNightRsvp.Create(1, GameNightRsvpState.Pending);
        var goes = GameNightRsvp.Create(2, GameNightRsvpState.Pending);
        gameNight.SetInvitedPlayers([stays, goes]);

        gameNight.RemoveInvitedPlayers([goes]);

        gameNight.InvitedPlayers.Should().ContainSingle().Which.Should().BeSameAs(stays);
    }

    [Fact]
    public void AddInvitedPlayers_ShouldAddNewPlayersAsPending_AndLeaveExistingAnswersAlone()
    {
        var gameNight = CreateNight();
        gameNight.SetInvitedPlayers([GameNightRsvp.Create(1, GameNightRsvpState.Declined)]);

        gameNight.AddInvitedPlayers([1, 2, 2]);

        gameNight.InvitedPlayers.Select(x => (x.PlayerId, x.State)).Should().Equal((1, GameNightRsvpState.Declined), (2, GameNightRsvpState.Pending));
    }

    [Fact]
    public void SetSuggestedGames_ShouldReplaceTheList_AndDropDuplicateGames()
    {
        var gameNight = CreateNight();
        gameNight.SetSuggestedGames([new Game("Old") { Id = 9 }]);

        gameNight.SetSuggestedGames([new Game("Root") { Id = 1 }, new Game("Root again") { Id = 1 }, new Game("Catan") { Id = 2 }]);

        gameNight.SuggestedGames.Select(x => x.Title).Should().Equal("Root", "Catan");
    }

    [Fact]
    public void AddSuggestedGame_ShouldAddAGameOnce()
    {
        var gameNight = CreateNight();

        gameNight.AddSuggestedGame(new Game("Root") { Id = 1 }).Should().BeTrue();
        gameNight.AddSuggestedGame(new Game("Root") { Id = 1 }).Should().BeFalse();

        gameNight.SuggestedGames.Should().ContainSingle();
    }

    [Fact]
    public void AddSuggestedGame_ShouldRejectAMissingGame()
    {
        var act = () => CreateNight().AddSuggestedGame(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveSuggestedGame_ShouldSayWhetherItRemovedAGame()
    {
        var gameNight = CreateNight();
        gameNight.SetSuggestedGames([new Game("Root") { Id = 1 }]);

        gameNight.RemoveSuggestedGame(2).Should().BeFalse();
        gameNight.SuggestedGames.Should().ContainSingle();
        gameNight.RemoveSuggestedGame(1).Should().BeTrue();
        gameNight.SuggestedGames.Should().BeEmpty();
    }

    [Fact]
    public void GetPlayedPlayerIds_ShouldCombineSessionsAndRunningGames_WithoutDuplicates()
    {
        var gameNight = CreateNight();
        gameNight.Sessions.Add(CreateSession(30, (2, true), (3, false)));
        gameNight.RunningGames.Add(RunningGame.Create(1, 1, Start, [3, 4]));

        gameNight.GetPlayedPlayerIds().Should().Equal(2, 3, 4);
    }

    [Fact]
    public void SetAttendance_ShouldFlipAnExistingOverride_WhenSomeoneIsNoLongerThere()
    {
        var gameNight = CreateNight();
        gameNight.SetInvitedPlayers([GameNightRsvp.Create(5, GameNightRsvpState.Accepted)]);
        gameNight.SetAttendance([1, 5]);
        var entry = gameNight.Attendance.Single();
        entry.Present.Should().BeTrue();

        gameNight.SetAttendance([1]);

        gameNight.Attendance.Should().ContainSingle().Which.Should().BeSameAs(entry);
        entry.PlayerId.Should().Be(5);
        entry.Present.Should().BeFalse();
    }

    [Fact]
    public void SetAttendance_ShouldDropOverridesThatAreNoLongerNeeded()
    {
        var gameNight = CreateNight();
        gameNight.SetAttendance([1, 5]);

        gameNight.SetAttendance([1]);

        gameNight.Attendance.Should().BeEmpty();
    }

    [Fact]
    public void SetAttendance_ShouldNotMarkAPendingInviteeAsAbsent()
    {
        var gameNight = CreateNight();
        gameNight.SetInvitedPlayers([GameNightRsvp.Create(5, GameNightRsvpState.Pending)]);

        gameNight.SetAttendance([1]);

        gameNight.Attendance.Should().BeEmpty();
    }

    [Fact]
    public void GetAttendeeIds_ShouldStartWithTheHost_AndApplyTheOverrides()
    {
        var gameNight = CreateNight();
        gameNight.Sessions.Add(CreateSession(30, (3, true)));
        gameNight.SetAttendance([1, 3, 2]);

        gameNight.GetAttendeeIds().Should().Equal(1, 2, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GameNightAttendance_ShouldRejectAnInvalidPlayer(int playerId)
    {
        var act = () => GameNightAttendance.Create(playerId, true);

        act.Should().Throw<ArgumentException>();
    }
}
