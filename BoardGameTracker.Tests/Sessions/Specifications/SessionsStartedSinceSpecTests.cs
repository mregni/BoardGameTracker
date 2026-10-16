using System.Linq;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Sessions.Specifications;

public class SessionsStartedSinceSpecTests
{
    [Fact]
    public void Evaluate_ShouldKeepSessionsStartingAtOrAfterTheGivenMoment()
    {
        var since = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var before = new Session(1, since.AddMinutes(-1), since.AddHours(1), string.Empty) { Id = 1 };
        var exactly = new Session(1, since, since.AddHours(1), string.Empty) { Id = 2 };
        var after = new Session(1, since.AddDays(3), since.AddDays(3).AddHours(1), string.Empty) { Id = 3 };
        var spec = new SessionsStartedSinceSpec(since);

        var result = spec.Evaluate([before, exactly, after]).ToList();

        result.Select(x => x.Id).Should().BeEquivalentTo([2, 3]);
        spec.AsNoTracking.Should().BeTrue();
    }
}
