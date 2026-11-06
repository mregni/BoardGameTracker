using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using Xunit;

namespace BoardGameTracker.Tests.Images;

public class ImageServiceTests : IDisposable
{
    private const int MaxBytes = 15 * 1024 * 1024;
    private const string ByteLimitMessage = "exceeds the 15728640 byte limit, storing no image";
    private const string PixelLimitMessage = "exceeds the 50000000 pixel limit, storing no image";

    private readonly Mock<IDiskProvider> _diskProviderMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ImageService>> _loggerMock;
    private readonly RecordingHandler _handler = new();
    private readonly MainDbContext _context;
    private readonly ImageService _imageService;

    public ImageServiceTests()
    {
        _diskProviderMock = new Mock<IDiskProvider>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ImageService>>();
        _context = new MainDbContext(new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _imageService = new ImageService(_diskProviderMock.Object, _httpClientFactoryMock.Object, _context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _diskProviderMock.VerifyNoOtherCalls();
        _httpClientFactoryMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private void VerifyDownloadRequest(string url)
    {
        _handler.Requests.Should().Equal(url);
        _httpClientFactoryMock.Verify(x => x.CreateClient(ImageService.HttpClientName), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, $"Downloading image from {url} for cover-file");
    }

    [Theory]
    [InlineData(UploadFileType.Game)]
    [InlineData(UploadFileType.Profile)]
    public async Task SaveImage_ShouldResizeEncodeAsWebpAndReturnPath(UploadFileType type)
    {
        var formFile = CreateFormFile("upload.png", CreateTestImageBytes());
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
        _loggerMock.VerifyLog(LogLevel.Debug, $"Saving uploaded image of type {type}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldAcceptAFileOfExactlyTheMaximumSize()
    {
        var formFile = CreateFormFile("big.jpg", PadTo(CreateTestImageBytes(), MaxBytes));
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "big.webp", PathHelper.FullCoverImagePath))
            .ReturnsAsync("unique-big.webp");

        var result = await _imageService.SaveImage(formFile, UploadFileType.Game);

        result.Should().Be("/images/cover/unique-big.webp");
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "big.webp", PathHelper.FullCoverImagePath), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type Game");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowException_WhenTypeIsUnsupported()
    {
        var formFile = CreateFormFile("test.jpg", CreateTestImageBytes());

        var action = async () => await _imageService.SaveImage(formFile, (UploadFileType)999);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("type");
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type 999");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveImage_ShouldReturnNoImage_WhenFileIsNullOrEmpty(bool fileIsNull)
    {
        var formFile = fileIsNull ? null : CreateFormFile("empty.png", []);

        var result = await _imageService.SaveImage(formFile, UploadFileType.Game);

        result.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type Game");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowValidationException_WhenFileExceedsMaxUploadSize()
    {
        var formFile = CreateFormFile("big.jpg", CreateTestImageBytes(), MaxBytes + 1);

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.ImageTooLarge);
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type Game");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowValidationException_WhenImageExceedsPixelLimit()
    {
        var formFile = CreateFormFile("huge.bmp", CreateBitmapBytes(10000, 5001));

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.ImageTooLarge);
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type Game");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldAcceptAnImageOfExactlyThePixelLimit()
    {
        var formFile = CreateFormFile("limit.bmp", CreateBitmapBytes(10000, 5000));
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "limit.webp", PathHelper.FullCoverImagePath))
            .ReturnsAsync("unique-limit.webp");

        var result = await _imageService.SaveImage(formFile, UploadFileType.Game);

        result.Should().Be("/images/cover/unique-limit.webp");
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "limit.webp", PathHelper.FullCoverImagePath), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type Game");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowValidationException_WhenFileIsNotAnImage()
    {
        var formFile = CreateFormFile("not-image.jpg", [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage(Constants.Errors.ImageUnsupportedFormat);
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type Game");
        _loggerMock.VerifyLog(LogLevel.Warning, "Rejected upload of type Game: unsupported image format");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveImage_ShouldThrowException_WhenDiskProviderThrows()
    {
        var formFile = CreateFormFile("test.jpg", CreateTestImageBytes());
        var expectedException = new IOException("Disk error");

        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "test.webp", PathHelper.FullCoverImagePath))
            .ThrowsAsync(expectedException);

        var action = async () => await _imageService.SaveImage(formFile, UploadFileType.Game);

        (await action.Should().ThrowAsync<IOException>()).Which.Should().Be(expectedException);
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "test.webp", PathHelper.FullCoverImagePath), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Debug, "Saving uploaded image of type Game");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldResizeAndWriteWebpFile_WhenDownloadSucceeds()
    {
        const string url = "https://example.com/image.jpg";
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(CreateTestImageBytes())
        });

        byte[]? written = null;
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath))
            .Callback<Stream, string, string>((stream, _, _) => written = ReadAll(stream))
            .ReturnsAsync("unique-cover.webp");

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().Be($"/{PathHelper.CoverImagePath}/unique-cover.webp".Replace("\\", "/"));
        AssertResizedWebp(written);
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath), Times.Once);
        VerifyDownloadRequest(url);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("exact-size")]
    [InlineData("exact-pixels")]
    public async Task DownloadImage_ShouldAcceptAnImageExactlyAtTheLimits(string limit)
    {
        const string url = "https://example.com/limit.jpg";
        var bytes = limit == "exact-size" ? PadTo(CreateTestImageBytes(), MaxBytes) : CreateBitmapBytes(10000, 5000);
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath))
            .ReturnsAsync("unique-cover.webp");

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().Be("/images/cover/unique-cover.webp");
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath), Times.Once);
        VerifyDownloadRequest(url);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DownloadImage_ShouldReturnNoImageWithoutARequest_WhenUrlIsEmpty(string imageUrl)
    {
        var result = await _imageService.DownloadImage(imageUrl, "cover-file");

        result.Should().BeEmpty();
        _handler.Requests.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Debug, $"Downloading image from {imageUrl} for cover-file");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenContentIsNotAnImage()
    {
        const string url = "https://example.com/page.html";
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08])
        });

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().BeEmpty();
        VerifyDownloadRequest(url);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Image at {url} is not a supported image format, storing no image");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenImageExceedsPixelLimit()
    {
        const string url = "https://example.com/huge.bmp";
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(CreateBitmapBytes(10000, 5001))
        });

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().BeEmpty();
        VerifyDownloadRequest(url);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Image at {url} {PixelLimitMessage}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenResponseIsNotSuccessful()
    {
        const string url = "https://example.com/missing.jpg";
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().BeEmpty();
        VerifyDownloadRequest(url);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Image download returned NotFound for {url}, storing no image");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenContentLengthExceedsLimit()
    {
        const string url = "https://example.com/huge.jpg";
        SetupHttpResponse(() =>
        {
            var content = new ByteArrayContent(CreateTestImageBytes());
            content.Headers.ContentLength = MaxBytes + 1L;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().BeEmpty();
        VerifyDownloadRequest(url);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Image at {url} {ByteLimitMessage}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenStreamedContentExceedsLimit()
    {
        const string url = "https://example.com/huge-stream.jpg";
        SetupHttpResponse(() =>
        {
            var content = new StreamContent(new MemoryStream(PadTo(CreateTestImageBytes(), MaxBytes + 1)));
            content.Headers.ContentLength = null;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().BeEmpty();
        VerifyDownloadRequest(url);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Image at {url} {ByteLimitMessage}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenRequestThrows()
    {
        const string url = "https://example.com/error.jpg";
        var failure = new HttpRequestException("boom");
        SetupHttpResponse(() => throw failure);

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().BeEmpty();
        VerifyDownloadRequest(url);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Failed to download image from {url}, storing no image", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadImage_ShouldReturnNoImage_WhenTheFileCannotBeWritten()
    {
        const string url = "https://example.com/image.jpg";
        var failure = new IOException("Disk full");
        SetupHttpResponse(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(CreateTestImageBytes()) });
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath))
            .ThrowsAsync(failure);

        var result = await _imageService.DownloadImage(url, "cover-file");

        result.Should().BeEmpty();
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "cover-file.webp", PathHelper.FullCoverImagePath), Times.Once);
        VerifyDownloadRequest(url);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Failed to download image from {url}, storing no image", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DeleteImage_ShouldMapWebPathToPhysicalPath_WhenUnderImagesRoot()
    {
        const string imagePath = "/images/cover/test.jpg";
        var expectedPhysical = PathHelper.MapImageWebPathToPhysical(imagePath);

        _imageService.DeleteImage(imagePath);

        expectedPhysical.Should().EndWith(Path.Combine("images", "cover", "test.jpg"));
        _diskProviderMock.Verify(x => x.DeleteFile(expectedPhysical!), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CopyProfileImage_ShouldWriteASeparateFileInTheProfileFolder()
    {
        const string imagePath = "/images/profile/avatar_abc.webp";
        var physical = PathHelper.MapImageWebPathToPhysical(imagePath)!;
        var source = new MemoryStream([1, 2, 3]);
        _diskProviderMock.Setup(x => x.FileExists(physical)).Returns(true);
        _diskProviderMock.Setup(x => x.OpenRead(physical)).Returns(source);
        _diskProviderMock
            .Setup(x => x.WriteFile(source, "player.webp", PathHelper.FullProfileImagePath))
            .ReturnsAsync("player_xyz.webp");

        var result = await _imageService.CopyProfileImage(imagePath);

        result.Should().Be("/images/profile/player_xyz.webp");
        _diskProviderMock.Verify(x => x.FileExists(physical), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(physical), Times.Once);
        _diskProviderMock.Verify(x => x.WriteFile(source, "player.webp", PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CopyProfileImage_ShouldReturnNull_WhenTheSourceFileIsMissing()
    {
        const string imagePath = "/images/profile/gone.webp";
        var physical = PathHelper.MapImageWebPathToPhysical(imagePath)!;
        _diskProviderMock.Setup(x => x.FileExists(physical)).Returns(false);

        var result = await _imageService.CopyProfileImage(imagePath);

        result.Should().BeNull();
        _diskProviderMock.Verify(x => x.FileExists(physical), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Cannot copy a profile picture whose file is missing");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CopyProfileImage_ShouldRefusePathsOutsideTheImagesFolder()
    {
        var result = await _imageService.CopyProfileImage("/images/../../secret.txt");

        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Warning, "Cannot copy a profile picture whose file is missing");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteImageIfUnusedAsync_ShouldDeleteTheFile_WhenNothingUsesIt()
    {
        const string imagePath = "/images/cover/free.webp";
        var game = new Game("Other");
        game.UpdateImage("/images/cover/other.webp");
        var user = new ApplicationUser("other", "other@test.com");
        user.UseUploadedProfileImage("/images/profile/other-user.webp");
        _context.Players.Add(new Player("Other", "/images/profile/other.webp"));
        _context.Games.Add(game);
        _context.Users.Add(user);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _imageService.DeleteImageIfUnusedAsync(imagePath);

        (await _context.Players.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        _diskProviderMock.Verify(x => x.DeleteFile(PathHelper.MapImageWebPathToPhysical(imagePath)!), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("player")]
    [InlineData("game")]
    [InlineData("user")]
    public async Task DeleteImageIfUnusedAsync_ShouldKeepTheFile_WhenAnotherRecordUsesIt(string owner)
    {
        const string imagePath = "/images/profile/shared.webp";
        switch (owner)
        {
            case "player":
                _context.Players.Add(new Player("Other", imagePath));
                break;
            case "game":
                var game = new Game("Other");
                game.UpdateImage(imagePath);
                _context.Games.Add(game);
                break;
            default:
                var user = new ApplicationUser("other", "other@test.com");
                user.UseUploadedProfileImage(imagePath);
                _context.Users.Add(user);
                break;
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _imageService.DeleteImageIfUnusedAsync(imagePath);

        await act.Should().NotThrowAsync();
        _loggerMock.VerifyLog(LogLevel.Debug, $"Keeping image {imagePath} because another record still uses it");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteImageIfUnusedAsync_ShouldDoNothing_WhenThereIsNoImage(string? imagePath)
    {
        var act = () => _imageService.DeleteImageIfUnusedAsync(imagePath);

        await act.Should().NotThrowAsync();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeleteImage_ShouldNotCallDiskProvider_WhenThereIsNoImage(string? imagePath)
    {
        var act = () => _imageService.DeleteImage(imagePath);

        act.Should().NotThrow();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("/profiles/user.png")]
    [InlineData(@"C:\temp\image.gif")]
    [InlineData("/images/../../secret.txt")]
    public void DeleteImage_ShouldNotCallDiskProvider_WhenPathIsOutsideImagesRoot(string imagePath)
    {
        var act = () => _imageService.DeleteImage(imagePath);

        act.Should().NotThrow();
        _loggerMock.VerifyLog(LogLevel.Warning, $"Refusing to delete image outside the images directory: {imagePath}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ClearAllImages_ShouldClearCoverAndProfileFolders()
    {
        _imageService.ClearAllImages();

        PathHelper.FullCoverImagePath.Should().NotBe(PathHelper.FullProfileImagePath);
        _diskProviderMock.Verify(x => x.ClearFolder(PathHelper.FullCoverImagePath), Times.Once);
        _diskProviderMock.Verify(x => x.ClearFolder(PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveProfileImage_ShouldResizeEncodeAsWebpIntoTheProfileFolder()
    {
        byte[]? written = null;
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath))
            .Callback<Stream, string, string>((stream, _, _) => written = ReadAll(stream))
            .ReturnsAsync("unique-avatar.webp");

        var result = await _imageService.SaveProfileImage(CreateTestImageBytes());

        result.Should().Be($"/{PathHelper.ProfileImagePath}/unique-avatar.webp".Replace("\\", "/"));
        AssertResizedWebp(written);
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(300, 100)]
    [InlineData(3000, 1000)]
    public async Task SaveProfileImage_ShouldCropTheCenterSquare_InsteadOfStretching(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Green);
            using var red = new SKPaint { Color = SKColors.Red };
            using var blue = new SKPaint { Color = SKColors.Blue };
            canvas.DrawRect(0, 0, height, height, red);
            canvas.DrawRect(width - height, 0, height, height, blue);
        }

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);

        var output = await SaveProfileImageAndDecode(data.ToArray());

        IsMostly(output.GetPixel(20, 256), SKColors.Green).Should().BeTrue();
        IsMostly(output.GetPixel(256, 256), SKColors.Green).Should().BeTrue();
        IsMostly(output.GetPixel(490, 256), SKColors.Green).Should().BeTrue();
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveProfileImage_ShouldCropTheCenterSquare_OfAPortraitImage()
    {
        using var bitmap = new SKBitmap(100, 300);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Green);
            using var red = new SKPaint { Color = SKColors.Red };
            using var blue = new SKPaint { Color = SKColors.Blue };
            canvas.DrawRect(0, 0, 100, 100, red);
            canvas.DrawRect(0, 200, 100, 100, blue);
        }

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);

        var output = await SaveProfileImageAndDecode(data.ToArray());

        IsMostly(output.GetPixel(256, 20), SKColors.Green).Should().BeTrue();
        IsMostly(output.GetPixel(256, 490), SKColors.Green).Should().BeTrue();
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveProfileImage_ShouldApplyTheExifOrientation()
    {
        using var bitmap = new SKBitmap(200, 100);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Blue);
            using var red = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(0, 0, 100, 100, red);
        }

        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);

        var output = await SaveProfileImageAndDecode(WithExifOrientation(data.ToArray(), 6));

        IsMostly(output.GetPixel(256, 64), SKColors.Red).Should().BeTrue();
        IsMostly(output.GetPixel(256, 448), SKColors.Blue).Should().BeTrue();
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1, "RGBY")]
    [InlineData(2, "GRYB")]
    [InlineData(3, "YBGR")]
    [InlineData(4, "BYRG")]
    [InlineData(5, "RBGY")]
    [InlineData(6, "BRYG")]
    [InlineData(7, "YGBR")]
    [InlineData(8, "GYRB")]
    public async Task SaveProfileImage_ShouldTurnTheQuadrants_ForEveryExifOrientation(byte orientation, string expected)
    {
        using var bitmap = new SKBitmap(200, 200);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Red);
            using var green = new SKPaint { Color = SKColors.Lime };
            using var blue = new SKPaint { Color = SKColors.Blue };
            using var yellow = new SKPaint { Color = SKColors.Yellow };
            canvas.DrawRect(100, 0, 100, 100, green);
            canvas.DrawRect(0, 100, 100, 100, blue);
            canvas.DrawRect(100, 100, 100, 100, yellow);
        }

        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);

        var output = await SaveProfileImageAndDecode(WithExifOrientation(data.ToArray(), orientation));

        string.Concat(
                Quadrant(output.GetPixel(128, 128)),
                Quadrant(output.GetPixel(384, 128)),
                Quadrant(output.GetPixel(128, 384)),
                Quadrant(output.GetPixel(384, 384)))
            .Should().Be(expected);
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveProfileImage_ShouldStoreNothing_WhenTheBytesAreNotAnImage()
    {
        var result = await _imageService.SaveProfileImage("<html>not an image</html>"u8.ToArray());

        result.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, "Rejected a profile picture: unsupported image format");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveProfileImage_ShouldStoreNothing_WhenTheImageExceedsThePixelLimit()
    {
        var result = await _imageService.SaveProfileImage(CreateBitmapBytes(10000, 5001));

        result.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Warning, "Rejected a profile picture: more than 50000000 pixels");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveProfileImage_ShouldStoreAnImageOfExactlyThePixelLimit()
    {
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath))
            .ReturnsAsync("unique-avatar.webp");

        var result = await _imageService.SaveProfileImage(CreateBitmapBytes(10000, 5000));

        result.Should().Be("/images/profile/unique-avatar.webp");
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath), Times.Once);
        VerifyNoOtherCalls();
    }

    private void SetupHttpResponse(Func<HttpResponseMessage> responseFactory)
    {
        _handler.Respond = responseFactory;
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(ImageService.HttpClientName))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));
    }

    private static FormFile CreateFormFile(string fileName, byte[] content, long? length = null)
    {
        return new FormFile(new MemoryStream(content), 0, length ?? content.Length, "file", fileName);
    }

    private async Task<SKBitmap> SaveProfileImageAndDecode(byte[] content)
    {
        byte[]? written = null;
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "avatar.webp", PathHelper.FullProfileImagePath))
            .Callback<Stream, string, string>((stream, _, _) => written = ReadAll(stream))
            .ReturnsAsync("unique-avatar.webp");

        await _imageService.SaveProfileImage(content);

        AssertResizedWebp(written);
        return SKBitmap.Decode(written!);
    }

    private static byte[] WithExifOrientation(byte[] jpeg, byte orientation)
    {
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, orientation, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        ];
        return jpeg[..2].Concat(exif).Concat(jpeg[2..]).ToArray();
    }

    private static bool IsMostly(SKColor actual, SKColor expected)
    {
        return Math.Abs(actual.Red - expected.Red) < 60
            && Math.Abs(actual.Green - expected.Green) < 60
            && Math.Abs(actual.Blue - expected.Blue) < 60;
    }

    private static char Quadrant(SKColor actual)
    {
        if (IsMostly(actual, SKColors.Red))
        {
            return 'R';
        }

        if (IsMostly(actual, SKColors.Lime))
        {
            return 'G';
        }

        if (IsMostly(actual, SKColors.Blue))
        {
            return 'B';
        }

        return IsMostly(actual, SKColors.Yellow) ? 'Y' : '?';
    }

    private static byte[] CreateTestImageBytes(int width = 100, int height = 100)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.SteelBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static byte[] PadTo(byte[] image, int length)
    {
        var padded = new byte[length];
        image.CopyTo(padded, 0);
        return padded;
    }

    private static byte[] CreateBitmapBytes(int width, int height)
    {
        var rowBytes = (width + 31) / 32 * 4;
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

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        public Func<HttpResponseMessage> Respond { get; set; } = () => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.ToString());
            return Task.FromResult(Respond());
        }
    }
}
