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

    public EnvironmentProviderTests()
    {
        _environmentProvider = new EnvironmentProvider();
        _originalEnvironmentVariables = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            ["ENVIRONMENT"] = Environment.GetEnvironmentVariable("ENVIRONMENT"),
            ["PORT"] = Environment.GetEnvironmentVariable("PORT"),
            ["STATISTICS"] = Environment.GetEnvironmentVariable("STATISTICS"),
            ["STATISTICS_ENABLED"] = Environment.GetEnvironmentVariable("STATISTICS_ENABLED"),
            ["LOGLEVEL"] = Environment.GetEnvironmentVariable("LOGLEVEL"),
            ["AUTH_ENABLED"] = Environment.GetEnvironmentVariable("AUTH_ENABLED"),
            ["JWT_SECRET"] = Environment.GetEnvironmentVariable("JWT_SECRET")
        };

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ENVIRONMENT", null);
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
    [InlineData("", 7178)]
    [InlineData("   ", 7178)]
    [InlineData("abc", 7178)]
    [InlineData("8080.5", 7178)]
    [InlineData("not-a-number", 7178)]
    [InlineData("-1", 7178)]
    [InlineData(null, 7178)]
    public void Port_ShouldReturnCorrectValue_WithValidPortNumbers(string? portString, int expectedPort)
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
    public void AllProperties_ShouldBeConsistent_WhenCalledMultipleTimes()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "production");
        Environment.SetEnvironmentVariable("PORT", "8080");
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", "true");
        Environment.SetEnvironmentVariable("LOGLEVEL", "ERROR");

        var environmentName1 = _environmentProvider.EnvironmentName;
        var environmentName2 = _environmentProvider.EnvironmentName;
        var port1 = _environmentProvider.Port;
        var port2 = _environmentProvider.Port;
        var statistics1 = _environmentProvider.StatisticsEnabled;
        var statistics2 = _environmentProvider.StatisticsEnabled;
        var logLevel1 = _environmentProvider.LogLevel;
        var logLevel2 = _environmentProvider.LogLevel;
        var isDev1 = _environmentProvider.IsDevelopment;
        var isDev2 = _environmentProvider.IsDevelopment;

        environmentName1.Should().Be(environmentName2);
        port1.Should().Be(port2);
        statistics1.Should().Be(statistics2);
        logLevel1.Should().Be(logLevel2);
        isDev1.Should().Be(isDev2);
    }

    [Fact]
    public void Properties_ShouldReflectEnvironmentChanges_WhenEnvironmentVariablesChange()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "development");
        Environment.SetEnvironmentVariable("PORT", "3000");
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", "false");

        var initialEnv = _environmentProvider.EnvironmentName;
        var initialPort = _environmentProvider.Port;
        var initialStats = _environmentProvider.StatisticsEnabled;
        var initialIsDev = _environmentProvider.IsDevelopment;

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "production");
        Environment.SetEnvironmentVariable("PORT", "8080");
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", "true");

        var updatedEnv = _environmentProvider.EnvironmentName;
        var updatedPort = _environmentProvider.Port;
        var updatedStats = _environmentProvider.StatisticsEnabled;
        var updatedIsDev = _environmentProvider.IsDevelopment;

        initialEnv.Should().Be("development");
        updatedEnv.Should().Be("production");
        initialPort.Should().Be(3000);
        updatedPort.Should().Be(8080);
        initialStats.Should().BeFalse();
        updatedStats.Should().BeTrue();
        initialIsDev.Should().BeTrue();
        updatedIsDev.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void EnableStatistics_ShouldReturnFalse_WhenEnvironmentVariableIsEmptyString(string input)
    {
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", input);

        var result = _environmentProvider.StatisticsEnabled;

        result.Should().BeFalse();
    }

    [Fact]
    public void EnvironmentName_ShouldDefaultToDevelopment_WhenNoEnvironmentVariablesSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ENVIRONMENT", null);

        var result = _environmentProvider.EnvironmentName;

        result.Should().Be("development");
    }

    [Fact]
    public void EnvironmentName_ShouldPreferAspNetCoreEnvironment_WhenBothAreSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        Environment.SetEnvironmentVariable("ENVIRONMENT", "staging");

        var result = _environmentProvider.EnvironmentName;

        result.Should().Be("Production");
    }

    [Fact]
    public void EnvironmentName_ShouldFallbackToEnvironment_WhenAspNetCoreEnvironmentNotSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ENVIRONMENT", "staging");

        var result = _environmentProvider.EnvironmentName;

        result.Should().Be("staging");
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
    public void IsDevelopment_ShouldReturnTrue_WhenNoEnvironmentSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("ENVIRONMENT", null);

        var result = _environmentProvider.IsDevelopment;

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDevelopment_ShouldReturnFalse_WhenProductionEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

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

    [Fact]
    public void JwtSecret_ShouldReturnNull_WhenNotSet()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", null);

        _environmentProvider.JwtSecret.Should().BeNull();
    }

    [Fact]
    public void JwtSecret_ShouldReturnValue_WhenSet()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "my-secret-key");

        _environmentProvider.JwtSecret.Should().Be("my-secret-key");
    }

    [Fact]
    public void JwtSecret_ShouldReturnNull_WhenSetToEmpty()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "");

        _environmentProvider.JwtSecret.Should().BeNull();
    }
}
