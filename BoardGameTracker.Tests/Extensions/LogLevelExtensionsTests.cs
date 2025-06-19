using System;
using System.Collections.Generic;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class LogLevelExtensionsTests : IDisposable
{
    private readonly string _originalLogLevel;

    public LogLevelExtensionsTests()
    {
        _originalLogLevel = Environment.GetEnvironmentVariable("LOGLEVEL") ?? "DEBUG";
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", _originalLogLevel);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnError_WhenEnvironmentVariableIsERROR()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "ERROR");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnInformation_WhenEnvironmentVariableIsINFO()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "INFO");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnDebug_WhenEnvironmentVariableIsDEBUG()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "DEBUG");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnWarning_WhenEnvironmentVariableIsWARNING()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "WARNING");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogLevel.Warning);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("invalid")]
    [InlineData("TRACE")]
    [InlineData("CRITICAL")]
    [InlineData("FATAL")]
    [InlineData("random")]
    [InlineData("123")]
    [InlineData("!@#")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetEnvironmentLogLevel_ShouldReturnWarning_WhenEnvironmentVariableIsUnknownValue(string? value)
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", value);

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogLevel.Warning);
    }

    [Theory]
    [InlineData(" ERROR ", LogLevel.Error)]
    [InlineData(" INFO ", LogLevel.Information)]
    [InlineData(" DEBUG ", LogLevel.Debug)]
    [InlineData(" WARNING ", LogLevel.Warning)]
    public void GetEnvironmentLogLevel_ShouldReturnWarning_WhenEnvironmentVariableHasWhitespace(string value, LogLevel logLevel)
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", value);

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(logLevel);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldBeConsistent_WhenCalledMultipleTimes()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "ERROR");

        var result1 = LogLevelExtensions.GetEnvironmentLogLevel();
        var result2 = LogLevelExtensions.GetEnvironmentLogLevel();
        var result3 = LogLevelExtensions.GetEnvironmentLogLevel();

        result1.Should().Be(LogLevel.Error);
        result2.Should().Be(LogLevel.Error);
        result3.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReflectChanges_WhenEnvironmentVariableIsModified()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "ERROR");
        var result1 = LogLevelExtensions.GetEnvironmentLogLevel();

        Environment.SetEnvironmentVariable("LOGLEVEL", "DEBUG");
        var result2 = LogLevelExtensions.GetEnvironmentLogLevel();

        Environment.SetEnvironmentVariable("LOGLEVEL", "INFO");
        var result3 = LogLevelExtensions.GetEnvironmentLogLevel();

        result1.Should().Be(LogLevel.Error);
        result2.Should().Be(LogLevel.Debug);
        result3.Should().Be(LogLevel.Information);
    }
}