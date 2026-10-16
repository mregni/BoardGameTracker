namespace BoardGameTracker.Core.Disk.Interfaces;

public interface IDiskProvider
{
    Task<string> WriteFile(Stream stream, string fileName, string path);
    void EnsureFolder(string path);
    void DeleteFile(string path);
    void ClearFolder(string path);
    bool FileExists(string path);
    Stream OpenRead(string path);
}