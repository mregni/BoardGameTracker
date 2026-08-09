using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace BoardGameTracker.Core.Disk.Interfaces;

public interface IDiskProvider
{
    Task<string> WriteFile(Image image, string fileName, string path, IImageEncoder? encoder = null);
    Task<string> WriteFile(Stream stream, string fileName, string path);
    void EnsureFolder(string path);
    void DeleteFile(string path);
    void ClearFolder(string path);
    bool FileExists(string path);
    Stream OpenRead(string path);
}