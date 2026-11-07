using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Images;

public class ImageControllerTests
{
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly ImageController _controller;

    public ImageControllerTests()
    {
        _imageServiceMock = new Mock<IImageService>();
        _controller = new ImageController(_imageServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _imageServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadImage_ShouldReturnOkWithFileName_WhenUploadSucceeds()
    {
        var formFile = new FormFile(Stream.Null, 0, 0, "file", "test.jpg");
        var command = new UploadImageCommand
        {
            File = formFile,
            Type = UploadFileType.Game
        };
        var expectedFileName = "saved-image-123.jpg";

        _imageServiceMock
            .Setup(x => x.SaveImage(formFile, UploadFileType.Game))
            .ReturnsAsync(expectedFileName);

        var result = await _controller.UploadImage(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedFileName);

        _imageServiceMock.Verify(x => x.SaveImage(formFile, UploadFileType.Game), Times.Once);
        VerifyNoOtherCalls();
    }

}
