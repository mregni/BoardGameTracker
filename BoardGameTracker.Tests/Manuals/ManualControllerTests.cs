using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Manuals.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Manuals;

public class ManualControllerTests
{
    private readonly Mock<IManualService> _manualServiceMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly ManualController _controller;
    private readonly CancellationToken _token = new CancellationTokenSource().Token;

    public ManualControllerTests()
    {
        _manualServiceMock = new Mock<IManualService>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _controller = new ManualController(_manualServiceMock.Object, _environmentProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _manualServiceMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void UploadManuals_ShouldAcceptARequestOfOneGibibyte()
    {
        var upload = typeof(ManualController).GetMethod(nameof(ManualController.UploadManuals))!;

        upload.GetCustomAttribute<RequestSizeLimitAttribute>().Should().BeAssignableTo<IRequestSizeLimitMetadata>()
            .Which.MaxRequestBodySize.Should().Be(1_073_741_824);
        upload.GetCustomAttribute<RequestFormLimitsAttribute>()!.MultipartBodyLengthLimit.Should().Be(1_073_741_824);
        VerifyNoOtherCalls();
    }

    private static Manual CreateManual(int id, int gameId)
    {
        return new Manual("rulebook.pdf", "stored-rulebook.pdf", "application/pdf", 1024, gameId, new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc)) { Id = id };
    }

    [Fact]
    public async Task GetManualsForGame_ShouldReturnOkWithDtos()
    {
        _manualServiceMock
            .Setup(x => x.GetManualsForGame(5))
            .ReturnsAsync(new List<Manual> { CreateManual(1, 5) });

        var result = await _controller.GetManualsForGame(5);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<List<ManualDto>>().Which.Should().ContainSingle().Subject;
        dto.Id.Should().Be(1);
        dto.GameId.Should().Be(5);
        dto.Title.Should().Be("rulebook.pdf");

        _manualServiceMock.Verify(x => x.GetManualsForGame(5), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldReturnCreatedWithDtos()
    {
        var command = new UploadManualsCommand { Files = new List<IFormFile>() };
        _manualServiceMock
            .Setup(x => x.UploadManuals(5, command.Files))
            .ReturnsAsync(new List<Manual> { CreateManual(1, 5) });

        var result = await _controller.UploadManuals(5, command);

        var okResult = result.Should().BeOfType<CreatedResult>().Subject;
        okResult.Location.Should().BeNull();
        var dto = okResult.Value.Should().BeOfType<List<ManualDto>>().Which.Should().ContainSingle().Subject;
        dto.Id.Should().Be(1);
        dto.GameId.Should().Be(5);

        _manualServiceMock.Verify(x => x.UploadManuals(5, command.Files), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReindexManual_ShouldReturnNoContent_WhenRagEnabled()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(true);

        var result = await _controller.ReindexManual(9);

        result.Should().BeOfType<NoContentResult>();

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _manualServiceMock.Verify(x => x.RequeueManualForIndexing(9), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReindexManual_ShouldReturnNotFound_WhenRagDisabled()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(false);

        var result = await _controller.ReindexManual(9);

        result.Should().BeOfType<NotFoundResult>();

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteManual_ShouldReturnNoContent()
    {
        var result = await _controller.DeleteManual(7);

        result.Should().BeOfType<NoContentResult>();

        _manualServiceMock.Verify(x => x.DeleteManual(7), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadManual_ShouldReturnPdfFile()
    {
        var stream = new MemoryStream();
        _manualServiceMock
            .Setup(x => x.GetManualForDownload(3))
            .ReturnsAsync(new ManualDownload { Stream = stream, ContentType = "application/pdf", FileName = "Catan.pdf" });

        var result = await _controller.DownloadManual(3);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.FileStream.Should().BeSameAs(stream);
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("Catan.pdf");
        fileResult.EnableRangeProcessing.Should().BeTrue();

        _manualServiceMock.Verify(x => x.GetManualForDownload(3), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldReturnNotFound_WhenRagDisabled()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(false);

        var result = await _controller.GetManualPageImage(3, 2, _token);

        result.Should().BeOfType<NotFoundResult>();

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldReturnPng_WhenRendered()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(true);
        var stream = new MemoryStream();
        _manualServiceMock
            .Setup(x => x.GetManualPageImage(3, 2, _token))
            .ReturnsAsync(new ManualDownload { Stream = stream, ContentType = "image/png", FileName = "page-2.png" });

        var result = await _controller.GetManualPageImage(3, 2, _token);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.FileStream.Should().BeSameAs(stream);
        fileResult.ContentType.Should().Be("image/png");
        fileResult.FileDownloadName.Should().BeEmpty();

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _manualServiceMock.Verify(x => x.GetManualPageImage(3, 2, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldReturnNotFound_WhenImageUnavailable()
    {
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(true);
        _manualServiceMock
            .Setup(x => x.GetManualPageImage(3, 2, _token))
            .ReturnsAsync((ManualDownload?)null);

        var result = await _controller.GetManualPageImage(3, 2, _token);

        result.Should().BeOfType<NotFoundResult>();

        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _manualServiceMock.Verify(x => x.GetManualPageImage(3, 2, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldReturnOk()
    {
        var linkId = Guid.NewGuid();
        var manuals = new List<GameNightManualsDto>();
        _manualServiceMock
            .Setup(x => x.GetManualsForGameNight(linkId, false))
            .ReturnsAsync(manuals);

        var result = await _controller.GetManualsForGameNight(linkId);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(manuals);

        _manualServiceMock.Verify(x => x.GetManualsForGameNight(linkId, false), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadGameNightManual_ShouldReturnPdfFile()
    {
        var linkId = Guid.NewGuid();
        var stream = new MemoryStream();
        _manualServiceMock
            .Setup(x => x.GetManualForGameNightDownload(linkId, 11, false))
            .ReturnsAsync(new ManualDownload { Stream = stream, ContentType = "application/pdf", FileName = "Catan.pdf" });

        var result = await _controller.DownloadGameNightManual(linkId, 11);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.FileStream.Should().BeSameAs(stream);
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("Catan.pdf");

        _manualServiceMock.Verify(x => x.GetManualForGameNightDownload(linkId, 11, false), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGamesWithManuals_ShouldReturnTheGameIds()
    {
        _manualServiceMock
            .Setup(x => x.GetGameIdsWithManuals(_token))
            .ReturnsAsync([1, 3]);

        var result = await _controller.GetGamesWithManuals(_token);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new List<int> { 1, 3 });
        _manualServiceMock.Verify(x => x.GetGameIdsWithManuals(_token), Times.Once);
        VerifyNoOtherCalls();
    }

    private void SignIn(bool isAuthenticated)
    {
        var identity = isAuthenticated ? new ClaimsIdentity("test") : new ClaimsIdentity();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetManualsForGameNight_ShouldPassWhetherTheVisitorIsSignedIn(bool isAuthenticated)
    {
        SignIn(isAuthenticated);
        var linkId = Guid.NewGuid();
        var manuals = new List<GameNightManualsDto>();
        _manualServiceMock.Setup(x => x.GetManualsForGameNight(linkId, isAuthenticated)).ReturnsAsync(manuals);

        var result = await _controller.GetManualsForGameNight(linkId);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(manuals);
        _manualServiceMock.Verify(x => x.GetManualsForGameNight(linkId, isAuthenticated), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldTreatAPrincipalWithoutIdentityAsAnonymous()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
        var linkId = Guid.NewGuid();
        var manuals = new List<GameNightManualsDto>();
        _manualServiceMock.Setup(x => x.GetManualsForGameNight(linkId, false)).ReturnsAsync(manuals);

        var result = await _controller.GetManualsForGameNight(linkId);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(manuals);
        _manualServiceMock.Verify(x => x.GetManualsForGameNight(linkId, false), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DownloadGameNightManual_ShouldPassWhetherTheVisitorIsSignedIn(bool isAuthenticated)
    {
        SignIn(isAuthenticated);
        var linkId = Guid.NewGuid();
        var stream = new MemoryStream();
        _manualServiceMock
            .Setup(x => x.GetManualForGameNightDownload(linkId, 11, isAuthenticated))
            .ReturnsAsync(new ManualDownload { Stream = stream, ContentType = "application/pdf", FileName = "Catan.pdf" });

        var result = await _controller.DownloadGameNightManual(linkId, 11);

        result.Should().BeOfType<FileStreamResult>().Which.FileStream.Should().BeSameAs(stream);
        _manualServiceMock.Verify(x => x.GetManualForGameNightDownload(linkId, 11, isAuthenticated), Times.Once);
        VerifyNoOtherCalls();
    }
}
