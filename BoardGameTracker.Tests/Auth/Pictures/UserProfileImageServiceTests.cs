using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Auth.Pictures;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Tests.Support;
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
    private const string ImportedImage = "/images/profile/idp.webp";
    private const string UploadedImage = "/images/profile/mine.webp";
    private const string OldImage = "/images/profile/idp-old.webp";

    private static readonly byte[] Content = [1, 2, 3];

    private readonly Mock<IUserStore<ApplicationUser>> _userStoreMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IImageService> _imageServiceMock = new();
    private readonly Mock<IIdentityProviderPictureDownloader> _downloaderMock = new();
    private readonly Mock<IPlayerAccountImageService> _playerAccountImageServiceMock = new();
    private readonly Mock<ILogger<UserProfileImageService>> _loggerMock = new();
    private readonly UserProfileImageService _service;
    private readonly ApplicationUser _user = new("jane", "jane@example.com", "Jane");
    private readonly IFormFile _file = new FormFile(Stream.Null, 0, 0, "file", "mine.png");
    private readonly CancellationToken _token = new CancellationTokenSource().Token;

    public UserProfileImageServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            _userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _userManagerMock.Setup(x => x.FindByIdAsync(_user.Id)).ReturnsAsync(_user);
        _userManagerMock.Setup(x => x.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(_user)).ReturnsAsync(["User"]);
        _downloaderMock.Setup(x => x.DownloadAsync(PictureUrl, Authority, _token)).ReturnsAsync(Content);
        _imageServiceMock.Setup(x => x.SaveProfileImage(Content)).ReturnsAsync(ImportedImage);
        _imageServiceMock.Setup(x => x.SaveImage(_file, UploadFileType.Profile)).ReturnsAsync(UploadedImage);
        _service = new UserProfileImageService(_userManagerMock.Object, _imageServiceMock.Object, _downloaderMock.Object, _playerAccountImageServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Import_ShouldStoreThePicture_ForAUserWithoutAnImage()
    {
        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().Be(ImportedImage);
        _user.ProfileImageSource.Should().Be(ProfileImageSource.IdentityProvider);
        _user.ProfileImageSourceUrl.Should().Be(PictureUrl);
        VerifyImported(PictureUrl);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_ShouldNeverOverwriteAnUploadedImage()
    {
        _user.UseUploadedProfileImage(UploadedImage);

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().Be(UploadedImage);
        _user.ProfileImageSource.Should().Be(ProfileImageSource.Upload);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_ShouldNotDownloadAgain_WhenThePictureAddressIsUnchanged()
    {
        _user.UseIdentityProviderPicture(OldImage, PictureUrl);

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().Be(OldImage);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_ShouldReplaceAnEarlierImportedPicture_WhenTheAddressChanges()
    {
        _user.UseIdentityProviderPicture(OldImage, "https://idp.example.com/avatars/old.png");

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().Be(ImportedImage);
        _user.ProfileImageSourceUrl.Should().Be(PictureUrl);
        VerifyImported(PictureUrl);
        _imageServiceMock.Verify(x => x.DeleteImage(OldImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_ShouldNotBringBackARemovedPicture_UntilTheProviderPictureChanges()
    {
        const string newPicture = "https://idp.example.com/avatars/new.png";
        _user.UseIdentityProviderPicture(OldImage, PictureUrl);
        _user.RemoveProfileImage();
        _downloaderMock.Setup(x => x.DownloadAsync(newPicture, Authority, _token)).ReturnsAsync(Content);

        await ImportAsync(PictureUrl);
        _user.ProfileImage.Should().BeNull();

        await ImportAsync(newPicture);
        _user.ProfileImage.Should().Be(ImportedImage);
        _user.ProfileImageSource.Should().Be(ProfileImageSource.IdentityProvider);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Exactly(2));
        _downloaderMock.Verify(x => x.DownloadAsync(newPicture, Authority, _token), Times.Once);
        _imageServiceMock.Verify(x => x.SaveProfileImage(Content), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(_user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"Imported the identity provider picture of user {_user.Id}");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_ShouldKeepTheCurrentImage_WhenTheDownloadFails()
    {
        _downloaderMock.Setup(x => x.DownloadAsync(PictureUrl, Authority, _token)).ReturnsAsync((byte[]?)null);

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().BeNull();
        _user.ProfileImageSourceUrl.Should().BeNull("a failed download is tried again on the next sign-in");
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _downloaderMock.Verify(x => x.DownloadAsync(PictureUrl, Authority, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Import_ShouldKeepTheCurrentImage_WhenTheBytesAreNotAnImage(string? saved)
    {
        _imageServiceMock.Setup(x => x.SaveProfileImage(Content)).ReturnsAsync(saved!);

        await ImportAsync(PictureUrl);

        _user.ProfileImage.Should().BeNull();
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _downloaderMock.Verify(x => x.DownloadAsync(PictureUrl, Authority, _token), Times.Once);
        _imageServiceMock.Verify(x => x.SaveProfileImage(Content), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"The identity provider picture of user {_user.Id} could not be read as an image");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_ShouldDiscardTheNewFile_WhenTheAccountChangedMeanwhile()
    {
        _user.UseIdentityProviderPicture(OldImage, "https://idp.example.com/avatars/old.png");
        _userManagerMock.Setup(x => x.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));

        await ImportAsync(PictureUrl);

        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _downloaderMock.Verify(x => x.DownloadAsync(PictureUrl, Authority, _token), Times.Once);
        _imageServiceMock.Verify(x => x.SaveProfileImage(Content), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(ImportedImage), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Kept the current profile picture of user {_user.Id} because the account changed during the import");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_ShouldIgnoreAnUnknownUser()
    {
        await _service.ImportFromIdentityProviderAsync(new IdentityProviderPictureImport("missing", PictureUrl, Authority), _token);

        _userManagerMock.Verify(x => x.FindByIdAsync("missing"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_ShouldMarkTheImageAsUploaded_AndDeleteThePreviousFile()
    {
        _user.UseIdentityProviderPicture(OldImage, PictureUrl);

        var profile = await _service.UploadAsync(_user.Id, _file);

        profile.ProfileImage.Should().Be(UploadedImage);
        profile.Roles.Should().Equal("User");
        _user.ProfileImageSource.Should().Be(ProfileImageSource.Upload);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _imageServiceMock.Verify(x => x.SaveImage(_file, UploadFileType.Profile), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(OldImage), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(_user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(_user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"User {_user.Id} uploaded a profile picture");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Upload_ShouldRefuse_AnEmptyFile(string? saved)
    {
        _imageServiceMock.Setup(x => x.SaveImage(null, UploadFileType.Profile)).ReturnsAsync(saved!);

        var action = () => _service.UploadAsync(_user.Id, null);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ImageUnsupportedFormat);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _imageServiceMock.Verify(x => x.SaveImage(null, UploadFileType.Profile), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_ShouldDiscardTheNewFile_WhenSavingTheAccountFails()
    {
        _user.UseUploadedProfileImage("/images/profile/previous.webp");
        _userManagerMock.Setup(x => x.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));

        var action = () => _service.UploadAsync(_user.Id, _file);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.ProfileImageUpdateFailed);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _imageServiceMock.Verify(x => x.SaveImage(_file, UploadFileType.Profile), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(UploadedImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upload_ShouldThrow_ForAnUnknownUser()
    {
        var action = () => _service.UploadAsync("missing", _file);

        await action.Should().ThrowAsync<EntityNotFoundException>();
        _userManagerMock.Verify(x => x.FindByIdAsync("missing"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Remove_ShouldClearTheImage_AndDeleteTheFile()
    {
        _user.UseUploadedProfileImage(UploadedImage);

        var profile = await _service.RemoveAsync(_user.Id);

        profile.ProfileImage.Should().BeNull();
        profile.Roles.Should().Equal("User");
        _user.ProfileImageSource.Should().Be(ProfileImageSource.None);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(UploadedImage), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(_user), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(_user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"User {_user.Id} removed their profile picture");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Remove_ShouldKeepTheFile_WhenSavingTheAccountFails()
    {
        _user.UseUploadedProfileImage(UploadedImage);
        _userManagerMock.Setup(x => x.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));

        var action = () => _service.RemoveAsync(_user.Id);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.ProfileImageUpdateFailed);
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Remove_ShouldThrow_ForAnUnknownUser()
    {
        var action = () => _service.RemoveAsync("missing");

        await action.Should().ThrowAsync<EntityNotFoundException>();
        _userManagerMock.Verify(x => x.FindByIdAsync("missing"), Times.Once);
        VerifyNoOtherCalls();
    }

    private Task ImportAsync(string pictureUrl) =>
        _service.ImportFromIdentityProviderAsync(new IdentityProviderPictureImport(_user.Id, pictureUrl, Authority), _token);

    private void VerifyImported(string pictureUrl)
    {
        _userManagerMock.Verify(x => x.FindByIdAsync(_user.Id), Times.Once);
        _downloaderMock.Verify(x => x.DownloadAsync(pictureUrl, Authority, _token), Times.Once);
        _imageServiceMock.Verify(x => x.SaveProfileImage(Content), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(_user), Times.Once);
        _playerAccountImageServiceMock.Verify(x => x.ApplyAccountImageAsync(_user), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, $"Imported the identity provider picture of user {_user.Id}");
    }

    private void VerifyNoOtherCalls()
    {
        _userStoreMock.VerifyNoOtherCalls();
        _userManagerMock.VerifySet(x => x.Logger = null!, Times.Once);
        _userManagerMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _downloaderMock.VerifyNoOtherCalls();
        _playerAccountImageServiceMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }
}
