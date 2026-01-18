using System;
using System.IO;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class ImageControllerTests
{
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<ILogger<ImageController>> _loggerMock;
    private readonly ImageController _controller;

    public ImageControllerTests()
    {
        _imageServiceMock = new Mock<IImageService>();
        _loggerMock = new Mock<ILogger<ImageController>>();
        _controller = new ImageController(_imageServiceMock.Object, _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _imageServiceMock.VerifyNoOtherCalls();
    }

    private static IFormFile CreateMockFormFile(string fileName = "test.jpg", string contentType = "image/jpeg")
    {
        var content = "fake image content";
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;

        var file = new Mock<IFormFile>();
        file.Setup(f => f.OpenReadStream()).Returns(stream);
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(stream.Length);
        file.Setup(f => f.ContentType).Returns(contentType);
        return file.Object;
    }

    [Fact]
    public async Task UploadImage_ShouldReturnOkWithFileName_WhenUploadSucceeds()
    {
        // Arrange
        var formFile = CreateMockFormFile();
        var command = new UploadImageCommand
        {
            File = formFile,
            Type = UploadFileType.Game
        };
        var expectedFileName = "saved-image-123.jpg";

        _imageServiceMock
            .Setup(x => x.SaveImage(formFile, UploadFileType.Game))
            .ReturnsAsync(expectedFileName);

        // Act
        var result = await _controller.UploadImage(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedFileName);

        _imageServiceMock.Verify(x => x.SaveImage(formFile, UploadFileType.Game), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadImage_ShouldReturnOkWithFileName_WhenUploadingProfileImage()
    {
        // Arrange
        var formFile = CreateMockFormFile("profile.png", "image/png");
        var command = new UploadImageCommand
        {
            File = formFile,
            Type = UploadFileType.Profile
        };
        var expectedFileName = "profile-image-456.png";

        _imageServiceMock
            .Setup(x => x.SaveImage(formFile, UploadFileType.Profile))
            .ReturnsAsync(expectedFileName);

        // Act
        var result = await _controller.UploadImage(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedFileName);

        _imageServiceMock.Verify(x => x.SaveImage(formFile, UploadFileType.Profile), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadImage_ShouldReturnInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var formFile = CreateMockFormFile();
        var command = new UploadImageCommand
        {
            File = formFile,
            Type = UploadFileType.Game
        };

        _imageServiceMock
            .Setup(x => x.SaveImage(formFile, UploadFileType.Game))
            .ThrowsAsync(new IOException("Disk full"));

        // Act
        var result = await _controller.UploadImage(command);

        // Assert
        var statusCodeResult = result.Should().BeOfType<StatusCodeResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _imageServiceMock.Verify(x => x.SaveImage(formFile, UploadFileType.Game), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadImage_ShouldReturnInternalServerError_WhenUnauthorizedAccessException()
    {
        // Arrange
        var formFile = CreateMockFormFile();
        var command = new UploadImageCommand
        {
            File = formFile,
            Type = UploadFileType.Game
        };

        _imageServiceMock
            .Setup(x => x.SaveImage(formFile, UploadFileType.Game))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.UploadImage(command);

        // Assert
        var statusCodeResult = result.Should().BeOfType<StatusCodeResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _imageServiceMock.Verify(x => x.SaveImage(formFile, UploadFileType.Game), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadImage_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var formFile = CreateMockFormFile("game-cover.jpg");
        var command = new UploadImageCommand
        {
            File = formFile,
            Type = UploadFileType.Game
        };

        _imageServiceMock
            .Setup(x => x.SaveImage(formFile, UploadFileType.Game))
            .ReturnsAsync("game-image.jpg");

        // Act
        await _controller.UploadImage(command);

        // Assert
        _imageServiceMock.Verify(x => x.SaveImage(
            It.Is<IFormFile>(f => f.FileName == "game-cover.jpg"),
            UploadFileType.Game), Times.Once);
        VerifyNoOtherCalls();
    }
}
