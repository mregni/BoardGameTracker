using System;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Serilog.Events;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

[Collection("EnvironmentVariables")]
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
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnError_WhenEnvironmentVariableIsERROR()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "ERROR");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnInformation_WhenEnvironmentVariableIsINFO()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "INFO");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnDebug_WhenEnvironmentVariableIsDEBUG()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "debug");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogEventLevel.Debug);
    }

    [Fact]
    public void GetEnvironmentLogLevel_ShouldReturnWarning_WhenEnvironmentVariableIsWARNING()
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", "warning");

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogEventLevel.Warning);
    }

    [Theory]
    [InlineData(" error ", LogEventLevel.Error)]
    [InlineData(" ERROR ", LogEventLevel.Error)]
    [InlineData(" info ", LogEventLevel.Information)]
    [InlineData(" INFO ", LogEventLevel.Information)]
    [InlineData(" debug ", LogEventLevel.Debug)]
    [InlineData(" DEBUG ", LogEventLevel.Debug)]
    [InlineData(" warning ", LogEventLevel.Warning)]
    [InlineData(" WARNING ", LogEventLevel.Warning)]
    [InlineData("unknown", LogEventLevel.Warning)]
    [InlineData("invalid", LogEventLevel.Warning)]
    [InlineData("TRACE", LogEventLevel.Warning)]
    [InlineData("CRITICAL", LogEventLevel.Warning)]
    [InlineData("FATAL", LogEventLevel.Warning)]
    [InlineData("random", LogEventLevel.Warning)]
    [InlineData("123", LogEventLevel.Warning)]
    [InlineData("!@#", LogEventLevel.Warning)]
    [InlineData(" ", LogEventLevel.Warning)]
    [InlineData(null, LogEventLevel.Warning)]
    public void GetEnvironmentLogLevel_ShouldReturnWarning_WhenEnvironmentVariableIsUnknownValue(string? value, LogEventLevel logLevel)
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", value);

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(logLevel);
    }
}