using System;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Sessions;

public class SessionRepositorySpecsTests
{
    private static Session SessionWithPlayer(int id, int gameId, int playerId, bool won)
    {
        var session = new Session(gameId, new DateTime(2030, 1, id), new DateTime(2030, 1, id).AddHours(1), string.Empty) { Id = id };
        session.AddPlayerSession(playerId, null, false, won);
        return session;
    }

    [Fact]
    public void SessionsByPlayerSpec_ShouldFilterByPlayer_Track_AndInclude()
    {
        var forPlayer5 = SessionWithPlayer(1, 1, 5, won: true);
        var forPlayer6 = SessionWithPlayer(2, 1, 6, won: true);
        var spec = new SessionsByPlayerSpec(5);

        var result = spec.Evaluate(new[] { forPlayer5, forPlayer6 }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
        spec.AsNoTracking.Should().BeFalse();
        spec.IncludeExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void SessionsByPlayerSpec_ShouldApplyWonFilter_WhenProvided()
    {
        var won = SessionWithPlayer(1, 1, 5, won: true);
        var lost = SessionWithPlayer(2, 1, 5, won: false);

        var result = new SessionsByPlayerSpec(5, won: true).Evaluate(new[] { won, lost }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void SessionsByPlayerAndGameSpec_ShouldFilterByBoth()
    {
        var match = SessionWithPlayer(1, 7, 5, won: false);
        var wrongGame = SessionWithPlayer(2, 9, 5, won: false);

        var result = new SessionsByPlayerAndGameSpec(5, 7).Evaluate(new[] { match, wrongGame }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void WonSessionsByPlayerAndGameSpec_ShouldMatchOnlyThatPlayersWinsOfThatGame()
    {
        var wonRightGame = SessionWithPlayer(1, 7, 5, won: true);
        var lostRightGame = SessionWithPlayer(2, 7, 5, won: false);
        var wonWrongGame = SessionWithPlayer(3, 9, 5, won: true);
        var wonWrongPlayer = SessionWithPlayer(4, 7, 6, won: true);

        var result = new WonSessionsByPlayerAndGameSpec(5, 7)
            .Evaluate(new[] { wonRightGame, lostRightGame, wonWrongGame, wonWrongPlayer })
            .ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void SessionByIdWithDetailsSpec_ShouldMatchId_Include_AndTrack()
    {
        var session = SessionWithPlayer(3, 1, 5, won: false);
        var spec = new SessionByIdWithDetailsSpec(3);

        spec.IsSatisfiedBy(session).Should().BeTrue();
        new SessionByIdWithDetailsSpec(4).IsSatisfiedBy(session).Should().BeFalse();
        spec.IncludeExpressions.Should().HaveCount(2);
        spec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void RecentSessionsSpec_ShouldOrderDescending_Take_AndNotTrack()
    {
        var day1 = SessionWithPlayer(1, 1, 5, won: false);
        var day2 = SessionWithPlayer(2, 1, 5, won: false);
        var day3 = SessionWithPlayer(3, 1, 5, won: false);
        var spec = new RecentSessionsSpec(2);

        var result = spec.Evaluate(new[] { day1, day2, day3 }).ToList();

        result.Select(x => x.Id).Should().ContainInOrder(3, 2);
        result.Should().HaveCount(2);
        spec.AsNoTracking.Should().BeTrue();
    }
}
