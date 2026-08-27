using System;
using System.Collections.Generic;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Sessions;

public class SessionQuerySpecsTests
{
    private static Session SessionFor(int id, int gameId, DateTime start, TimeSpan? duration = null)
    {
        var session = new Session(gameId, start, start.Add(duration ?? TimeSpan.FromHours(1)), string.Empty) { Id = id };
        return session;
    }

    [Fact]
    public void SessionsByGameSpec_ShouldFilterByGame_OrderDescending_AndNotTrack()
    {
        var older = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var newer = SessionFor(2, 1, new DateTime(2030, 1, 5));
        var otherGame = SessionFor(3, 2, new DateTime(2030, 1, 9));
        var spec = new SessionsByGameSpec(1);

        var result = spec.Evaluate(new[] { older, newer, otherGame }).ToList();

        result.Select(x => x.Id).Should().ContainInOrder(2, 1);
        result.Should().NotContain(x => x.Id == 3);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void SessionsByGameSpec_ShouldApplyTake_WhenCountGiven()
    {
        var older = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var newer = SessionFor(2, 1, new DateTime(2030, 1, 5));

        var result = new SessionsByGameSpec(1, 1).Evaluate(new[] { older, newer }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [Fact]
    public void SessionsByGameSinceSpec_ShouldFilterByGameAndCutoff_OrderAscending_AndNotTrack()
    {
        var cutoff = new DateTime(2030, 1, 4);
        var old = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var atCutoff = SessionFor(2, 1, cutoff);
        var recent = SessionFor(3, 1, new DateTime(2030, 1, 6));
        var earlierRecent = SessionFor(4, 1, new DateTime(2030, 1, 5));
        var otherGame = SessionFor(5, 2, new DateTime(2030, 1, 6));
        var spec = new SessionsByGameSinceSpec(1, cutoff);

        var result = spec.Evaluate(new[] { old, atCutoff, recent, earlierRecent, otherGame }).ToList();

        result.Select(x => x.Id).Should().Equal(4, 3);
        spec.IncludeExpressions.Should().HaveCount(1);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void SessionsByPlayerRecentFirstSpec_ShouldFilterByPlayer_Include_AndNotTrack()
    {
        var withPlayer5 = SessionFor(1, 1, new DateTime(2030, 1, 1));
        withPlayer5.AddPlayerSession(5, null, false, false);
        var withPlayer6 = SessionFor(2, 1, new DateTime(2030, 1, 2));
        withPlayer6.AddPlayerSession(6, null, false, false);
        var spec = new SessionsByPlayerRecentFirstSpec(5);

        var result = spec.Evaluate(new[] { withPlayer5, withPlayer6 }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
        spec.IncludeExpressions.Should().HaveCount(1);
        spec.AsNoTracking.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, new[] { 3, 2, 1 })]
    [InlineData(2, new[] { 3, 2 })]
    public void SessionsByPlayerRecentFirstSpec_ShouldReturnRecentFirst_LimitedToCountWhenProvided(int? count, int[] expectedIds)
    {
        var sessions = PlayerSessionsAcrossThreeDays(5);

        var result = new SessionsByPlayerRecentFirstSpec(5, count).Evaluate(sessions).ToList();

        result.Select(x => x.Id).Should().Equal(expectedIds);
    }

    private static Session[] PlayerSessionsAcrossThreeDays(int playerId)
    {
        var first = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var second = SessionFor(2, 1, new DateTime(2030, 1, 2));
        var third = SessionFor(3, 1, new DateTime(2030, 1, 3));

        foreach (var session in new[] { first, second, third })
        {
            session.AddPlayerSession(playerId, null, false, false);
        }

        return [first, second, third];
    }

    [Fact]
    public void LastPlayedDateSpec_ShouldProjectMostRecentStartOfRequestedGame()
    {
        var day1 = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var day3 = SessionFor(2, 1, new DateTime(2030, 1, 3));
        var otherGameLater = SessionFor(3, 2, new DateTime(2030, 1, 9));
        var spec = new LastPlayedDateSpec(1);

        var result = spec.Evaluate(new[] { day1, day3, otherGameLater }).ToList();

        result.Should().Equal(new DateTime(2030, 1, 3), new DateTime(2030, 1, 1));
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void LastPlayedDateSpec_ShouldReturnNothing_WhenGameHasNoSessions()
    {
        var otherGame = SessionFor(1, 2, new DateTime(2030, 1, 1));

        new LastPlayedDateSpec(1).Evaluate(new[] { otherGame }).Should().BeEmpty();
    }
}
