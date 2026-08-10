using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class ImageServiceTests : IDisposable
    {
        private readonly Mock<IDiskProvider> _diskProviderMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<ImageService>> _loggerMock;
        private readonly ImageService _imageService;
        private readonly string _testDirectory;

        public ImageServiceTests()
        {
            _diskProviderMock = new Mock<IDiskProvider>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<ImageService>>();
            _imageService = new ImageService(_diskProviderMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);
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
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task SaveImage_ShouldReturnGameImagePath_WhenTypeIsGame()
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
            const string expectedFileName = "unique-game-image.webp";

            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), "test.webp", PathHelper.FullCoverImagePath, It.IsAny<IImageEncoder?>()))
                           .ReturnsAsync(expectedFileName);

            var result = await _imageService.SaveImage(formFile, UploadFileType.Game);

            result.Should().Be($"/{PathHelper.CoverImagePath}/{expectedFileName}".Replace("\\", "/"));
            _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Image>(), "test.webp", PathHelper.FullCoverImagePath, It.IsAny<IImageEncoder?>()), Times.Once);
            _diskProviderMock.VerifyNoOtherCalls();
        }
        
        [Fact]
        public async Task SaveImage_ShouldReturnProfileImagePath_WhenTypeIsProfile()
        {
            var formFile = CreateMockFormFile("profile.png", CreateTestImageBytes());
            const string expectedFileName = "unique-profile-image.webp";
        
            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), "profile.webp", PathHelper.FullProfileImagePath, It.IsAny<IImageEncoder?>()))
                           .ReturnsAsync(expectedFileName);

            var result = await _imageService.SaveImage(formFile, UploadFileType.Profile);

            result.Should().Be($"/{PathHelper.ProfileImagePath}/{expectedFileName}".Replace("\\", "/"));
            _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Image>(), "profile.webp", PathHelper.FullProfileImagePath, It.IsAny<IImageEncoder?>()), Times.Once);
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
        
        [Fact]
        public void DeleteImage_ShouldMapWebPathToPhysicalPath_WhenUnderImagesRoot()
        {
            const string imagePath = "/images/cover/test.jpg";
            var expectedPhysical = PathHelper.MapImageWebPathToPhysical(imagePath);

            _imageService.DeleteImage(imagePath);

            expectedPhysical.Should().NotBeNull();
            _diskProviderMock.Verify(x => x.DeleteFile(expectedPhysical!), Times.Once);
            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void DeleteImage_ShouldNotCallDiskProvider_WhenImagePathIsNull()
        {
            _imageService.DeleteImage(null);

            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("/profiles/user.png")]
        [InlineData(@"C:\temp\image.gif")]
        [InlineData("/images/../../secret.txt")]
        [InlineData("")]
        [InlineData("   ")]
        public void DeleteImage_ShouldNotCallDiskProvider_WhenPathIsInvalidOrOutsideImagesRoot(string imagePath)
        {
            _imageService.DeleteImage(imagePath);

            _diskProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SaveImage_ShouldResizeImageTo512x512_WhenProcessingFormFile()
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
            Image? capturedImage = null;

            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IImageEncoder?>()))
                           .Callback<Image, string, string, IImageEncoder?>((img, name, path, enc) => capturedImage = img)
                           .ReturnsAsync("result.jpg");

            await _imageService.SaveImage(formFile, UploadFileType.Game);

            capturedImage.Should().NotBeNull();
            capturedImage!.Width.Should().Be(512);
            capturedImage.Height.Should().Be(512);
        }

        [Fact]
        public async Task SaveImage_ShouldEncodeGameCoverAsWebp_WhenTypeIsGame()
        {
            var formFile = CreateMockFormFile("cover.png", CreateTestImageBytes());
            string? capturedName = null;
            IImageEncoder? capturedEncoder = null;

            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IImageEncoder?>()))
                           .Callback<Image, string, string, IImageEncoder?>((_, name, _, enc) =>
                           {
                               capturedName = name;
                               capturedEncoder = enc;
                           })
                           .ReturnsAsync("cover.webp");

            await _imageService.SaveImage(formFile, UploadFileType.Game);

            capturedName.Should().Be("cover.webp");
            capturedEncoder.Should().BeOfType<WebpEncoder>();
        }

        [Fact]
        public async Task SaveImage_ShouldEncodeProfileAsWebp_WhenTypeIsProfile()
        {
            var formFile = CreateMockFormFile("avatar.png", CreateTestImageBytes());
            string? capturedName = null;
            IImageEncoder? capturedEncoder = null;

            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IImageEncoder?>()))
                           .Callback<Image, string, string, IImageEncoder?>((_, name, _, enc) =>
                           {
                               capturedName = name;
                               capturedEncoder = enc;
                           })
                           .ReturnsAsync("avatar.webp");

            await _imageService.SaveImage(formFile, UploadFileType.Profile);

            capturedName.Should().Be("avatar.webp");
            capturedEncoder.Should().BeOfType<WebpEncoder>();
        }

        [Fact]
        public async Task SaveImage_ShouldThrowException_WhenDiskProviderThrows()
        {
            var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
            var expectedException = new IOException("Disk error");
        
            _diskProviderMock.Setup(x => x.WriteFile(It.IsAny<Image>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IImageEncoder?>()))
                           .ThrowsAsync(expectedException);
        
            var exception = await Assert.ThrowsAsync<IOException>(
                () => _imageService.SaveImage(formFile, UploadFileType.Game));
        
            exception.Should().Be(expectedException);
        }

        [Fact]
        public void ClearAllImages_ShouldClearCoverAndProfileFolders()
        {
            _imageService.ClearAllImages();

            _diskProviderMock.Verify(x => x.ClearFolder(PathHelper.FullCoverImagePath), Times.Once);
            _diskProviderMock.Verify(x => x.ClearFolder(PathHelper.FullProfileImagePath), Times.Once);
            _diskProviderMock.VerifyNoOtherCalls();
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

        private static void SetupPathHelper()
        {
            Directory.CreateDirectory(PathHelper.FullCoverImagePath);
            Directory.CreateDirectory(PathHelper.FullProfileImagePath);
            Directory.CreateDirectory(PathHelper.FullRootImagePath);
        }

        private static void SetupNoImageFile()
        {
            var noImagePath = Path.Combine(PathHelper.FullRootImagePath, "no-image.jpg");
            using var noImage = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(50, 50);
            noImage.SaveAsJpeg(noImagePath);
        }
}