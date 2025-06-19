namespace BoardGameTracker.Common.Helpers;

public static class PathHelper
{
    private static readonly string CurrentDir = Directory.GetCurrentDirectory();
    private const string DataPath = "data";
    private const string ImagesPath = "images";
    
    public static readonly string CoverImagePath = Path.Combine(ImagesPath, "cover");
    public static readonly string ProfileImagePath = Path.Combine(ImagesPath, "profile");
    public static readonly string BadgeImagePath = Path.Combine(ImagesPath, "badges");
    
    public static readonly string FullRootImagePath = Path.Combine(CurrentDir, ImagesPath);
    public static readonly string FullCoverImagePath = Path.Combine(CurrentDir, CoverImagePath);
    public static readonly string FullBadgeImagePath = Path.Combine(CurrentDir, BadgeImagePath);
    public static readonly string FullProfileImagePath = Path.Combine(CurrentDir, ProfileImagePath);
    public static readonly string FullConfigFilePath = Path.Combine(CurrentDir, DataPath);
    public static readonly string FullConfigFile = Path.Combine(FullConfigFilePath, "config.xml");
}