using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Core.Disk.Interfaces;

public interface IDiskProvider
{
    bool FileExists(string path);
    string ReadAllText(string filePath);
    void WriteAllText(string filename, string contents);
    Task<string> WriteFile(IFormFile file, string path);
    void EnsureFolder(string path);
}