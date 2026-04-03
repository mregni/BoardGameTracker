using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Disk.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace BoardGameTracker.Core.Disk;

public class DiskProvider : IDiskProvider
{
    private readonly ILogger<DiskProvider> _logger;

    public DiskProvider(ILogger<DiskProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> WriteFile(Image image, string fileName, string path)
    {
        var uniqueFileName = fileName.GenerateUniqueFileName();
        var filePath = Path.Combine(path, uniqueFileName);
    
        await image.SaveAsync(filePath);
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
}