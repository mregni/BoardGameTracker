using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Core.Images.Interfaces;

public interface IImageService
{
    Task<string> DownloadImage(string imageUrl, string imageFileName);
    Task<string> SaveProfileImage(IFormFile? file);
    Task<string> DownloadBackgroundImage(int bggId);
    void DeleteImage(string image);
}