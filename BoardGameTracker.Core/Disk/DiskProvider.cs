using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Disk.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace BoardGameTracker.Core.Disk;

public class DiskProvider : IDiskProvider
{
    private readonly ILogger<DiskProvider> _logger;

    public DiskProvider(ILogger<DiskProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> WriteFile(Image image, string fileName, string path, IImageEncoder? encoder = null)
    {
        var uniqueFileName = fileName.GenerateUniqueFileName();
        var filePath = Path.Combine(path, uniqueFileName);

        if (encoder != null)
        {
            await image.SaveAsync(filePath, encoder);
        }
        else
        {
            await image.SaveAsync(filePath);
        }

        return uniqueFileName;
    }

    public async Task<string> WriteFile(Stream stream, string fileName, string path)
    {
        var uniqueFileName = fileName.GenerateUniqueFileName();
        var filePath = Path.Combine(path, uniqueFileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);

        return uniqueFileName;
    }

    public void DeleteFile(string path)
    {
        try
        {
            _logger.LogInformation("Removing file {Path}", path);
            File.Delete(path);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Can't delete file because it seems to be in use");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unknown error occurred while deleting file {Path}", path);
        }
    }
   
    public void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public void ClearFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path))
        {
            DeleteFile(file);
        }
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }
}