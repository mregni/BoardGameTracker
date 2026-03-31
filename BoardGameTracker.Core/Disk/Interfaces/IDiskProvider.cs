using BoardGameTracker.Common.Enums;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;

namespace BoardGameTracker.Core.Disk.Interfaces;

public interface IDiskProvider
{
    Task<string> WriteFile(Image image, string fileName, string path);
    void EnsureFolder(string path);
    void DeleteFile(string path);
}