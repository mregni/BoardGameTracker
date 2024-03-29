﻿using BoardGameTracker.Common.Extensions;
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

    public async Task<string> DownloadImage(string imageUrl, string bggId)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                var fileName = CreateFileNameFromUrl(imageUrl, bggId);
                var imageContent = await response.Content.ReadAsByteArrayAsync();

                using var image = Image.Load(imageContent);
                image.Mutate(x => x.Resize(500, 500));
                var newFileName = await _diskProvider.WriteFile(image, fileName, PathHelper.FullCoverImagePath);
                return Path.Combine(PathHelper.CoverImagePath, newFileName);
            }

            return CreateNoImageImages(bggId, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
        }
        catch (Exception e)
        {
            return CreateNoImageImages(bggId, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
        }
    }

    public async Task<string> SaveProfileImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return CreateNoImageImages("profile", PathHelper.FullProfileImagePath, PathHelper.ProfileImagePath);
        }
        
        using var image = await Image.LoadAsync(file.OpenReadStream());
        image.Mutate(x => x.Resize(512, 512));
        var newFileName = await _diskProvider.WriteFile(image, file.FileName, PathHelper.FullProfileImagePath);
        return Path.Combine(PathHelper.ProfileImagePath, newFileName);
    }

    public void DeleteImage(string image)
    {
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
        return Path.Combine(relativePath, Path.GetFileName(destinationPath));
    }
}