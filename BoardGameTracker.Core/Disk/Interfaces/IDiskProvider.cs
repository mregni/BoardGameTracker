using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace BoardGameTracker.Core.Disk.Interfaces;

public interface IDiskProvider
{
    Task<string> WriteFile(Image image, string fileName, string path, IImageEncoder? encoder = null);
    void EnsureFolder(string path);
    void DeleteFile(string path);
}