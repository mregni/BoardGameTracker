using System.Text.RegularExpressions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public async Task<string> SaveProfileImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return CreateNoImageImages("profile", PathHelper.FullProfileImagePath, PathHelper.ProfileImagePath);
        }
        
        using var image = await Image.LoadAsync(file.OpenReadStream());
        image.Mutate(x => x.Resize(512, 512));
        var newFileName = await _diskProvider.WriteFile(image, file.FileName, PathHelper.FullProfileImagePath);
        var path = Path.Combine(PathHelper.ProfileImagePath, newFileName);
        return $"/{path.Replace("\\", "/")}";
    }

    public void DeleteImage(string image)
    {
        _diskProvider.DeleteFile(image);
    }

    public async Task<string> DownloadBackgroundImage(int bggId)
    {
        var url = $"https://boardgamegeek.com/boardgame/{bggId}";
        var web = new HtmlWeb();
        var document = await web.LoadFromWebAsync(url);
        
        var node = document.DocumentNode.SelectSingleNode("//script[contains(text(), 'GEEK.geekitemPreload')]");
        if (node != null)
        {
            var regex = new Regex(@"GEEK\.geekitemPreload\s*=\s*(\{.*?\});", RegexOptions.Singleline);
            var match = regex.Match(node.InnerText);

            if (match.Success)
            {
                var json = match.Groups[1].Value;
                var backgroundImageUrl = JsonConvert.DeserializeObject<JObject>(json)?["item"]["topimageurl"].Value<string>();

                if (!string.IsNullOrEmpty(backgroundImageUrl))
                {
                    using var client = new HttpClient();
                    var response = await client.GetAsync(backgroundImageUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var fileName = CreateFileNameFromUrl(backgroundImageUrl, $"{bggId}-bg");
                        var imageContent = await response.Content.ReadAsByteArrayAsync();

                        using var image = Image.Load(imageContent);
                        var newFileName = await _diskProvider.WriteFile(image, fileName, PathHelper.FullBackgroundImagePath);
                        var path = Path.Combine(PathHelper.BackgroundImagePath, newFileName);
                        return $"/{path.Replace("\\", "/")}";
                    }
                }
            }
        }
        
        return CreateNoImageImages($"{bggId}-bg", PathHelper.FullBackgroundImagePath, PathHelper.BackgroundImagePath);
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