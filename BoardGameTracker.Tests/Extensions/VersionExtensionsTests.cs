using System;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class VersionExtensionsTests
{
    [Fact]
    public void ToVersionString_ShouldReturnEmptyString_WhenVersionIsNull()
    {
        Version? version = null;

        var result = version.ToVersionString();

        result.Should().NotBeNull();
        result.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(0, 0, 0, "0.0.0")]
    [InlineData(1, 0, 0, "1.0.0")]
    [InlineData(1, 2, 3, "1.2.3")]
    [InlineData(2, 5, 10, "2.5.10")]
    [InlineData(10, 20, 30, "10.20.30")]
    [InlineData(100, 200, 300, "100.200.300")]
    public void ToVersionString_ShouldReturnCorrectFormat_WithDifferentVersionNumbers(int major, int minor, int build,
        string expected)
    {
        var version = new Version(major, minor, build);

        var result = version.ToVersionString();

        result.Should().NotBeNull();
        result.Should().Be(expected);
    }

    [Fact]
    public void ToVersionString_ShouldHandleVersionWithRevision_WhenRevisionIsProvided()
    {
        var version = new Version(1, 2, 3, 4);

        var result = version.ToVersionString();

        result.Should().NotBeNull();
        result.Should().Be("1.2.3");
    }

    [Fact]
    public void ToVersionString_ShouldReturnFormattedString_WhenVersionHasOnlyMajorMinor()
    {
        var version = new Version(1, 2);

        var result = version.ToVersionString();

        result.Should().NotBeNull();
        result.Should().Be("1.2.0");
    }

    [Fact]
    public void ToVersionString_ShouldBeConsistent_WhenCalledMultipleTimes()
    {
        var version = new Version(1, 2, 3);

        var result1 = version.ToVersionString();
        var result2 = version.ToVersionString();
        var result3 = version.ToVersionString();

        result1.Should().Be("1.2.3");
        result2.Should().Be("1.2.3");
        result3.Should().Be("1.2.3");
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    [Fact]
    public void ToVersionString_ShouldHandleVersionFromAssembly_WhenVersionIsFromAssemblyName()
    {
        var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        var result = assemblyVersion.ToVersionString();

        if (assemblyVersion != null)
        {
            result.Should().NotBeEmpty();
            result.Should().Contain(".");
            result.Split('.').Should().HaveCount(3);
        }
        else
        {
            result.Should().Be(string.Empty);
        }
    }
}