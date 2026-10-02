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
    [InlineData("http://[::1]:8080", true)]
    [InlineData("http://[fd00::1]", true)]
    [InlineData("http://[fe80::1]", true)]
    [InlineData("http://[::ffff:10.0.0.1]", true)]
    [InlineData("http://172.16.0.1", true)]
    [InlineData("http://172.31.255.254", true)]
    [InlineData("http://169.254.10.1", true)]
    [InlineData("http://[2001:4860:4860::8888]", false)]
    [InlineData("http://[2001:db8::1]/token", false)]
    [InlineData("http://[::ffff:8.8.8.8]", false)]
    [InlineData("http://172.15.0.1", false)]
    [InlineData("http://172.32.0.1", false)]
    [InlineData("https://[2001:4860:4860::8888]", true)]
    [InlineData("http://auth.example.com", false)]
    [InlineData("http://8.8.8.8", false)]
    [InlineData("ftp://auth.example.com", false)]
    [InlineData("not a url", false)]
    public void IsAcceptable_ShouldOnlyAllowHttpsOrLocalHttp(string url, bool expected)
    {
        SecureUrlPolicy.IsAcceptable(url).Should().Be(expected);
    }
}
