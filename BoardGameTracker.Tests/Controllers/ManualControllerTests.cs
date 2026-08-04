using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Manuals.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class ManualControllerTests
{
    private readonly Mock<IManualService> _manualServiceMock;
    private readonly ManualController _controller;

    public ManualControllerTests()
    {
        _manualServiceMock = new Mock<IManualService>();
        _controller = new ManualController(_manualServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _manualServiceMock.VerifyNoOtherCalls();
    }

    private static Manual CreateManual(int id, int gameId)
    {
        return new Manual("rulebook.pdf", "stored-rulebook.pdf", "application/pdf", 1024, gameId, DateTime.UtcNow) { Id = id };
    }

    [Fact]
    public async Task GetManualsForGame_ShouldReturnOkWithDtos()
    {
        _manualServiceMock
            .Setup(x => x.GetManualsForGame(5))
            .ReturnsAsync(new List<Manual> { CreateManual(1, 5) });

        var result = await _controller.GetManualsForGame(5);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<List<ManualDto>>().Which.Should().HaveCount(1);

        _manualServiceMock.Verify(x => x.GetManualsForGame(5), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldReturnOkWithDtos()
    {
        var command = new UploadManualsCommand { Files = new List<IFormFile>() };
        _manualServiceMock
            .Setup(x => x.UploadManuals(5, command.Files))
            .ReturnsAsync(new List<Manual> { CreateManual(1, 5) });

        var result = await _controller.UploadManuals(5, command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<List<ManualDto>>().Which.Should().HaveCount(1);

        _manualServiceMock.Verify(x => x.UploadManuals(5, command.Files), Times.Once);
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
        _manualServiceMock
            .Setup(x => x.GetManualForDownload(3))
            .ReturnsAsync(new ManualDownload { Stream = new MemoryStream(), ContentType = "application/pdf", FileName = "Catan.pdf" });

        var result = await _controller.DownloadManual(3);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("Catan.pdf");

        _manualServiceMock.Verify(x => x.GetManualForDownload(3), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldReturnOk()
    {
        var linkId = Guid.NewGuid();
        _manualServiceMock
            .Setup(x => x.GetManualsForGameNight(linkId))
            .ReturnsAsync(new List<GameNightManualsDto>());

        var result = await _controller.GetManualsForGameNight(linkId);

        result.Should().BeOfType<OkObjectResult>();

        _manualServiceMock.Verify(x => x.GetManualsForGameNight(linkId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadGameNightManual_ShouldReturnPdfFile()
    {
        var linkId = Guid.NewGuid();
        _manualServiceMock
            .Setup(x => x.GetManualForGameNightDownload(linkId, 11))
            .ReturnsAsync(new ManualDownload { Stream = new MemoryStream(), ContentType = "application/pdf", FileName = "Catan.pdf" });

        var result = await _controller.DownloadGameNightManual(linkId, 11);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("Catan.pdf");

        _manualServiceMock.Verify(x => x.GetManualForGameNightDownload(linkId, 11), Times.Once);
        VerifyNoOtherCalls();
    }
}
