using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Auth.Pictures;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Core.Auth.Interfaces;

public interface IUserProfileImageService
{
    Task<ProfileResponse> UploadAsync(string userId, IFormFile? file);
    Task<ProfileResponse> RemoveAsync(string userId);
    Task ImportFromIdentityProviderAsync(IdentityProviderPictureImport import, CancellationToken cancellationToken);
}
