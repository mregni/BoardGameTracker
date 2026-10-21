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

    [Theory]
    [InlineData("https://shop.example.com/brass", true)]
    [InlineData("http://shop.example.com/brass", true)]
    [InlineData("https://[2001:4860:4860::8888]/", true)]
    [InlineData("http://localhost:8080/admin", false)]
    [InlineData("https://127.0.0.1/", false)]
    [InlineData("http://192.168.1.1/", false)]
    [InlineData("http://169.254.169.254/latest/meta-data", false)]
    [InlineData("http://[::1]/", false)]
    [InlineData("http://[fd00::1]/", false)]
    [InlineData("http://router/", false)]
    [InlineData("https://nas.local/", false)]
    [InlineData("https://grafana.internal/", false)]
    [InlineData("ftp://shop.example.com/", false)]
    [InlineData("not a url", false)]
    public void IsPublicWebAddress_ShouldRefuseLocalNetworkAddresses(string url, bool expected)
    {
        SecureUrlPolicy.IsPublicWebAddress(url).Should().Be(expected);
    }

    [Theory]
    [InlineData("localhost", true)]
    [InlineData("LOCALHOST.", true)]
    [InlineData("app.localhost", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("[::1]", true)]
    [InlineData("idp.example.com", false)]
    [InlineData("192.168.1.20", false)]
    public void IsLoopbackHost_ShouldRecogniseLoopbackNamesAndAddresses(string host, bool expected)
    {
        SecureUrlPolicy.IsLoopbackHost(host).Should().Be(expected);
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("0.0.0.0", true)]
    [InlineData("0.1.2.3", true)]
    [InlineData("100.64.1.1", true)]
    [InlineData("224.0.0.1", true)]
    [InlineData("::ffff:10.0.0.1", true)]
    [InlineData("ff02::1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("100.128.0.1", false)]
    [InlineData("2001:4860:4860::8888", false)]
    public void IsInternalAddress_ShouldFlagAddressesThatAreNotPublic(string address, bool expected)
    {
        SecureUrlPolicy.IsInternalAddress(System.Net.IPAddress.Parse(address)).Should().Be(expected);
    }

    [Theory]
    [InlineData("169.254.169.254", true)]
    [InlineData("::ffff:169.254.169.254", true)]
    [InlineData("fd00:ec2::254", true)]
    [InlineData("100.100.100.200", true)]
    [InlineData("169.254.169.253", false)]
    public void IsMetadataAddress_ShouldFlagCloudMetadataEndpoints(string address, bool expected)
    {
        SecureUrlPolicy.IsMetadataAddress(System.Net.IPAddress.Parse(address)).Should().Be(expected);
    }
}
