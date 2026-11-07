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
    private static readonly DateTime Now = new(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly EphemeralDataProtectionProvider _provider = new();
    private readonly ProfileImageTicketService _service;

    public ProfileImageTicketServiceTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _service = new ProfileImageTicketService(
            _provider,
            Options.Create(new JwtOptions { RefreshTokenExpiryDays = 7 }),
            _clock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _clock.VerifyNoOtherCalls();
    }

    private string ProtectedPayload(string payload) => _provider.CreateProtector(Purpose).Protect(payload);

    [Fact]
    public void Issue_ShouldProduceATicketForTheUser_ThatIsValidForTheRefreshTokenLifetime()
    {
        var ticket = _service.Issue("user-1");

        _service.Lifetime.Should().Be(TimeSpan.FromDays(7));
        _service.ReadUserId(ticket).Should().Be("user-1");
        _clock.Setup(x => x.UtcNow).Returns(Now.AddDays(7).AddTicks(-1));
        _service.ReadUserId(ticket).Should().Be("user-1");
        _clock.Setup(x => x.UtcNow).Returns(Now.AddDays(7));
        _service.ReadUserId(ticket).Should().BeNull();
        _clock.Setup(x => x.UtcNow).Returns(Now.AddDays(7).AddTicks(1));
        _service.ReadUserId(ticket).Should().BeNull();
        _clock.Verify(x => x.UtcNow, Times.Exactly(5));
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Issue_ShouldPutTheExpiryAndTheUserInTheProtectedPayload()
    {
        var ticket = _service.Issue("user-1");

        _provider.CreateProtector(Purpose).Unprotect(ticket).Should().Be($"{Now.AddDays(7).Ticks}|user-1");
        _clock.Verify(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ReadUserId_ShouldKeepASeparatorInTheUserId()
    {
        var ticket = ProtectedPayload($"{Now.AddHours(1).Ticks}|user|1");

        _service.ReadUserId(ticket).Should().Be("user|1");
        _clock.Verify(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ticket")]
    public void ReadUserId_ShouldRejectGarbage(string? ticket)
    {
        _service.ReadUserId(ticket).Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("639999999999999999")]
    [InlineData("639999999999999999|")]
    [InlineData("soon|user-1")]
    [InlineData("-1|user-1")]
    public void ReadUserId_ShouldRejectAProtectedPayloadWithoutAnExpiryAndAUser(string payload)
    {
        var ticket = ProtectedPayload(payload);

        _service.ReadUserId(ticket).Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ReadUserId_ShouldRejectATicketFromAnotherKeyRing()
    {
        var other = new ProfileImageTicketService(new EphemeralDataProtectionProvider(), Options.Create(new JwtOptions()), _clock.Object);

        _service.ReadUserId(other.Issue("user-1")).Should().BeNull();
        _clock.Verify(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ReadUserId_ShouldRejectATicketForAnotherPurpose()
    {
        var ticket = _provider.CreateProtector("BoardGameTracker.ProfileImages.v1").Protect($"{Now.AddHours(1).Ticks}|user-1");

        _service.ReadUserId(ticket).Should().BeNull();
        VerifyNoOtherCalls();
    }
}
