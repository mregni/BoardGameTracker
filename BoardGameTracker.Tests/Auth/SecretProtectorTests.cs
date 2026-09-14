using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class SecretProtectorTests
{
    private readonly SecretProtector _protector = new(new EphemeralDataProtectionProvider(), Mock.Of<ILogger<SecretProtector>>());

    [Fact]
    public void Protect_ShouldProduceAPrefixedCiphertextThatRoundTrips()
    {
        var stored = _protector.Protect("client-secret");

        stored.Should().StartWith("dp1:");
        stored.Should().NotContain("client-secret");
        _protector.Unprotect(stored).Should().Be("client-secret");
    }

    [Fact]
    public void Unprotect_ShouldReturnLegacyPlaintextUnchanged()
    {
        _protector.Unprotect("stored-before-encryption").Should().Be("stored-before-encryption");
    }
}
