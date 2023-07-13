using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Commands;
using BoardGameTracker.Core.Disk.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Disk;

public class DiskProvider : IDiskProvider
{
    private readonly ILogger<DiskProvider> _logger;

    public DiskProvider(ILogger<DiskProvider> logger)
    {
        _logger = logger;
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public string ReadAllText(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    public void WriteAllText(string filename, string contents)
    {
        RemoveReadOnly(filename);
        
        using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(fs);
        writer.WriteAsync(contents);
    }

    public async Task<string> WriteFile(IFormFile file, UploadFileType type)
    {
        var uniqueFileName = file.FileName.GenerateUniqueFileName();
        var path = type.ConvertToPath();
        var filePath = Path.Combine(path, uniqueFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return uniqueFileName;
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
        catch (IOException)
        {
            _logger.LogError("Can't delete file because it seems to be in use");
        }
        catch (Exception e)
        {
            _logger.LogError("Unknow error occured while deleting file {Path}: {Message}", path, e.Message);
        }
    }
   
    public void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void RemoveReadOnly(string path)
    {
        if (File.Exists(path))
        {
            var attributes = File.GetAttributes(path);

            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                var newAttributes = attributes & ~FileAttributes.ReadOnly;
                File.SetAttributes(path, newAttributes);
            }
        }
    }
}