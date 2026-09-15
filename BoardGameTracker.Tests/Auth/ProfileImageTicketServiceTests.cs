using System;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Common;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class ProfileImageTicketServiceTests
{
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly ProfileImageTicketService _service;

    public ProfileImageTicketServiceTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
        _service = new ProfileImageTicketService(
            new EphemeralDataProtectionProvider(),
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }),
            _clock.Object);
    }

    [Fact]
    public void Issue_ShouldProduceATicket_ThatIsValidForTheRefreshTokenLifetime()
    {
        var ticket = _service.Issue();

        _service.Lifetime.Should().Be(TimeSpan.FromDays(7));
        _service.IsValid(ticket).Should().BeTrue();
        _clock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 9, 19, 9, 59, 0, DateTimeKind.Utc));
        _service.IsValid(ticket).Should().BeTrue();
        _clock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 9, 19, 10, 0, 1, DateTimeKind.Utc));
        _service.IsValid(ticket).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ticket")]
    public void IsValid_ShouldRejectGarbage(string? ticket)
    {
        _service.IsValid(ticket).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldRejectATicketFromAnotherKeyRing()
    {
        var other = new ProfileImageTicketService(new EphemeralDataProtectionProvider(), Options.Create(new JwtOptions()), _clock.Object);

        _service.IsValid(other.Issue()).Should().BeFalse();
    }
}
