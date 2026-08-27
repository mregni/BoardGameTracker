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

    [Theory]
    [InlineData("ERROR", LogEventLevel.Error)]
    [InlineData(" error ", LogEventLevel.Error)]
    [InlineData(" ERROR ", LogEventLevel.Error)]
    [InlineData("INFO", LogEventLevel.Information)]
    [InlineData(" info ", LogEventLevel.Information)]
    [InlineData(" INFO ", LogEventLevel.Information)]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData(" debug ", LogEventLevel.Debug)]
    [InlineData(" DEBUG ", LogEventLevel.Debug)]
    public void GetEnvironmentLogLevel_ShouldMapTrimmedCaseInsensitiveValue(string value, LogEventLevel expected)
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", value);

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("warning")]
    [InlineData("warn")]
    [InlineData("WARNING")]
    [InlineData(" warning ")]
    [InlineData("unknown")]
    [InlineData("invalid")]
    [InlineData("TRACE")]
    [InlineData("CRITICAL")]
    [InlineData("FATAL")]
    [InlineData("random")]
    [InlineData("123")]
    [InlineData("!@#")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetEnvironmentLogLevel_ShouldFallBackToWarning_ForUnrecognizedValue(string? value)
    {
        Environment.SetEnvironmentVariable("LOGLEVEL", value);

        var result = LogLevelExtensions.GetEnvironmentLogLevel();

        result.Should().Be(LogEventLevel.Warning);
    }
}
