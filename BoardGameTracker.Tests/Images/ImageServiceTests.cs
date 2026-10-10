using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SkiaSharp;
using Xunit;

namespace BoardGameTracker.Tests.Images;

public class ImageServiceTests : IDisposable
{
    private readonly Mock<IDiskProvider> _diskProviderMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ImageService>> _loggerMock;
    private readonly ImageService _imageService;
    private readonly List<string> _filesToCleanup = [];

    public ImageServiceTests()
    {
        _diskProviderMock = new Mock<IDiskProvider>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ImageService>>();
        _imageService = new ImageService(_diskProviderMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);

        Directory.CreateDirectory(PathHelper.FullCoverImagePath);
        Directory.CreateDirectory(PathHelper.FullProfileImagePath);
        Directory.CreateDirectory(PathHelper.FullRootImagePath);
    }

    public void Dispose()
    {
        foreach (var file in _filesToCleanup.Where(File.Exists))
        {
            File.Delete(file);
        }
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(UploadFileType.Game)]
    [InlineData(UploadFileType.Profile)]
    public async Task SaveImage_ShouldResizeEncodeAsWebpAndReturnPath(UploadFileType type)
    {
        var formFile = CreateMockFormFile("upload.png", CreateTestImageBytes());
        var expectedFullPath = type == UploadFileType.Game ? PathHelper.FullCoverImagePath : PathHelper.FullProfileImagePath;
        var expectedFolder = type == UploadFileType.Game ? PathHelper.CoverImagePath : PathHelper.ProfileImagePath;
        byte[]? written = null;

        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "upload.webp", expectedFullPath))
            .Callback<Stream, string, string>((stream, _, _) => written = ReadAll(stream))
            .ReturnsAsync("unique-upload.webp");

        var result = await _imageService.SaveImage(formFile, type);

        result.Should().Be($"/{expectedFolder}/unique-upload.webp".Replace("\\", "/"));
        AssertResizedWebp(written);
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "upload.webp", expectedFullPath), Times.Once);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowException_WhenTypeIsUnsupported()
    {
        var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());

        var action = async () => await _imageService.SaveImage(formFile, (UploadFileType)999);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("type");
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveImage_ShouldReturnNoImage_WhenFileIsNullOrEmpty(bool fileIsNull)
    {
        var formFile = fileIsNull ? null : CreateMockFormFile("empty.png", []);

        var result = await _imageService.SaveImage(formFile, UploadFileType.Game);

        result.Should().BeEmpty();
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowValidationException_WhenFileExceedsMaxUploadSize()
    {
        var formFile = CreateMockFormFile("big.jpg", CreateTestImageBytes(), 16 * 1024 * 1024);

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.ImageTooLarge);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowValidationException_WhenImageExceedsPixelLimit()
    {
        var formFile = CreateMockFormFile("huge.bmp", CreateOversizedBitmapBytes());

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.ImageTooLarge);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowValidationException_WhenFileIsNotAnImage()
    {
        var formFile = CreateMockFormFile("not-image.jpg", [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.ImageUnsupportedFormat);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowException_WhenDiskProviderThrows()
    {
        var formFile = CreateMockFormFile("test.jpg", CreateTestImageBytes());
        var expectedException = new IOException("Disk error");

        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        (await action.Should().ThrowAsync<IOException>()).Which.Should().Be(expectedException);
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldResizeAndWriteWebpFile_WhenDownloadSucceeds()
    {
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(CreateTestImageBytes())
        });

        byte[]? written = null;
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath))
            .Callback<Stream, string, string>((stream, _, _) => written = ReadAll(stream))
            .ReturnsAsync("unique-cover.webp");

        var result = await _imageService.DownloadImage("https://example.com/image.jpg", "cover-file");

        result.Should().Be($"/{PathHelper.CoverImagePath}/unique-cover.webp".Replace("\\", "/"));
        AssertResizedWebp(written);
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath), Times.Once);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DownloadImage_ShouldReturnNoImageWithoutARequest_WhenUrlIsEmpty(string imageUrl)
    {
        var result = await _imageService.DownloadImage(imageUrl, "cover-file");

        result.Should().BeEmpty();
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenContentIsNotAnImage()
    {
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08])
        });

        var result = await _imageService.DownloadImage("https://example.com/page.html", "cover-file");

        result.Should().BeEmpty();
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenImageExceedsPixelLimit()
    {
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(CreateOversizedBitmapBytes())
        });

        var result = await _imageService.DownloadImage("https://example.com/huge.bmp", "cover-file");

        result.Should().BeEmpty();
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenResponseIsNotSuccessful()
    {
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _imageService.DownloadImage("https://example.com/missing.jpg", "cover-file");

        result.Should().BeEmpty();
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenContentLengthExceedsLimit()
    {
        SetupHttpResponse(() =>
        {
            var content = new ByteArrayContent(CreateTestImageBytes());
            content.Headers.ContentLength = 16 * 1024 * 1024;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        var result = await _imageService.DownloadImage("https://example.com/huge.jpg", "cover-file");

        result.Should().BeEmpty();
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenStreamedContentExceedsLimit()
    {
        SetupHttpResponse(() =>
        {
            var content = new StreamContent(new MemoryStream(new byte[16 * 1024 * 1024]));
            content.Headers.ContentLength = null;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        var result = await _imageService.DownloadImage("https://example.com/huge-stream.jpg", "cover-file");

        result.Should().BeEmpty();
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenRequestThrows()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object));

        var result = await _imageService.DownloadImage("https://example.com/error.jpg", "cover-file");

        result.Should().BeEmpty();
        _diskProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void DeleteImage_ShouldMapWebPathToPhysicalPath_WhenUnderImagesRoot()
    {
        const string imagePath = "/images/cover/test.jpg";
        var expectedPhysical = PathHelper.MapImageWebPathToPhysical(imagePath);

        _imageService.DeleteImage(imagePath);

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
    public void ClearAllImages_ShouldClearCoverAndProfileFolders()
    {
        _imageService.ClearAllImages();

        _diskProviderMock.Verify(x => x.ClearFolder(PathHelper.FullCoverImagePath), Times.Once);
        _diskProviderMock.Verify(x => x.ClearFolder(PathHelper.FullProfileImagePath), Times.Once);
        _diskProviderMock.VerifyNoOtherCalls();
    }

    private void SetupHttpResponse(Func<HttpResponseMessage> responseFactory)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseFactory);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object));
    }

    private void TrackWebPathForCleanup(string webPath)
    {
        var physicalPath = PathHelper.MapImageWebPathToPhysical(webPath);
        if (physicalPath != null)
        {
            _filesToCleanup.Add(physicalPath);
        }
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

    private static byte[] CreateTestImageBytes(int width = 100, int height = 100)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.SteelBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static byte[] CreateOversizedBitmapBytes()
    {
        const int width = 8000;
        const int height = 8000;
        const int rowBytes = (width + 31) / 32 * 4;
        const int pixelOffset = 14 + 40 + 8;
        var bytes = new byte[pixelOffset + rowBytes * height];
        using var writer = new BinaryWriter(new MemoryStream(bytes));
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(bytes.Length);
        writer.Write(0);
        writer.Write(pixelOffset);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(0);
        writer.Write(rowBytes * height);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(2);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0x00FFFFFF);
        return bytes;
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static void AssertResizedWebp(byte[]? written)
    {
        written.Should().NotBeNull();
        using var codec = SKCodec.Create(SKData.CreateCopy(written!));
        codec.Should().NotBeNull();
        codec.EncodedFormat.Should().Be(SKEncodedImageFormat.Webp);
        codec.Info.Width.Should().Be(512);
        codec.Info.Height.Should().Be(512);
    }
}
