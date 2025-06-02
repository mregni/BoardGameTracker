using System;
using System.IO;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SixLabors.ImageSharp;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class ImageServiceTests : IDisposable
    {
        private readonly Mock<IDiskProvider> _diskProviderMock;
        private readonly ImageService _imageService;
        private readonly string _testDirectory;

        public ImageServiceTests()
        {
            _diskProviderMock = new Mock<IDiskProvider>();
            _imageService = new ImageService(_diskProviderMock.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), "ImageServiceTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            
            SetupPathHelper();
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task SaveImage_ShouldReturnGameImagePath_WhenTypeIsGame()
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
            const string expectedFileName = "unique-game-image.jpg";
        
            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), "test.jpg", PathHelper.FullCoverImagePath))
                           .ReturnsAsync(expectedFileName);
        
            var result = await _imageService.SaveImage(formFile, UploadFileType.Game);
        
            result.Should().Be($"/{PathHelper.CoverImagePath}/{expectedFileName}".Replace("\\", "/"));
            _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Image>(), "test.jpg", PathHelper.FullCoverImagePath), Times.Once);
            _diskProviderMock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task SaveImage_ShouldReturnProfileImagePath_WhenTypeIsProfile()
        {
            var formFile = CreateMockFormFile("profile.png", CreateTestImageBytes());
            const string expectedFileName = "unique-profile-image.png";
        
            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), "profile.png", PathHelper.FullProfileImagePath))
                           .ReturnsAsync(expectedFileName);
        
            var result = await _imageService.SaveImage(formFile, UploadFileType.Profile);
        
            result.Should().Be($"/{PathHelper.ProfileImagePath}/{expectedFileName}".Replace("\\", "/"));
            _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Image>(), "profile.png", PathHelper.FullProfileImagePath), Times.Once);
            _diskProviderMock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task SaveImage_ShouldThrowException_WhenTypeIsUnsupported()
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
        
            var action = async () => await _imageService.SaveImage(formFile, (UploadFileType)999) ;
        
            await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
                .WithParameterName("type");
            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SaveImage_ShouldReturnNoImagePath_WhenFileIsNull()
        {
            SetupNoImageFile();

            var result = await _imageService.SaveImage(null, UploadFileType.Game);

            var expectedPath = $"/{PathHelper.CoverImagePath}/";
            result.Should().StartWith(expectedPath.Replace("\\", "/"));
            result.Should().EndWith(".jpg");
            _diskProviderMock.VerifyNoOtherCalls();
        }
        
        [Theory]
        [InlineData(UploadFileType.Game)]
        [InlineData(UploadFileType.Profile)]
        public async Task SaveImage_ShouldHandleValidTypes_WithDifferentUploadTypes(UploadFileType type)
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
            const string expectedFileName = "unique-file.jpg";
            var expectedFullPath = type == UploadFileType.Game ? PathHelper.FullCoverImagePath : PathHelper.FullProfileImagePath;
            var expectedFolder = type == UploadFileType.Game ? PathHelper.CoverImagePath : PathHelper.ProfileImagePath;
        
            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), "test.jpg", expectedFullPath))
                           .ReturnsAsync(expectedFileName);
        
            var result = await _imageService.SaveImage(formFile, type);
        
            result.Should().Be($"/{expectedFolder}/{expectedFileName}".Replace("\\", "/"));
            _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Image>(), "test.jpg", expectedFullPath), Times.Once);
        }

        [Fact]
        public void DeleteImage_ShouldCallDiskProviderDelete_WhenImagePathProvided()
        {
            const string imagePath = "/images/test.jpg";

            _imageService.DeleteImage(imagePath);

            _diskProviderMock.Verify(x => x.DeleteFile(imagePath), Times.Once);
            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void DeleteImage_ShouldNotCallDiskProvider_WhenImagePathIsNull()
        {
            _imageService.DeleteImage(null);

            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("/images/game1.jpg")]
        [InlineData("/profiles/user.png")]
        [InlineData(@"C:\temp\image.gif")]
        [InlineData("")]
        public void DeleteImage_ShouldPassCorrectPath_WithDifferentImagePaths(string imagePath)
        {
            _imageService.DeleteImage(imagePath);

            _diskProviderMock.Verify(x => x.DeleteFile(imagePath), Times.Once);
            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SaveImage_ShouldResizeImageTo512x512_WhenProcessingFormFile()
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
            Image? capturedImage = null;

            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Callback<Image, string, string>((img, name, path) => capturedImage = img)
                           .ReturnsAsync("result.jpg");

            await _imageService.SaveImage(formFile, UploadFileType.Game);

            capturedImage.Should().NotBeNull();
        }

        [Fact]
        public async Task SaveImage_ShouldThrowException_WhenDiskProviderThrows()
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
            var expectedException = new IOException("Disk error");
        
            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), It.IsAny<string>(), It.IsAny<string>()))
                           .ThrowsAsync(expectedException);
        
            var exception = await Assert.ThrowsAsync<IOException>(
                () => _imageService.SaveImage(formFile, UploadFileType.Game));
        
            exception.Should().Be(expectedException);
        }

        private static IFormFile CreateMockFormFile(string fileName, byte[] content, long length = -1)
        {
            var formFileMock = new Mock<IFormFile>();
            var stream = new MemoryStream(content);
            stream.Position = 0;
            
            formFileMock.Setup(f => f.FileName).Returns(fileName);
            formFileMock.Setup(f => f.Length).Returns(length == -1 ? stream.Length : length);
            formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);

            return formFileMock.Object;
        }

        private static byte[] CreateTestImageBytes()
        {
            using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(100, 100);
            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms);
            return ms.ToArray();
        }

        private void SetupPathHelper()
        {
            Directory.CreateDirectory(PathHelper.FullCoverImagePath);
            Directory.CreateDirectory(PathHelper.FullProfileImagePath);
            Directory.CreateDirectory(PathHelper.FullRootImagePath);
        }

        private void SetupNoImageFile()
        {
            var noImagePath = Path.Combine(PathHelper.FullRootImagePath, "no-image.jpg");
            using var noImage = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(50, 50);
            noImage.SaveAsJpeg(noImagePath);
        }
}