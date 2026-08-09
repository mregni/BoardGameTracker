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
    public void SessionsByGameSinceSpec_ShouldFilterByGameAndCutoff()
    {
        var cutoff = new DateTime(2030, 1, 4);
        var old = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var recent = SessionFor(2, 1, new DateTime(2030, 1, 6));

        var result = new SessionsByGameSinceSpec(1, cutoff).Evaluate(new[] { old, recent }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [Fact]
    public void SessionsByPlayerRecentFirstSpec_ShouldFilterByPlayer()
    {
        var withPlayer5 = SessionFor(1, 1, new DateTime(2030, 1, 1));
        withPlayer5.AddPlayerSession(5, null, false, false);
        var withPlayer6 = SessionFor(2, 1, new DateTime(2030, 1, 2));
        withPlayer6.AddPlayerSession(6, null, false, false);

        var result = new SessionsByPlayerRecentFirstSpec(5).Evaluate(new[] { withPlayer5, withPlayer6 }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void SessionsByGamePagedSpec_ShouldSkipAndTakeInDescendingOrder()
    {
        var day1 = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var day2 = SessionFor(2, 1, new DateTime(2030, 1, 2));
        var day3 = SessionFor(3, 1, new DateTime(2030, 1, 3));

        var result = new SessionsByGamePagedSpec(1, skip: 1, take: 1).Evaluate(new[] { day1, day2, day3 }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [Fact]
    public void LastPlayedDateSpec_ShouldProjectMostRecentStart()
    {
        var day1 = SessionFor(1, 1, new DateTime(2030, 1, 1));
        var day3 = SessionFor(2, 1, new DateTime(2030, 1, 3));

        var result = new LastPlayedDateSpec(1).Evaluate(new[] { day1, day3 }).First();

        result.Should().Be(new DateTime(2030, 1, 3));
    }

    [Fact]
    public void ShortestAndLongestPlayIdSpec_ShouldProjectIdByDuration()
    {
        var shortPlay = SessionFor(1, 1, new DateTime(2030, 1, 1), TimeSpan.FromHours(1));
        var longPlay = SessionFor(2, 1, new DateTime(2030, 1, 2), TimeSpan.FromHours(3));
        var games = new[] { shortPlay, longPlay };

        new ShortestPlayIdSpec(1).Evaluate(games).First().Should().Be(1);
        new LongestPlayIdSpec(1).Evaluate(games).First().Should().Be(2);
    }
}
