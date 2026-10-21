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
    private const string Purpose = "BoardGameTracker.ProfileImages.v2";
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly EphemeralDataProtectionProvider _provider = new();
    private readonly ProfileImageTicketService _service;

    public ProfileImageTicketServiceTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc));
        _service = new ProfileImageTicketService(
            _provider,
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }),
            _clock.Object);
    }

    [Fact]
    public void Issue_ShouldProduceATicketForTheUser_ThatIsValidForTheRefreshTokenLifetime()
    {
        var ticket = _service.Issue("user-1");

        _service.Lifetime.Should().Be(TimeSpan.FromDays(7));
        _service.ReadUserId(ticket).Should().Be("user-1");
        _clock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 9, 19, 9, 59, 0, DateTimeKind.Utc));
        _service.ReadUserId(ticket).Should().Be("user-1");
        _clock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 9, 19, 10, 0, 1, DateTimeKind.Utc));
        _service.ReadUserId(ticket).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ticket")]
    public void ReadUserId_ShouldRejectGarbage(string? ticket)
    {
        _service.ReadUserId(ticket).Should().BeNull();
    }

    [Theory]
    [InlineData("639999999999999999")]
    [InlineData("639999999999999999|")]
    [InlineData("soon|user-1")]
    public void ReadUserId_ShouldRejectAProtectedPayloadWithoutAnExpiryAndAUser(string payload)
    {
        var ticket = _provider.CreateProtector(Purpose).Protect(payload);

        _service.ReadUserId(ticket).Should().BeNull();
    }

    [Fact]
    public void ReadUserId_ShouldRejectATicketFromAnotherKeyRing()
    {
        var other = new ProfileImageTicketService(new EphemeralDataProtectionProvider(), Options.Create(new JwtOptions()), _clock.Object);

        _service.ReadUserId(other.Issue("user-1")).Should().BeNull();
    }
}
