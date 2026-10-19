using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Sessions.Specifications;

public class ScoredSessionsByGameSpecTests
{
    [Fact]
    public void Evaluate_ShouldKeepOnlySessionsOfTheGameWithAtLeastOneScore()
    {
        var start = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var scored = new Session(1, start, start.AddHours(1), string.Empty) { Id = 1 };
        scored.AddPlayerSession(1, 72, false, true);
        scored.AddPlayerSession(2, null, false, false);
        var unscored = new Session(1, start, start.AddHours(1), string.Empty) { Id = 2 };
        unscored.AddPlayerSession(1, null, false, true);
        var otherGame = new Session(2, start, start.AddHours(1), string.Empty) { Id = 3 };
        otherGame.AddPlayerSession(1, 40, false, true);
        var spec = new ScoredSessionsByGameSpec(1);

        var result = spec.Evaluate([scored, unscored, otherGame]).ToList();

        result.Select(x => x.Id).Should().Equal(1);
        spec.AsNoTracking.Should().BeTrue();
    }
}
