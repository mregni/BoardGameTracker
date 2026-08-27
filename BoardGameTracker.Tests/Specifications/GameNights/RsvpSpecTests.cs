using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Specifications.GameNights;

public class RsvpSpecTests
{
    [Fact]
    public void RsvpByIdSpec_ShouldMatchOnlyTheRequestedId()
    {
        var rsvp = GameNightRsvp.Create(1, GameNightRsvpState.Pending);
        rsvp.Id = 3;

        new RsvpByIdSpec(3).IsSatisfiedBy(rsvp).Should().BeTrue();
        new RsvpByIdSpec(4).IsSatisfiedBy(rsvp).Should().BeFalse();
    }

    [Fact]
    public void RsvpByIdSpec_ShouldIncludePlayerAndGameNightWithHost()
    {
        new RsvpByIdSpec(1).IncludeExpressions.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(5, 0, true)]
    [InlineData(9, 0, false)]
    [InlineData(5, 99, false)]
    [InlineData(9, 99, false)]
    public void RsvpByPlayerAndGameNightSpec_ShouldMatchOnBothPlayerAndGameNight(int playerId, int gameNightId, bool expected)
    {
        var rsvp = GameNightRsvp.Create(5, GameNightRsvpState.Pending);

        new RsvpByPlayerAndGameNightSpec(playerId, gameNightId).IsSatisfiedBy(rsvp).Should().Be(expected);
    }

    [Fact]
    public void RsvpByPlayerAndGameNightSpec_ShouldIncludePlayerAndGameNightWithHost()
    {
        new RsvpByPlayerAndGameNightSpec(1, 1).IncludeExpressions.Should().HaveCount(3);
    }
}
