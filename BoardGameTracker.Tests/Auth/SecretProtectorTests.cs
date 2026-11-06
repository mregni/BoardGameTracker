using System.Security.Cryptography;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class SecretProtectorTests
{
    private const string Purpose = "BoardGameTracker.Secrets.v1";

    private readonly EphemeralDataProtectionProvider _provider = new();
    private readonly Mock<ILogger<SecretProtector>> _loggerMock = new();
    private readonly SecretProtector _protector;

    public SecretProtectorTests()
    {
        _protector = new SecretProtector(_provider, _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Protect_ShouldProduceAPrefixedCiphertextThatRoundTrips()
    {
        var stored = _protector.Protect("client-secret");

        stored.Should().StartWith("dp1:");
        stored.Should().NotContain("client-secret");
        _protector.Unprotect(stored).Should().Be("client-secret");
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Protect_ShouldKeepItsPurpose_SoThatStoredSecretsStayReadable()
    {
        var stored = _protector.Protect("client-secret");

        _provider.CreateProtector(Purpose).Unprotect(stored["dp1:".Length..]).Should().Be("client-secret");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("stored-before-encryption")]
    [InlineData("DP1:looks-like-the-prefix-in-another-case")]
    [InlineData("")]
    public void Unprotect_ShouldReturnLegacyPlaintextUnchanged(string stored)
    {
        _protector.Unprotect(stored).Should().Be(stored);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Unprotect_ShouldLogAndRethrow_WhenTheKeysWereLost()
    {
        var stored = new SecretProtector(new EphemeralDataProtectionProvider(), _loggerMock.Object).Protect("client-secret");

        var act = () => _protector.Unprotect(stored);

        var thrown = act.Should().Throw<CryptographicException>().Which;
        _loggerMock.VerifyLog(
            LogLevel.Error,
            "A stored secret could not be decrypted; the data-protection keys may have been lost. Re-enter the secret in the settings",
            thrown);
        VerifyNoOtherCalls();
    }
}
