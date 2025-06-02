using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


namespace BoardGameTracker.Core.Images;

public class ImageService : IImageService
{
    private readonly IDiskProvider _diskProvider;

    public ImageService(IDiskProvider diskProvider)
    {
        _diskProvider = diskProvider;
    }

    public async Task<string> DownloadImage(string imageUrl, string imageFileName)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                var fileName = CreateFileNameFromUrl(imageUrl, imageFileName);
                var imageContent = await response.Content.ReadAsByteArrayAsync();

                using var image = Image.Load(imageContent);
                image.Mutate(x => x.Resize(500, 500));
                var newFileName = await _diskProvider.WriteFile(image, fileName, PathHelper.FullCoverImagePath);
                var path = Path.Combine(PathHelper.CoverImagePath, newFileName);
                return $"/{path.Replace("\\", "/")}";
            }

            return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
        }
        catch (Exception)
        {
            return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
        }
    }

    public async Task<string> SaveImage(IFormFile? file, UploadFileType type)
    {
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
        image.Mutate(x => x.Resize(512, 512));
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