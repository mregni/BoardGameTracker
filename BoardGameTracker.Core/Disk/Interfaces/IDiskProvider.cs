using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;

namespace BoardGameTracker.Core.Disk.Interfaces;

public interface IDiskProvider
{
    bool FileExists(string path);
    string ReadAllText(string filePath);
    void WriteAllText(string filename, string contents);
    Task<string> WriteFile(IFormFile file, UploadFileType type);
    Task<string> WriteFile(Image image, string fileName, string path);
    void EnsureFolder(string path);
    void DeleteFile(string path);
}