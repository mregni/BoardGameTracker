using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class StartupSecretsTests
{
    private static readonly string LongValue = new('k', 48);

    [Theory]
    [InlineData("CHANGEME")]
    [InlineData("CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS")]
    [InlineData("changeme_generate_at_least_32_characters")]
    [InlineData("ChangeMe-please-use-a-real-secret-here")]
    [InlineData("  CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS")]
    public void IsPlaceholder_ShouldBeTrue_ForValuesStartingWithChangeMe(string value)
    {
        StartupSecrets.IsPlaceholder(value).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a-value-without-the-prefix-changeme")]
    [InlineData("dev-only-data-protection-key-change-me-for-real-use")]
    public void IsPlaceholder_ShouldBeFalse_ForOtherValues(string? value)
    {
        StartupSecrets.IsPlaceholder(value).Should().BeFalse();
    }

    [Fact]
    public void PlaceholderMessage_ShouldNameTheVariableAndHowToGenerateAValue()
    {
        StartupSecrets.PlaceholderMessage("JWT_SECRET").Should()
            .StartWith("JWT_SECRET still holds the CHANGEME placeholder")
            .And.Contain("openssl rand -base64 48");
    }

    [Fact]
    public void ResolveJwtSecret_ShouldReturnTheSecret_WhenItIsValid()
    {
        StartupSecrets.ResolveJwtSecret(LongValue, authEnabled: true).Should().Be(LongValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveJwtSecret_ShouldFail_WhenTheSecretIsNotSetAndAuthIsEnabled(string? secret)
    {
        var act = () => StartupSecrets.ResolveJwtSecret(secret, authEnabled: true);

        act.Should().Throw<InvalidOperationException>().WithMessage("JWT_SECRET is not set.*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ResolveJwtSecret_ShouldReturnTheInternalKey_WhenTheSecretIsNotSetAndAuthIsDisabled(string? secret)
    {
        StartupSecrets.ResolveJwtSecret(secret, authEnabled: false).Should().Be(StartupSecrets.AuthDisabledJwtSecret);
    }

    [Theory]
    [InlineData("CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS")]
    [InlineData("changeme_generate_at_least_32_characters")]
    [InlineData("CHANGEME")]
    public void ResolveJwtSecret_ShouldFail_WhenTheSecretIsThePlaceholder(string secret)
    {
        var act = () => StartupSecrets.ResolveJwtSecret(secret, authEnabled: true);

        act.Should().Throw<InvalidOperationException>().WithMessage("JWT_SECRET still holds the CHANGEME placeholder*");
    }

    [Fact]
    public void ResolveJwtSecret_ShouldFail_WhenTheSecretIsTooShort()
    {
        var act = () => StartupSecrets.ResolveJwtSecret("too-short", authEnabled: true);

        act.Should().Throw<InvalidOperationException>().WithMessage("JWT_SECRET must be at least 32 characters long, but was 9.*");
    }

    [Theory]
    [InlineData("CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS")]
    [InlineData("short")]
    public void ResolveJwtSecret_ShouldNotValidateTheSecret_WhenAuthIsDisabled(string secret)
    {
        StartupSecrets.ResolveJwtSecret(secret, authEnabled: false).Should().Be(secret);
    }
}
