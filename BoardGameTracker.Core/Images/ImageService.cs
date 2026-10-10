using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace BoardGameTracker.Core.Images;

public class ImageService : IImageService
{
    private const int ImageSize = 512;
    public const string HttpClientName = "images";

    private const long MaxDownloadBytes = 15 * 1024 * 1024;
    private const long MaxUploadBytes = 15 * 1024 * 1024;
    private const long MaxUploadPixels = 50_000_000;
    private const int WebpQuality = 80;
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

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
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength is > MaxDownloadBytes)
                {
                    _logger.LogWarning("Image at {ImageUrl} exceeds the {Max} byte limit, using placeholder", imageUrl, MaxDownloadBytes);
                    return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
                }

                var fileName = $"{imageFileName}.webp";
                var imageContent = await ReadWithLimitAsync(response.Content, MaxDownloadBytes);
                if (imageContent == null)
                {
                    _logger.LogWarning("Image at {ImageUrl} exceeds the {Max} byte limit, using placeholder", imageUrl, MaxDownloadBytes);
                    return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
                }

                using var data = SKData.CreateCopy(imageContent);
                using var codec = SKCodec.Create(data);
                if (codec == null)
                {
                    _logger.LogWarning("Image at {ImageUrl} is not a supported image format, using placeholder", imageUrl);
                    return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
                }

                if ((long)codec.Info.Width * codec.Info.Height > MaxUploadPixels)
                {
                    _logger.LogWarning("Image at {ImageUrl} exceeds the {Max} pixel limit, using placeholder", imageUrl, MaxUploadPixels);
                    return CreateNoImageImages(imageFileName, PathHelper.FullCoverImagePath, PathHelper.CoverImagePath);
                }

                await using var webp = ResizeToWebp(codec);
                var newFileName = await _diskProvider.WriteFile(webp, fileName, PathHelper.FullCoverImagePath);
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

        if (file.Length > MaxUploadBytes)
        {
            throw new ValidationException(Constants.Errors.ImageTooLarge);
        }

        using var buffered = new MemoryStream();
        await using (var uploadStream = file.OpenReadStream())
        {
            await uploadStream.CopyToAsync(buffered);
        }

        using var data = SKData.CreateCopy(buffered.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec == null)
        {
            _logger.LogWarning("Rejected upload of type {UploadType}: unsupported image format", type);
            throw new ValidationException(Constants.Errors.ImageUnsupportedFormat);
        }

        if ((long)codec.Info.Width * codec.Info.Height > MaxUploadPixels)
        {
            throw new ValidationException(Constants.Errors.ImageTooLarge);
        }

        await using var webp = ResizeToWebp(codec);
        var outputFileName = Path.ChangeExtension(file.FileName, ".webp");
        var newFileName = await _diskProvider.WriteFile(webp, outputFileName, fullPath);
        var path = Path.Combine(folder, newFileName);
        return $"/{path.Replace("\\", "/")}";
    }

    public void DeleteImage(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return;
        }

        var physicalPath = PathHelper.MapImageWebPathToPhysical(image);
        if (physicalPath == null)
        {
            _logger.LogWarning("Refusing to delete image outside the images directory: {Image}", image);
            return;
        }

        _diskProvider.DeleteFile(physicalPath);
    }

    public void ClearAllImages()
    {
        _diskProvider.ClearFolder(PathHelper.FullCoverImagePath);
        _diskProvider.ClearFolder(PathHelper.FullProfileImagePath);
    }

    private static MemoryStream ResizeToWebp(SKCodec codec)
    {
        using var source = SKBitmap.Decode(codec)
            ?? throw new ValidationException(Constants.Errors.ImageUnsupportedFormat);
        using var sourceImage = SKImage.FromBitmap(source);
        using var surface = SKSurface.Create(new SKImageInfo(ImageSize, ImageSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.DrawImage(sourceImage, new SKRect(0, 0, ImageSize, ImageSize), Sampling);
        using var resized = surface.Snapshot();
        using var encoded = resized.Encode(SKEncodedImageFormat.Webp, WebpQuality);
        return new MemoryStream(encoded.ToArray());
    }

    private static async Task<byte[]?> ReadWithLimitAsync(HttpContent content, long maxBytes)
    {
        await using var stream = await content.ReadAsStreamAsync();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private string CreateNoImageImages(string fileName, string absolutePath, string relativePath)
    {
        var sourcePath = PathHelper.NoImagePlaceholderPath;
        fileName += Path.GetExtension(sourcePath);

        if (!File.Exists(sourcePath))
        {
            _logger.LogWarning("Placeholder image {SourcePath} is missing, storing no image instead", sourcePath);
            return string.Empty;
        }

        var destinationPath = Path.Combine(absolutePath, fileName.GenerateUniqueFileName());
        File.Copy(sourcePath, destinationPath);
        var path = Path.Combine(relativePath, Path.GetFileName(destinationPath));
        return $"/{path.Replace("\\", "/")}";
    }
}