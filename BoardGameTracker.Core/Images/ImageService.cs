using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


namespace BoardGameTracker.Core.Images;

public class ImageService : IImageService
{
    private const int ImageSize = 512;

    private readonly IDiskProvider _diskProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageService> _logger;

    public ImageService(IDiskProvider diskProvider, IHttpClientFactory httpClientFactory, ILogger<ImageService> logger)
    {
        _diskProvider = diskProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> DownloadImage(string imageUrl, string imageFileName)
    {
        _logger.LogDebug("Downloading image from {ImageUrl} for {FileName}", imageUrl, imageFileName);
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                var fileName = CreateFileNameFromUrl(imageUrl, imageFileName);
                var imageContent = await response.Content.ReadAsByteArrayAsync();

                using var image = Image.Load(imageContent);
                image.Mutate(x => x.Resize(ImageSize, ImageSize));
                var newFileName = await _diskProvider.WriteFile(image, fileName, PathHelper.FullCoverImagePath);
                var path = Path.Combine(PathHelper.CoverImagePath, newFileName);
                return $"/{path.Replace("\\", "/")}";
            }

            _logger.LogWarning("Image download returned {StatusCode} for {ImageUrl}, using placeholder", response.StatusCode, imageUrl);
            return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image from {ImageUrl}, using placeholder", imageUrl);
            return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
        }
    }

    public async Task<string> SaveImage(IFormFile? file, UploadFileType type)
    {
        _logger.LogDebug("Saving uploaded image of type {UploadType}", type);
        string folder;
        string fullPath;
        switch (type)
        {
            case UploadFileType.Game:
                folder = PathHelper.CoverImagePath;
                fullPath = PathHelper.FullCoverImagePath;
                break;
            case UploadFileType.Profile:
                folder = PathHelper.ProfileImagePath;
                fullPath = PathHelper.FullProfileImagePath;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        if (file == null || file.Length == 0)
        {
            return CreateNoImageImages(folder, fullPath, folder);
        }

        using var image = await Image.LoadAsync(file.OpenReadStream());
        image.Mutate(x => x.Resize(ImageSize, ImageSize));
        var newFileName = await _diskProvider.WriteFile(image, file.FileName, fullPath);
        var path = Path.Combine(folder, newFileName);
        return $"/{path.Replace("\\", "/")}";
    }

    public void DeleteImage(string? image)
    {
        if (image == null)
        {
            return;
        }
        
        _diskProvider.DeleteFile(image);
    }

    private static string CreateFileNameFromUrl(string imageUrl, string fileName)
    {
        var extension = Path.GetExtension(imageUrl);
        return $"{fileName}{extension}";
    }

    private static string CreateNoImageImages(string fileName, string absolutePath, string relativePath)
    {
        const string noImageFile = "no-image.jpg";
        fileName += Path.GetExtension(noImageFile);
        
        var sourcePath = Path.Combine(PathHelper.FullRootImagePath, noImageFile);
        var destinationPath = Path.Combine(absolutePath, fileName.GenerateUniqueFileName());
        File.Copy(sourcePath, destinationPath);
        var path = Path.Combine(relativePath, Path.GetFileName(destinationPath));
        return $"/{path.Replace("\\", "/")}";
    }
}