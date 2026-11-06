using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Sessions;

public class SessionDtoExtensionsTests
{
    private static readonly DateTime Start = new(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ToDto_ShouldMapEveryField()
    {
        var session = new Session(3, Start, Start.AddMinutes(95), "Fun") { Id = 11 };
        session.SetLocation(new Location("Kitchen") { Id = 5 });
        session.SetGameNight(9);
        session.AddPlayerSession(4, 15, true, true);
        session.AddExpansion(new Expansion("Seafarers", 100, 3) { Id = 8 });

        var dto = session.ToDto();

        dto.Id.Should().Be(11);
        dto.Comment.Should().Be("Fun");
        dto.GameId.Should().Be(3);
        dto.IsCoop.Should().BeFalse();
        dto.TeamWon.Should().BeNull();
        dto.TeamScore.Should().BeNull();
        dto.Start.Should().Be(Start);
        dto.End.Should().Be(Start.AddMinutes(95));
        dto.Minutes.Should().Be(95);
        dto.LocationId.Should().Be(5);
        dto.GameNightId.Should().Be(9);
        dto.PlayerSessions.Should().ContainSingle(ps => ps.PlayerId == 4 && ps.Score == 15 && ps.FirstPlay && ps.Won);
        dto.Expansions.Should().ContainSingle(e => e.Id == 8);
    }

    [Fact]
    public void ToDto_ShouldMapTheTeamResult_WhenCoop()
    {
        var session = new Session(3, Start, Start.AddMinutes(30), string.Empty);
        session.MarkAsCoop(false, 18);

        var dto = session.ToDto();

        dto.IsCoop.Should().BeTrue();
        dto.TeamWon.Should().BeFalse();
        dto.TeamScore.Should().Be(18);
    }

    [Fact]
    public void ToListDto_ShouldMapEverySessionInOrder()
    {
        var sessions = new List<Session>
        {
            new(1, Start, Start.AddMinutes(30), "first") { Id = 2 },
            new(1, Start, Start.AddMinutes(30), "second") { Id = 1 }
        };

        var dtos = sessions.ToListDto();

        dtos.Select(x => x.Id).Should().Equal(2, 1);
    }

    [Fact]
    public void ToListDto_ShouldReturnAnEmptyList_WhenThereAreNoSessions()
    {
        new List<Session>().ToListDto().Should().BeEmpty();
    }
}
