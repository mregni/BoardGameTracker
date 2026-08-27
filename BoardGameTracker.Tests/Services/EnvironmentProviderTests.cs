using System;
using System.Collections.Generic;
using BoardGameTracker.Core.Configuration;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Services;

[Collection("EnvironmentVariables")]
public class EnvironmentProviderTests : IDisposable
{
    private readonly EnvironmentProvider _environmentProvider;
    private readonly Dictionary<string, string?> _originalEnvironmentVariables;

    private static readonly string[] ResetKeys =
    [
        "ASPNETCORE_ENVIRONMENT", "ENVIRONMENT", "RAG_ENABLED", "ADMIN_PASSWORD", "TRUSTED_PROXIES", "CORS_ORIGINS",
        "SWAGGER_ENABLED", "SMTP_HOST", "SMTP_PORT", "SMTP_USERNAME", "SMTP_PASSWORD", "SMTP_USE_SSL",
        "SMTP_FROM_ADDRESS", "SMTP_FROM_NAME"
    ];

    public EnvironmentProviderTests()
    {
        _environmentProvider = new EnvironmentProvider();
        _originalEnvironmentVariables = new Dictionary<string, string?>
        {
            ["PORT"] = Environment.GetEnvironmentVariable("PORT"),
            ["STATISTICS"] = Environment.GetEnvironmentVariable("STATISTICS"),
            ["STATISTICS_ENABLED"] = Environment.GetEnvironmentVariable("STATISTICS_ENABLED"),
            ["LOGLEVEL"] = Environment.GetEnvironmentVariable("LOGLEVEL"),
            ["AUTH_ENABLED"] = Environment.GetEnvironmentVariable("AUTH_ENABLED"),
            ["JWT_SECRET"] = Environment.GetEnvironmentVariable("JWT_SECRET")
        };

        foreach (var key in ResetKeys)
        {
            _originalEnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("3000", 3000)]
    [InlineData("5000", 5000)]
    [InlineData("8080", 8080)]
    [InlineData("9999", 9999)]
    [InlineData("1", 1)]
    [InlineData("65535", 65535)]
    [InlineData("0", 0)]
    [InlineData("", 7178)]
    [InlineData("   ", 7178)]
    [InlineData("abc", 7178)]
    [InlineData("8080.5", 7178)]
    [InlineData("not-a-number", 7178)]
    [InlineData("-1", 7178)]
    [InlineData(null, 7178)]
    public void Port_ShouldParseValue_OrFallBackToDefault(string? portString, int expectedPort)
    {
        Environment.SetEnvironmentVariable("PORT", portString);

        var result = _environmentProvider.Port;

        result.Should().Be(expectedPort);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("yes", false)]
    [InlineData("no", false)]
    [InlineData("1", false)]
    [InlineData("0", false)]
    [InlineData("enabled", false)]
    [InlineData("disabled", false)]
    [InlineData(null, false)]
    public void EnableStatistics_ShouldHandleCaseInsensitive_WithValidBooleanValues(string? statisticsValue, bool expected)
    {
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", statisticsValue);

        var result = _environmentProvider.StatisticsEnabled;

        result.Should().Be(expected);
    }

    [Fact]
    public void Properties_ShouldNotCacheValues_WhenEnvironmentVariablesChange()
    {
        Environment.SetEnvironmentVariable("PORT", "3000");
        var initialPort = _environmentProvider.Port;

        Environment.SetEnvironmentVariable("PORT", "8080");
        var updatedPort = _environmentProvider.Port;

        initialPort.Should().Be(3000);
        updatedPort.Should().Be(8080);
    }

    [Theory]
    [InlineData(null, null, "production")]
    [InlineData("Production", "staging", "Production")]
    [InlineData(null, "staging", "staging")]
    public void EnvironmentName_ShouldPreferAspNetCoreEnvironment_AndDefaultToProduction(string? aspNetCoreEnvironment, string? environment, string expected)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspNetCoreEnvironment);
        Environment.SetEnvironmentVariable("ENVIRONMENT", environment);

