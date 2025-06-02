using System;
using System.Collections.Generic;
using BoardGameTracker.Core.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class EnvironmentProviderTests : IDisposable
    {
        private readonly EnvironmentProvider _environmentProvider;
        private readonly Dictionary<string, string> _originalEnvironmentVariables;

        public EnvironmentProviderTests()
        {
            _environmentProvider = new EnvironmentProvider();
            _originalEnvironmentVariables = new Dictionary<string, string>
            {
                ["ENVIRONMENT"] = Environment.GetEnvironmentVariable("ENVIRONMENT"),
                ["PORT"] = Environment.GetEnvironmentVariable("PORT"),
                ["STATISTICS"] = Environment.GetEnvironmentVariable("STATISTICS"),
                ["LOGLEVEL"] = Environment.GetEnvironmentVariable("LOGLEVEL")
            };
        }

        public void Dispose()
        {
            foreach (var kvp in _originalEnvironmentVariables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
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
            Environment.SetEnvironmentVariable("STATISTICS", statisticsValue);

            var result = _environmentProvider.EnableStatistics;

            result.Should().Be(expected);
        }

        [Fact]
        public void AllProperties_ShouldBeConsistent_WhenCalledMultipleTimes()
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "production");
            Environment.SetEnvironmentVariable("PORT", "8080");
            Environment.SetEnvironmentVariable("STATISTICS", "true");
            Environment.SetEnvironmentVariable("LOGLEVEL", "ERROR");

            var environmentName1 = _environmentProvider.EnvironmentName;
            var environmentName2 = _environmentProvider.EnvironmentName;
            var port1 = _environmentProvider.Port;
            var port2 = _environmentProvider.Port;
            var statistics1 = _environmentProvider.EnableStatistics;
            var statistics2 = _environmentProvider.EnableStatistics;
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
            Environment.SetEnvironmentVariable("ENVIRONMENT", "development");
            Environment.SetEnvironmentVariable("PORT", "3000");
            Environment.SetEnvironmentVariable("STATISTICS", "false");

            var initialEnv = _environmentProvider.EnvironmentName;
            var initialPort = _environmentProvider.Port;
            var initialStats = _environmentProvider.EnableStatistics;
            var initialIsDev = _environmentProvider.IsDevelopment;

            Environment.SetEnvironmentVariable("ENVIRONMENT", "production");
            Environment.SetEnvironmentVariable("PORT", "8080");
            Environment.SetEnvironmentVariable("STATISTICS", "true");

            var updatedEnv = _environmentProvider.EnvironmentName;
            var updatedPort = _environmentProvider.Port;
            var updatedStats = _environmentProvider.EnableStatistics;
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

        [Fact]
        public void EnableStatistics_ShouldReturnFalse_WhenEnvironmentVariableIsEmptyString()
        {
            Environment.SetEnvironmentVariable("STATISTICS", "");

            var result = _environmentProvider.EnableStatistics;

            result.Should().BeFalse();
        }

        [Fact]
        public void EnableStatistics_ShouldReturnFalse_WhenEnvironmentVariableIsWhitespace()
        {
            Environment.SetEnvironmentVariable("STATISTICS", "   ");

            var result = _environmentProvider.EnableStatistics;

            result.Should().BeFalse();
        }

        [Fact]
        public void EnvironmentName_ShouldHandleEmptyString_WhenEnvironmentVariableIsEmpty()
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "");

            var result = _environmentProvider.EnvironmentName;

            result.Should().Be("development");
        }

        [Fact]
        public void EnvironmentName_ShouldHandleWhitespace_WhenEnvironmentVariableIsWhitespace()
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "   ");

            var result = _environmentProvider.EnvironmentName;

            result.Should().Be("   ");
        }

        [Fact]
        public void IsDevelopment_ShouldBeCaseSensitive_WhenComparingEnvironmentName()
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", "DEVELOPMENT");

            var result = _environmentProvider.IsDevelopment;

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("development", true)]
        [InlineData("Development", false)]
        [InlineData("DEVELOPMENT", false)]
        [InlineData("dev", false)]
        [InlineData("develop", false)]
        [InlineData("production", false)]
        [InlineData("staging", false)]
        [InlineData("test", false)]
        [InlineData("custom", false)]
        [InlineData(null, true)]
        public void IsDevelopment_ShouldCheckExactMatch_WithDifferentCasing(string? environmentName, bool expectedResult)
        {
            Environment.SetEnvironmentVariable("ENVIRONMENT", environmentName);

            var result = _environmentProvider.IsDevelopment;

            result.Should().Be(expectedResult);
        }
    }