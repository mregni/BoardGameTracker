using System;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Email;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class PublicUrlBuilderTests
{
    private readonly Mock<IConfigRepository> _configRepositoryMock = new();
    private readonly PublicUrlBuilder _builder;

    public PublicUrlBuilderTests()
    {
        _builder = new PublicUrlBuilder(_configRepositoryMock.Object);
    }

    private void SetupPublicUrl(string? url)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl))
            .ReturnsAsync(url!);
    }

    private void VerifyBaseUrlReadOnce()
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl), Times.Once);
        _configRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildRsvpUrlAsync_ShouldAppendRsvpPath_WhenBaseUrlIsConfigured()
    {
        var linkId = new Guid("11111111-2222-3333-4444-555555555555");
        SetupPublicUrl("https://games.example.com");

        var result = await _builder.BuildRsvpUrlAsync(linkId);

        result.Should().Be("https://games.example.com/rsvp?linkId=11111111-2222-3333-4444-555555555555");
        VerifyBaseUrlReadOnce();
    }

    [Theory]
    [InlineData("https://games.example.com/", "https://games.example.com")]
    [InlineData("https://games.example.com///", "https://games.example.com")]
    [InlineData("https://games.example.com/sub/", "https://games.example.com/sub")]
    public async Task BuildRsvpUrlAsync_ShouldTrimTrailingSlashes_WhenBaseUrlEndsWithSlash(string configured, string expectedBase)
    {
        var linkId = Guid.NewGuid();
        SetupPublicUrl(configured);

        var result = await _builder.BuildRsvpUrlAsync(linkId);

        result.Should().Be($"{expectedBase}/rsvp?linkId={linkId}");
        VerifyBaseUrlReadOnce();
    }

    [Fact]
    public async Task BuildRsvpUrlAsync_ShouldReturnRelativeUrl_WhenBaseUrlIsNull()
    {
        var linkId = Guid.NewGuid();
        SetupPublicUrl(null);

        var result = await _builder.BuildRsvpUrlAsync(linkId);

        result.Should().Be($"/rsvp?linkId={linkId}");
        VerifyBaseUrlReadOnce();
    }

    [Fact]
    public async Task BuildResetUrlAsync_ShouldAppendResetPath_WhenBaseUrlIsConfigured()
    {
        SetupPublicUrl("https://games.example.com");

        var result = await _builder.BuildResetUrlAsync("user-1", "token-1");

        result.Should().Be("https://games.example.com/reset-password?userId=user-1&token=token-1");
        VerifyBaseUrlReadOnce();
    }

    [Fact]
    public async Task BuildResetUrlAsync_ShouldEscapeUserIdAndToken_WhenTheyContainReservedCharacters()
    {
        SetupPublicUrl("https://games.example.com");

        var result = await _builder.BuildResetUrlAsync("user id&x", "a+b/c=d");

        result.Should().Be("https://games.example.com/reset-password?userId=user%20id%26x&token=a%2Bb%2Fc%3Dd");
        VerifyBaseUrlReadOnce();
    }

    [Fact]
    public async Task BuildResetUrlAsync_ShouldReturnRelativeUrl_WhenBaseUrlIsEmpty()
    {
        SetupPublicUrl(string.Empty);

        var result = await _builder.BuildResetUrlAsync("user-1", "token-1");

        result.Should().Be("/reset-password?userId=user-1&token=token-1");
        VerifyBaseUrlReadOnce();
    }
}
