using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Core.Images.Interfaces;

public interface IImageService
{
    Task<string> DownloadImage(string imageUrl, string imageFileName);
    Task<string> SaveImage(IFormFile? file, UploadFileType type);
    void DeleteImage(string? image);
}