using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Commands;
using BoardGameTracker.Core.Disk.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Core.Disk;

public class DiskProvider : IDiskProvider
{
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

    public async Task<string> WriteFile(IFormFile file, string path)
    {
        var uniqueFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + Path.GetRandomFileName() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(path, uniqueFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return uniqueFileName;
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