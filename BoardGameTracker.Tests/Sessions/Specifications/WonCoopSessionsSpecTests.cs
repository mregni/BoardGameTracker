using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Sessions.Specifications;

public class WonCoopSessionsSpecTests
{
    [Fact]
    public void Evaluate_ShouldKeepOnlyCoopSessionsTheTeamWon()
    {
        var start = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var competitive = new Session(1, start, start.AddHours(1), string.Empty) { Id = 1 };
        var coopWon = new Session(1, start, start.AddHours(1), string.Empty) { Id = 2 };
        coopWon.MarkAsCoop(true, null);
        var coopLost = new Session(1, start, start.AddHours(1), string.Empty) { Id = 3 };
        coopLost.MarkAsCoop(false, null);
        var spec = new WonCoopSessionsSpec();

        var result = spec.Evaluate([competitive, coopWon, coopLost]).ToList();

        result.Select(x => x.Id).Should().Equal(2);
        spec.AsNoTracking.Should().BeTrue();
    }
}
