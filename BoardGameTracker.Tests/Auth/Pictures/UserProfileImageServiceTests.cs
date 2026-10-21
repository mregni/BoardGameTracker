using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Auth.Pictures;
using BoardGameTracker.Core.Images.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class UserProfileImageServiceTests
{
    private const string Authority = "https://idp.example.com";
    private const string PictureUrl = "https://idp.example.com/avatars/jane.png";

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IImageService> _imageServiceMock = new();
    private readonly Mock<IIdentityProviderPictureDownloader> _downloaderMock = new();
    private readonly Mock<ILogger<UserProfileImageService>> _loggerMock = new();
    private readonly UserProfileImageService _service;
    private readonly ApplicationUser _user = new("jane", "jane@example.com", "Jane");

    public UserProfileImageServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _userManagerMock.Setup(x => x.FindByIdAsync(_user.Id)).ReturnsAsync(_user);
        _userManagerMock.Setup(x => x.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(_user)).ReturnsAsync(["User"]);
        _downloaderMock.Setup(x => x.DownloadAsync(It.IsAny<string>(), Authority, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        _imageServiceMock.Setup(x => x.SaveProfileImage(It.IsAny<byte[]>())).ReturnsAsync("/images/profile/idp.webp");
        _service = new UserProfileImageService(_userManagerMock.Object, _imageServiceMock.Object, _downloaderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Import_ShouldStoreThePicture_ForAUserWithoutAnImage()
    {
        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().Be("/images/profile/idp.webp");
        _user.ProfileImageSource.Should().Be(ProfileImageSource.IdentityProvider);
        _user.ProfileImageSourceUrl.Should().Be(PictureUrl);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
    }

    [Fact]
    public async Task Import_ShouldNeverOverwriteAnUploadedImage()
    {
        _user.UseUploadedProfileImage("/images/profile/mine.webp");

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().Be("/images/profile/mine.webp");
        _user.ProfileImageSource.Should().Be(ProfileImageSource.Upload);
        _downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Import_ShouldNotDownloadAgain_WhenThePictureAddressIsUnchanged()
    {
        _user.UseIdentityProviderPicture("/images/profile/idp-old.webp", PictureUrl);

        await ImportAsync(PictureUrl);

        _downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _user.ProfileImage.Should().Be("/images/profile/idp-old.webp");
    }

    [Fact]
    public async Task Import_ShouldReplaceAnEarlierImportedPicture_WhenTheAddressChanges()
    {
        _user.UseIdentityProviderPicture("/images/profile/idp-old.webp", "https://idp.example.com/avatars/old.png");

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().Be("/images/profile/idp.webp");
        _user.ProfileImageSourceUrl.Should().Be(PictureUrl);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/idp-old.webp"), Times.Once);
    }

    [Fact]
    public async Task Import_ShouldNotBringBackARemovedPicture_UntilTheProviderPictureChanges()
    {
        _user.UseIdentityProviderPicture("/images/profile/idp-old.webp", PictureUrl);
        _user.RemoveProfileImage();

        await ImportAsync(PictureUrl);
        _user.ProfileImage.Should().BeNull();

        await ImportAsync("https://idp.example.com/avatars/new.png");
        _user.ProfileImage.Should().Be("/images/profile/idp.webp");
        _user.ProfileImageSource.Should().Be(ProfileImageSource.IdentityProvider);
    }

    [Fact]
    public async Task Import_ShouldKeepTheCurrentImage_WhenTheDownloadFails()
    {
        _downloaderMock.Setup(x => x.DownloadAsync(PictureUrl, Authority, It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().BeNull();
        _user.ProfileImageSourceUrl.Should().BeNull("a failed download is tried again on the next sign-in");
        _imageServiceMock.Verify(x => x.SaveProfileImage(It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task Import_ShouldKeepTheCurrentImage_WhenTheBytesAreNotAnImage()
    {
        _imageServiceMock.Setup(x => x.SaveProfileImage(It.IsAny<byte[]>())).ReturnsAsync(string.Empty);

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().BeNull();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Import_ShouldDiscardTheNewFile_WhenTheAccountChangedMeanwhile()
    {
        _userManagerMock.Setup(x => x.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));

        await ImportAsync(PictureUrl);

        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/idp.webp"), Times.Once);
    }

    [Fact]
    public async Task Import_ShouldIgnoreAnUnknownUser()
    {
        await _service.ImportFromIdentityProviderAsync(new IdentityProviderPictureImport("missing", PictureUrl, Authority), TestContext.Current.CancellationToken);

        _downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ShouldMarkTheImageAsUploaded_AndDeleteThePreviousFile()
    {
        _user.UseIdentityProviderPicture("/images/profile/idp-old.webp", PictureUrl);
        var file = Mock.Of<IFormFile>();
        _imageServiceMock.Setup(x => x.SaveImage(file, UploadFileType.Profile)).ReturnsAsync("/images/profile/mine.webp");

        var profile = await _service.UploadAsync(_user.Id, file);

        profile.ProfileImage.Should().Be("/images/profile/mine.webp");
        _user.ProfileImageSource.Should().Be(ProfileImageSource.Upload);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/idp-old.webp"), Times.Once);
    }

    [Fact]
    public async Task Upload_ShouldRefuse_AnEmptyFile()
    {
        _imageServiceMock.Setup(x => x.SaveImage(null, UploadFileType.Profile)).ReturnsAsync(string.Empty);

        var action = () => _service.UploadAsync(_user.Id, null);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ImageUnsupportedFormat);
    }

    [Fact]
    public async Task Upload_ShouldDiscardTheNewFile_WhenSavingTheAccountFails()
    {
        var file = Mock.Of<IFormFile>();
        _imageServiceMock.Setup(x => x.SaveImage(file, UploadFileType.Profile)).ReturnsAsync("/images/profile/mine.webp");
        _userManagerMock.Setup(x => x.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));

        var action = () => _service.UploadAsync(_user.Id, file);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.ProfileImageUpdateFailed);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/mine.webp"), Times.Once);
    }

    [Fact]
    public async Task Upload_ShouldThrow_ForAnUnknownUser()
    {
        var action = () => _service.UploadAsync("missing", Mock.Of<IFormFile>());

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Remove_ShouldClearTheImage_AndDeleteTheFile()
    {
        _user.UseUploadedProfileImage("/images/profile/mine.webp");

        var profile = await _service.RemoveAsync(_user.Id);

        profile.ProfileImage.Should().BeNull();
        _user.ProfileImageSource.Should().Be(ProfileImageSource.None);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/mine.webp"), Times.Once);
    }

    [Fact]
    public async Task ImportService_ShouldSwallowFailures_SoTheQueueKeepsRunning()
    {
        var services = new Mock<IServiceProvider>();
        var images = new Mock<IUserProfileImageService>();
        images.Setup(x => x.ImportFromIdentityProviderAsync(It.IsAny<IdentityProviderPictureImport>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        services.Setup(x => x.GetService(typeof(IUserProfileImageService))).Returns(images.Object);
        var scope = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(services.Object);
        var scopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        var logger = new Mock<ILogger<IdentityProviderPictureImportService>>();
        var worker = new IdentityProviderPictureImportService(scopeFactory.Object, new IdentityProviderPictureQueue(), logger.Object);

        await worker.ImportAsync(new IdentityProviderPictureImport(_user.Id, PictureUrl, Authority), TestContext.Current.CancellationToken);

        logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ImportService_ShouldImportQueuedPictures()
    {
        var services = new Mock<IServiceProvider>();
        var images = new Mock<IUserProfileImageService>();
        services.Setup(x => x.GetService(typeof(IUserProfileImageService))).Returns(images.Object);
        var scope = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(services.Object);
        var scopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        var queue = new IdentityProviderPictureQueue();
        var import = new IdentityProviderPictureImport(_user.Id, PictureUrl, Authority);
        var imported = new TaskCompletionSource();
        images.Setup(x => x.ImportFromIdentityProviderAsync(import, It.IsAny<CancellationToken>())).Callback(() => imported.TrySetResult()).Returns(Task.CompletedTask);
        using var worker = new IdentityProviderPictureImportService(scopeFactory.Object, queue, Mock.Of<ILogger<IdentityProviderPictureImportService>>());

        await worker.StartAsync(TestContext.Current.CancellationToken);
        queue.Enqueue(import).Should().BeTrue();
        await imported.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        images.Verify(x => x.ImportFromIdentityProviderAsync(import, It.IsAny<CancellationToken>()), Times.Once);
    }

    private Task ImportAsync(string pictureUrl) =>
        _service.ImportFromIdentityProviderAsync(new IdentityProviderPictureImport(_user.Id, pictureUrl, Authority), TestContext.Current.CancellationToken);
}
