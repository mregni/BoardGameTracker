using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
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

        [Fact]
        public async Task UploadImage_ShouldReturnOkResultWithImageName_WhenFileIsNull()
        {
            const string expectedImageName = "default-image.jpg";
            var upload = new FileUploadViewModel
            {
                File = null,
                Type = UploadFileType.Game
            };

            _imageServiceMock
                .Setup(x => x.SaveImage(null, upload.Type))
                .ReturnsAsync(expectedImageName);

            var result = await _controller.UploadImage(upload);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(expectedImageName);

            _imageServiceMock.Verify(x => x.SaveImage(null, upload.Type), Times.Once);
            _imageServiceMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(UploadFileType.Profile)]
        [InlineData(UploadFileType.Game)]
        public async Task UploadImage_ShouldHandleDifferentFileTypes_WhenValidUploadProvided(UploadFileType fileType)
        {
            const string expectedImageName = "test-image.png";
            var formFile = CreateMockFormFile("test.png", "image/png", "test content");
            var upload = new FileUploadViewModel
            {
                File = formFile,
                Type = fileType
            };

            _imageServiceMock
                .Setup(x => x.SaveImage(upload.File, fileType))
                .ReturnsAsync(expectedImageName);

            var result = await _controller.UploadImage(upload);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(expectedImageName);

            _imageServiceMock.Verify(x => x.SaveImage(upload.File, fileType), Times.Once);
            _imageServiceMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UploadImage_ShouldReturnInternalServerError_WhenImageServiceThrowsException()
        {
            var expectedException = new InvalidOperationException("Failed to save image");
            var formFile = CreateMockFormFile("test.jpg", "image/jpeg", "test content");
            var upload = new FileUploadViewModel
            {
                File = formFile,
                Type = UploadFileType.Profile
            };

            _imageServiceMock
                .Setup(x => x.SaveImage(upload.File, upload.Type))
                .ThrowsAsync(expectedException);

            var result = await _controller.UploadImage(upload);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult!.StatusCode.Should().Be(500);

            _imageServiceMock.Verify(x => x.SaveImage(upload.File, upload.Type), Times.Once);
            _imageServiceMock.VerifyNoOtherCalls();
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error while uploading image")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UploadImage_ShouldReturnEmptyStringResult_WhenServiceReturnsEmptyString()
        {
            var formFile = CreateMockFormFile("test.jpg", "image/jpeg", "content");
            var upload = new FileUploadViewModel
            {
                File = formFile,
                Type = UploadFileType.Profile
            };

            _imageServiceMock
                .Setup(x => x.SaveImage(upload.File, upload.Type))
                .ReturnsAsync(string.Empty);

            var result = await _controller.UploadImage(upload);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(string.Empty);

            _imageServiceMock.Verify(x => x.SaveImage(upload.File, upload.Type), Times.Once);
            _imageServiceMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        private static IFormFile CreateMockFormFile(string fileName, string contentType, string content)
        {
            var formFileMock = new Mock<IFormFile>();
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            
            formFileMock.Setup(f => f.FileName).Returns(fileName);
            formFileMock.Setup(f => f.ContentType).Returns(contentType);
            formFileMock.Setup(f => f.Length).Returns(stream.Length);
            formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                       .Returns((Stream target, CancellationToken token) => stream.CopyToAsync(target, token));

            return formFileMock.Object;
        }
    }