namespace BoardGameTracker.Common.Helpers;

public static class PathHelper
{
    private static readonly string CurrentDir = Directory.GetCurrentDirectory();
    private const string DataPath = "data";
    private const string TempPath = "temp";
    
    public static readonly string TempFilePath = Path.Combine(CurrentDir, TempPath);
    public static readonly string ConfigFilePath = Path.Combine(CurrentDir, DataPath);
    public static readonly string ConfigFile = Path.Combine(ConfigFilePath, "config.xml");
}