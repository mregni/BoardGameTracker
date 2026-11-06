using BoardGameTracker.Common;
using BoardGameTracker.Core.Settings;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Settings;

public class IntegrationKeyProtectorTests
{
    private const string Plaintext = "plain-integration-key";

    private readonly Mock<ILogger<IntegrationKeyProtector>> _loggerMock = new();
    private readonly IntegrationKeyProtector _protector;

    public IntegrationKeyProtectorTests()
    {
        _protector = new IntegrationKeyProtector(new EphemeralDataProtectionProvider(), _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
    }

    private void VerifyUndecryptableWarning(string setting)
    {
        _loggerMock.VerifyLog(LogLevel.Warning, $"The stored {setting} could not be decrypted; the data-protection keys may have been lost or rotated. The integration is treated as not configured until the key is entered again in the settings");
    }

    [Fact]
    public void Protect_ShouldReturnAMarkedValue_ThatDoesNotContainThePlaintext()
    {
        var stored = _protector.Protect(Plaintext);

        stored.Should().StartWith("dp1:");
        stored.Should().NotContain(Plaintext);
        _protector.IsProtected(stored).Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Reveal_ShouldReturnThePlaintext_ForAProtectedValue()
    {
        var stored = _protector.Protect(Plaintext);

        var result = _protector.Reveal(Constants.BggConfig.ApiKey, stored);

        result.Should().Be(Plaintext);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Reveal_ShouldPassLegacyPlaintextThrough()
    {
        var result = _protector.Reveal(Constants.BggConfig.ApiKey, Plaintext);

        result.Should().Be(Plaintext);
        _protector.IsProtected(Plaintext).Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reveal_ShouldReturnNull_WhenNothingIsStored(string? stored)
    {
        var result = _protector.Reveal(Constants.BggConfig.ApiKey, stored);

        result.Should().BeNull();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Reveal_ShouldReturnNull_AndWarnWithoutTheValue_WhenTheKeysChanged()
    {
        var foreign = new IntegrationKeyProtector(new EphemeralDataProtectionProvider(), NullLogger<IntegrationKeyProtector>.Instance);
        var stored = foreign.Protect(Plaintext);

        var result = _protector.Reveal(Constants.ChangeDetectionConfig.ApiKey, stored);

        result.Should().BeNull();
        VerifyUndecryptableWarning(Constants.ChangeDetectionConfig.ApiKey);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("dp1:a")]
    [InlineData("dp1:not-a-valid-payload")]
    public void Reveal_ShouldReturnNull_AndWarn_ForAMalformedProtectedValue(string stored)
    {
        var result = _protector.Reveal(Constants.BggConfig.ApiKey, stored);

        result.Should().BeNull();
        VerifyUndecryptableWarning(Constants.BggConfig.ApiKey);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Reveal_ShouldNotDecrypt_ValuesProtectedForADifferentPurpose()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = new IntegrationKeyProtector(provider, _loggerMock.Object);
        var otherPurpose = "dp1:" + provider.CreateProtector("BoardGameTracker.Secrets.v1").Protect(Plaintext);

        var result = protector.Reveal(Constants.BggConfig.ApiKey, otherPurpose);

        result.Should().BeNull();
        VerifyUndecryptableWarning(Constants.BggConfig.ApiKey);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void IsProtected_ShouldOnlyAcceptTheExactCasedMarkerAtTheStart()
    {
        _protector.IsProtected("dp1:value").Should().BeTrue();
        _protector.IsProtected("DP1:value").Should().BeFalse();
        _protector.IsProtected("key-dp1:value").Should().BeFalse();
        VerifyNoOtherCalls();
    }
}
