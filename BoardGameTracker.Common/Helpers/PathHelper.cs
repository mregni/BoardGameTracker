namespace BoardGameTracker.Common.Helpers;

public static class PathHelper
{
    private static readonly string CurrentDir = Directory.GetCurrentDirectory();
    private const string DataPath = "data";
    public static readonly string ConfigFilePath = Path.Combine(CurrentDir, DataPath, "config.xml");
    public static readonly string LogPath = Path.Combine(CurrentDir, "logs");
}