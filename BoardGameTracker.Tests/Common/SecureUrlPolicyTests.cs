using BoardGameTracker.Core.Common;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Common;

public class SecureUrlPolicyTests
{
    [Theory]
    [InlineData("https://accounts.google.com", true)]
    [InlineData("https://auth.example.com/realms/home", true)]
    [InlineData("http://localhost:8080", true)]
    [InlineData("http://127.0.0.1:9000", true)]
    [InlineData("http://192.168.1.20:8080/auth", true)]
    [InlineData("http://10.0.0.5", true)]
    [InlineData("http://172.20.0.3", true)]
    [InlineData("http://authelia", true)]
    [InlineData("http://nas.local", true)]
    [InlineData("http://auth.example.com", false)]
    [InlineData("http://8.8.8.8", false)]
    [InlineData("ftp://auth.example.com", false)]
    [InlineData("not a url", false)]
    public void IsAcceptable_ShouldOnlyAllowHttpsOrLocalHttp(string url, bool expected)
    {
        SecureUrlPolicy.IsAcceptable(url).Should().Be(expected);
    }
}
