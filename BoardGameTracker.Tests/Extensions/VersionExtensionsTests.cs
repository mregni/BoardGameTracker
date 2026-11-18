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

    public static TheoryData<Version, string> VersionCases => new()
    {
        { new Version(0, 0, 0), "0.0.0" },
        { new Version(1, 0, 0), "1.0.0" },
        { new Version(1, 2, 3), "1.2.3" },
        { new Version(2, 5, 10), "2.5.10" },
        { new Version(10, 20, 30), "10.20.30" },
        { new Version(100, 200, 300), "100.200.300" },
        { new Version(1, 2, 3, 4), "1.2.3" },
        { new Version(1, 2), "1.2.0" },
    };

    [Theory]
    [MemberData(nameof(VersionCases))]
    public void ToVersionString_ShouldReturnMajorMinorBuild(Version version, string expected)
    {
        version.ToVersionString().Should().Be(expected);
    }
}
