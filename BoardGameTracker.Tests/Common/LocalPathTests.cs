using BoardGameTracker.Core.Common;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Common;

public class LocalPathTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/games")]
    [InlineData("/games/12?tab=sessions")]
    [InlineData("/settings#account")]
    public void IsSafe_ShouldAcceptLocalPaths(string path)
    {
        LocalPath.IsSafe(path).Should().BeTrue();
        LocalPath.Normalize(path).Should().Be(path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("games")]
    [InlineData("//evil.example.com")]
    [InlineData("/\\evil.example.com")]
    [InlineData("https://evil.example.com")]
    [InlineData("https:/evil.example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/games\r\nSet-Cookie: x=y")]
    [InlineData("/games two")]
    public void IsSafe_ShouldRejectEverythingElse(string? path)
    {
        LocalPath.IsSafe(path).Should().BeFalse();
        LocalPath.Normalize(path).Should().Be("/");
    }
}