        var result = _environmentProvider.EnvironmentName;

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("development", true)]
    [InlineData("Development", true)]
    [InlineData("DEVELOPMENT", true)]
    [InlineData("dev", false)]
    [InlineData("develop", false)]
    [InlineData("production", false)]
    [InlineData("Production", false)]
    [InlineData("staging", false)]
    [InlineData("test", false)]
    [InlineData("custom", false)]
    public void IsDevelopment_ShouldBeCaseInsensitive(string? environmentName, bool expectedResult)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);

        var result = _environmentProvider.IsDevelopment;

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void IsDevelopment_ShouldReturnFalse_WhenNoEnvironmentSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ENVIRONMENT", null);

        var result = _environmentProvider.IsDevelopment;

        result.Should().BeFalse();
    }

    [Fact]
    public void AuthEnabled_ShouldReturnTrue_WhenNotSet()
    {
        Environment.SetEnvironmentVariable("AUTH_ENABLED", null);

        _environmentProvider.AuthEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    [InlineData("yes", true)]
    [InlineData("no", true)]
    [InlineData("0", true)]
    [InlineData("1", true)]
    [InlineData("something", true)]
    public void AuthEnabled_ShouldOnlyReturnFalse_WhenExplicitlySetToFalse(string value, bool expected)
    {
        Environment.SetEnvironmentVariable("AUTH_ENABLED", value);

        _environmentProvider.AuthEnabled.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("my-secret-key", "my-secret-key")]
    public void JwtSecret_ShouldReturnValue_OrNullWhenBlank(string? value, string? expected)
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", value);

        _environmentProvider.JwtSecret.Should().Be(expected);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    [InlineData("1", false)]
    [InlineData("yes", false)]
    [InlineData(null, false)]
    public void RagEnabled_ShouldOnlyBeTrue_WhenExplicitlySetToTrue(string? value, bool expected)
    {
        Environment.SetEnvironmentVariable("RAG_ENABLED", value);

        _environmentProvider.RagEnabled.Should().Be(expected);
    }

    [Fact]
    public void AdminPassword_ShouldReturnNull_WhenNotSet()
    {
        _environmentProvider.AdminPassword.Should().BeNull();
    }

    [Fact]
    public void AdminPassword_ShouldReturnRawValue_WhenSet()
    {
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "  p@ss  ");

        _environmentProvider.AdminPassword.Should().Be("  p@ss  ");
    }

    [Fact]
    public void TrustedProxies_ShouldReturnEmpty_WhenNotSet()
    {
        _environmentProvider.TrustedProxies.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TrustedProxies_ShouldReturnEmpty_WhenBlank(string value)
    {
        Environment.SetEnvironmentVariable("TRUSTED_PROXIES", value);

        _environmentProvider.TrustedProxies.Should().BeEmpty();
    }

    [Fact]
    public void TrustedProxies_ShouldSplitOnCommasAndTrimEntries()
    {
        Environment.SetEnvironmentVariable("TRUSTED_PROXIES", " 10.0.0.1 , 10.0.0.2 ,,10.0.0.3 ");

        _environmentProvider.TrustedProxies.Should().BeEquivalentTo(["10.0.0.1", "10.0.0.2", "10.0.0.3"]);
    }

    [Fact]
    public void CorsOrigins_ShouldReturnEmpty_WhenNotSet()
    {
        _environmentProvider.CorsOrigins.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CorsOrigins_ShouldReturnEmpty_WhenBlank(string value)
    {
        Environment.SetEnvironmentVariable("CORS_ORIGINS", value);

        _environmentProvider.CorsOrigins.Should().BeEmpty();
    }

    [Fact]
    public void CorsOrigins_ShouldSplitOnCommasAndTrimEntries()
    {
        Environment.SetEnvironmentVariable("CORS_ORIGINS", "https://a.example.com, https://b.example.com");

        _environmentProvider.CorsOrigins.Should().BeEquivalentTo(["https://a.example.com", "https://b.example.com"]);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("TRUE", true)]
    public void SwaggerEnabled_ShouldUseExplicitValue_WhenParsable(string value, bool expected)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "production");
        Environment.SetEnvironmentVariable("SWAGGER_ENABLED", value);

        _environmentProvider.SwaggerEnabled.Should().Be(expected);
    }

    [Theory]
    [InlineData("development", true)]
    [InlineData("production", false)]
    public void SwaggerEnabled_ShouldFallBackToIsDevelopment_WhenNotSet(string environmentName, bool expected)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);

        _environmentProvider.SwaggerEnabled.Should().Be(expected);
    }

    [Fact]
    public void SwaggerEnabled_ShouldFallBackToIsDevelopment_WhenValueIsNotABoolean()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");
        Environment.SetEnvironmentVariable("SWAGGER_ENABLED", "maybe");

        _environmentProvider.SwaggerEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("2525", 2525)]
    [InlineData("0", 587)]
    [InlineData("-1", 587)]
    [InlineData("", 587)]
    [InlineData("not-a-port", 587)]
    [InlineData(null, 587)]
    public void SmtpPort_ShouldFallBackTo587_WhenValueIsNotAPositiveInteger(string? value, int expected)
    {
        Environment.SetEnvironmentVariable("SMTP_PORT", value);

        _environmentProvider.SmtpPort.Should().Be(expected);
    }

    [Fact]
    public void SmtpSettings_ShouldReturnConfiguredValues()
    {
        Environment.SetEnvironmentVariable("SMTP_HOST", "smtp.example.com");
        Environment.SetEnvironmentVariable("SMTP_USERNAME", "mailer");
        Environment.SetEnvironmentVariable("SMTP_PASSWORD", "secret");
        Environment.SetEnvironmentVariable("SMTP_FROM_ADDRESS", "no-reply@example.com");
        Environment.SetEnvironmentVariable("SMTP_FROM_NAME", "Board Game Tracker");

        _environmentProvider.SmtpHost.Should().Be("smtp.example.com");
        _environmentProvider.SmtpUsername.Should().Be("mailer");
        _environmentProvider.SmtpPassword.Should().Be("secret");
        _environmentProvider.SmtpFromAddress.Should().Be("no-reply@example.com");
        _environmentProvider.SmtpFromName.Should().Be("Board Game Tracker");
    }

    [Fact]
    public void SmtpSettings_ShouldReturnNull_WhenNotSet()
    {
        _environmentProvider.SmtpHost.Should().BeNull();
        _environmentProvider.SmtpUsername.Should().BeNull();
        _environmentProvider.SmtpPassword.Should().BeNull();
        _environmentProvider.SmtpFromAddress.Should().BeNull();
        _environmentProvider.SmtpFromName.Should().BeNull();
    }

    [Theory]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    [InlineData("true", true)]
    [InlineData("anything", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    public void SmtpUseSsl_ShouldOnlyBeFalse_WhenExplicitlySetToFalse(string? value, bool expected)
    {
        Environment.SetEnvironmentVariable("SMTP_USE_SSL", value);

        _environmentProvider.SmtpUseSsl.Should().Be(expected);
    }

    [Fact]
    public void EmailEnabled_ShouldBeTrue_WhenHostAndFromAddressAreSet()
    {
        Environment.SetEnvironmentVariable("SMTP_HOST", "smtp.example.com");
        Environment.SetEnvironmentVariable("SMTP_FROM_ADDRESS", "no-reply@example.com");

        _environmentProvider.EmailEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "no-reply@example.com")]
    [InlineData("smtp.example.com", null)]
    [InlineData("   ", "no-reply@example.com")]
    [InlineData("smtp.example.com", "   ")]
    [InlineData(null, null)]
    public void EmailEnabled_ShouldBeFalse_WhenHostOrFromAddressIsMissing(string? host, string? fromAddress)
    {
        Environment.SetEnvironmentVariable("SMTP_HOST", host);
        Environment.SetEnvironmentVariable("SMTP_FROM_ADDRESS", fromAddress);

        _environmentProvider.EmailEnabled.Should().BeFalse();
    }
}
