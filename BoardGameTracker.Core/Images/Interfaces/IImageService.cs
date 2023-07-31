using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Core.Images.Interfaces;

public interface IImageService
{
    Task<string> DownloadImage(string imageUrl, string bggId);
    Task<string> SaveProfileImage(IFormFile? file);
    void DeleteImage(string image);
}