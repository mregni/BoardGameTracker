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
    public void RsvpByIdSpec_ShouldIncludePlayer()
    {
        new RsvpByIdSpec(1).IncludeExpressions.Should().NotBeEmpty();
    }

    [Fact]
    public void RsvpByPlayerAndGameNightSpec_ShouldMatchOnBothPlayerAndGameNight()
    {
        // GameNightId is not publicly settable, so it defaults to 0 here — the spec must AND both predicates.
        var rsvp = GameNightRsvp.Create(5, GameNightRsvpState.Pending);

        new RsvpByPlayerAndGameNightSpec(5, 0).IsSatisfiedBy(rsvp).Should().BeTrue();
        new RsvpByPlayerAndGameNightSpec(9, 0).IsSatisfiedBy(rsvp).Should().BeFalse();
        new RsvpByPlayerAndGameNightSpec(5, 99).IsSatisfiedBy(rsvp).Should().BeFalse();
    }
}
