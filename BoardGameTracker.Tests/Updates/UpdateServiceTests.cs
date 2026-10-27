using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.DockerHub;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Updates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using Xunit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Tests.Updates;

public class UpdateServiceTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IDockerHubApi> _dockerHubApiMock;
    private readonly Mock<ILogger<UpdateService>> _loggerMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly UpdateService _updateService;

    public UpdateServiceTests()
    {
        _configRepositoryMock = new Mock<IConfigRepository>();
        _dockerHubApiMock = new Mock<IDockerHubApi>();
        _loggerMock = new Mock<ILogger<UpdateService>>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(FixedUtcNow);

        _updateService = new UpdateService(
            _configRepositoryMock.Object,
            _dockerHubApiMock.Object,
            _loggerMock.Object,
            _dateTimeProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _configRepositoryMock.VerifyNoOtherCalls();
        _dockerHubApiMock.VerifyNoOtherCalls();
    }

    private void SetupTrack(VersionTrack track)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track))
            .ReturnsAsync(track);
    }

    private void SetupTags(params string[] tags)
    {
        SetupTagPage(1, null, tags);
    }

    private void SetupTagPage(int page, string? next, params string[] tags)
    {
        var response = new ApiResponse<DockerHubTagsResponse>(
            new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage() },
            new DockerHubTagsResponse
            {
                Count = tags.Length,
                Next = next,
                Results = tags.Select(t => new DockerHubTag { Name = t }).ToList()
            },
            new RefitSettings());

        _dockerHubApiMock
            .Setup(x => x.GetTags("uping", "boardgametracker", page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void VerifyCheckPreamble()
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track), Times.Once);
        _dockerHubApiMock.Verify(x => x.GetTags("uping", "boardgametracker", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyLastRunPersisted()
    {
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckLastRun, FixedUtcNow.ToString("O")), Times.Once);
    }

    #region GetVersionInfoAsync Tests

    [Fact]
    public async Task GetVersionInfoAsync_ShouldReturnUpdateStatus_WhenConfigExists()
    {
        var lastRun = new DateTime(2026, 8, 20, 8, 30, 0, DateTimeKind.Utc);
        var config = new Dictionary<string, string>
        {
            { UpdateConfig.AvailableVersion, "1.2.0" },
            { UpdateConfig.Available, "true" },
            { UpdateConfig.CheckLastRun, lastRun.ToString("O") }
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix))
            .ReturnsAsync(config);

        var result = await _updateService.GetVersionInfoAsync();

        result.Should().NotBeNull();
        result.CurrentVersion.Should().Be(_updateService.GetCurrentVersion());
        result.LatestVersion.Should().Be("1.2.0");
        result.UpdateAvailable.Should().BeTrue();
        result.LastChecked.Should().NotBeNull();
        result.LastChecked!.Value.ToUniversalTime().Should().Be(lastRun);

        _configRepositoryMock.Verify(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetVersionInfoAsync_ShouldReturnNoUpdateAvailable_WhenConfigIsFalse()
    {
        var config = new Dictionary<string, string>
        {
            { UpdateConfig.Available, "false" }
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix))
            .ReturnsAsync(config);

        var result = await _updateService.GetVersionInfoAsync();

        result.UpdateAvailable.Should().BeFalse();

        _configRepositoryMock.Verify(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetVersionInfoAsync_ShouldIncludeErrorMessage_WhenPresent()
    {
        var config = new Dictionary<string, string>
        {
            { UpdateConfig.CheckError, "Network error occurred" }
        };

        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix))
            .ReturnsAsync(config);

        var result = await _updateService.GetVersionInfoAsync();

        result.ErrorMessage.Should().Be("Network error occurred");

        _configRepositoryMock.Verify(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetVersionInfoAsync_ShouldReturnEmptyStatus_WhenNoConfigExists()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix))
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _updateService.GetVersionInfoAsync();

        result.Should().NotBeNull();
        result.CurrentVersion.Should().Be(_updateService.GetCurrentVersion());
        result.LatestVersion.Should().BeNull();
        result.UpdateAvailable.Should().BeFalse();
        result.LastChecked.Should().BeNull();

        _configRepositoryMock.Verify(x => x.GetConfigsByPrefixAsync(UpdateConfig.Prefix), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CheckForUpdatesAsync Tests

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistLatestStableVersion_WhenUpdateIsAvailable()
    {
        SetupTrack(VersionTrack.Stable);
        SetupTags("latest", "999.0.0", "999.1.0-beta", "1.0.0");

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.AvailableVersion, "999.0.0"), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.Available, true), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistLatestBetaVersion_WhenTrackIsBeta()
    {
        SetupTrack(VersionTrack.Beta);
        SetupTags("999.0.0", "998.0.0-beta", "999.1.0-beta");

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.AvailableVersion, "999.1.0-beta"), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.Available, true), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReadLaterPages_WhenTheStableTagsAreNotOnTheFirstPage()
    {
        SetupTrack(VersionTrack.Stable);
        SetupTagPage(1, "page-2", "998.0.0-beta", "998.1.0", "999.0.0-beta");
        SetupTagPage(2, null, "latest", "999.2.0");

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _dockerHubApiMock.Verify(x => x.GetTags("uping", "boardgametracker", 2, It.IsAny<CancellationToken>()), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.AvailableVersion, "999.2.0"), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.Available, true), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldStopAfterTenPages()
    {
        SetupTrack(VersionTrack.Stable);
        for (var page = 1; page <= 11; page++)
        {
            SetupTagPage(page, "more", $"{page}.0.0");
        }

        await _updateService.CheckForUpdatesAsync();

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track), Times.Once);
        _dockerHubApiMock.Verify(x => x.GetTags("uping", "boardgametracker", It.IsInRange(1, 10, Moq.Range.Inclusive), It.IsAny<CancellationToken>()), Times.Exactly(10));
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.AvailableVersion, "10.0.0"), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.Available, true), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("999.0.0-beta", "999.0.0", "999.0.0")]
    [InlineData("999.0.0", "999.1.0-beta", "999.1.0-beta")]
    [InlineData("999.0.0", "998.5.0-beta", "999.0.0")]
    public async Task CheckForUpdatesAsync_ShouldPickTheHighestOfAllTags_WhenTrackIsBeta(string first, string second, string expected)
    {
        SetupTrack(VersionTrack.Beta);
        SetupTags(first, second);

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.AvailableVersion, expected), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.Available, true), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReportNoUpdate_WhenLatestVersionIsNotNewer()
    {
        SetupTrack(VersionTrack.Stable);
        SetupTags("0.0.0");

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.AvailableVersion, "0.0.0"), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.Available, false), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, string.Empty), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistError_WhenNoSemanticVersionTagsFound()
    {
        SetupTrack(VersionTrack.Stable);
        SetupTags("latest", "dev", "1.2");

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, "No valid versions found"), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistError_WhenNoVersionsMatchTrack()
    {
        SetupTrack(VersionTrack.Stable);
        SetupTags("1.0.0-beta", "2.0.0-beta");

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, "No Stable versions found"), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistError_WhenApiReturnsNonSuccess()
    {
        SetupTrack(VersionTrack.Stable);
        var response = new ApiResponse<DockerHubTagsResponse>(
            new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = new HttpRequestMessage() },
            null,
            new RefitSettings());

        _dockerHubApiMock
            .Setup(x => x.GetTags("uping", "boardgametracker", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, "Docker Hub API returned status InternalServerError"), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistApiError_WhenApiThrowsApiException()
    {
        SetupTrack(VersionTrack.Stable);
        var apiException = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://hub.docker.com"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new RefitSettings());

        _dockerHubApiMock
            .Setup(x => x.GetTags("uping", "boardgametracker", 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        await _updateService.CheckForUpdatesAsync();

        VerifyCheckPreamble();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, It.Is<string>(s => s.StartsWith("API Error:"))), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldPersistError_WhenTrackLookupThrows()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track))
            .ThrowsAsync(new InvalidOperationException("config unavailable"));

        await _updateService.CheckForUpdatesAsync();

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<VersionTrack>(UpdateConfig.Track), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckError, "config unavailable"), Times.Once);
        VerifyLastRunPersisted();
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetCurrentVersion Tests

    [Fact]
    public void GetCurrentVersion_ShouldReturnThreePartVersion()
    {
        var result = _updateService.GetCurrentVersion();

        result.Should().NotBeNullOrEmpty();
        result.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    #endregion
}
