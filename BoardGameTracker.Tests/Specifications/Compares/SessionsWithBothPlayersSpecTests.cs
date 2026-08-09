using System;
using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Compares.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.Compares;

public class SessionsWithBothPlayersSpecTests
{
    private static Session SessionWith(int id, params int[] playerIds)
    {
        var session = new Session(1, new DateTime(2030, 1, id), new DateTime(2030, 1, id).AddHours(1), string.Empty) { Id = id };
        foreach (var playerId in playerIds)
        {
            session.AddPlayerSession(playerId, null, false, false);
        }

        return session;
    }

    [Fact]
    public void Evaluate_ShouldReturnOnlySessionsContainingBothPlayers()
    {
        var both = SessionWith(1, 5, 6);
        var onlyOne = SessionWith(2, 5);
        var otherPair = SessionWith(3, 6, 7);
        var spec = new SessionsWithBothPlayersSpec(5, 6);

        var result = spec.Evaluate(new[] { both, onlyOne, otherPair }).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(1);
        spec.AsNoTracking.Should().BeTrue();
    }
}
