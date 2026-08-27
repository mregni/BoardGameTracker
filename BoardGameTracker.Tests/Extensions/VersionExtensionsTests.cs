using System;
using System.Collections.Generic;
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

        version.ToVersionString().Should().Be(string.Empty);
    }

    public static IEnumerable<object[]> VersionCases()
    {
        yield return new object[] { new Version(0, 0, 0), "0.0.0" };
        yield return new object[] { new Version(1, 0, 0), "1.0.0" };
        yield return new object[] { new Version(1, 2, 3), "1.2.3" };
        yield return new object[] { new Version(2, 5, 10), "2.5.10" };
        yield return new object[] { new Version(10, 20, 30), "10.20.30" };
        yield return new object[] { new Version(100, 200, 300), "100.200.300" };
        yield return new object[] { new Version(1, 2, 3, 4), "1.2.3" };
        yield return new object[] { new Version(1, 2), "1.2.0" };
    }

    [Theory]
    [MemberData(nameof(VersionCases))]
    public void ToVersionString_ShouldReturnMajorMinorBuild(Version version, string expected)
    {
        version.ToVersionString().Should().Be(expected);
    }
}
