using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth.Pictures;

public class UserProfileImageService : IUserProfileImageService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IImageService _imageService;
    private readonly IIdentityProviderPictureDownloader _downloader;
    private readonly IPlayerAccountImageService _playerAccountImageService;
    private readonly ILogger<UserProfileImageService> _logger;

    public UserProfileImageService(
        UserManager<ApplicationUser> userManager,
        IImageService imageService,
        IIdentityProviderPictureDownloader downloader,
        IPlayerAccountImageService playerAccountImageService,
        ILogger<UserProfileImageService> logger)
    {
        _userManager = userManager;
        _imageService = imageService;
        _downloader = downloader;
        _playerAccountImageService = playerAccountImageService;
        _logger = logger;
    }

    public async Task<ProfileResponse> UploadAsync(string userId, IFormFile? file)
    {
        var user = await FindAsync(userId);
        var image = await _imageService.SaveImage(file, UploadFileType.Profile);
        if (string.IsNullOrEmpty(image))
        {
            throw new ValidationException(Constants.Errors.ImageUnsupportedFormat);
        }

        var previous = user.ProfileImage;
        user.UseUploadedProfileImage(image);
        await SaveOrDiscardAsync(user, image);
        _imageService.DeleteImage(previous);
        await _playerAccountImageService.ApplyAccountImageAsync(user);

        _logger.LogInformation("User {UserId} uploaded a profile picture", userId);
        return user.ToProfileDto(await _userManager.GetRolesAsync(user));
    }

    public async Task<ProfileResponse> RemoveAsync(string userId)
    {
        var user = await FindAsync(userId);
        var previous = user.ProfileImage;
        user.RemoveProfileImage();
        await SaveOrDiscardAsync(user, null);
        _imageService.DeleteImage(previous);
        await _playerAccountImageService.ApplyAccountImageAsync(user);

        _logger.LogInformation("User {UserId} removed their profile picture", userId);
        return user.ToProfileDto(await _userManager.GetRolesAsync(user));
    }

    public async Task ImportFromIdentityProviderAsync(IdentityProviderPictureImport import, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(import.UserId);
        if (user == null || !user.AcceptsIdentityProviderPicture(import.PictureUrl))
        {
            return;
        }

        var content = await _downloader.DownloadAsync(import.PictureUrl, import.Authority, cancellationToken);
        if (content == null)
        {
            return;
        }

        var image = await _imageService.SaveProfileImage(content);
        if (string.IsNullOrEmpty(image))
        {
            _logger.LogWarning("The identity provider picture of user {UserId} could not be read as an image", user.Id);
            return;
        }

        var previous = user.ProfileImage;
        user.UseIdentityProviderPicture(image, import.PictureUrl);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _imageService.DeleteImage(image);
            _logger.LogWarning("Kept the current profile picture of user {UserId} because the account changed during the import", user.Id);
            return;
        }

        _imageService.DeleteImage(previous);
        await _playerAccountImageService.ApplyAccountImageAsync(user);
        _logger.LogInformation("Imported the identity provider picture of user {UserId}", user.Id);
    }

    private async Task<ApplicationUser> FindAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);
    }

    private async Task SaveOrDiscardAsync(ApplicationUser user, string? newImage)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _imageService.DeleteImage(newImage);
            throw new DomainException(Constants.Errors.ProfileImageUpdateFailed);
        }
    }
}
