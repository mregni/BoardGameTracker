namespace BoardGameTracker.Common.Helpers;

public static class PathHelper
{
    private static readonly string CurrentDir = Directory.GetCurrentDirectory();
    private const string ImagesPath = "images";
    
    public static readonly string CoverImagePath = Path.Combine(ImagesPath, "cover");
    public static readonly string ProfileImagePath = Path.Combine(ImagesPath, "profile");

    public static readonly string FullRootImagePath = Path.Combine(CurrentDir, ImagesPath);
    public static readonly string FullCoverImagePath = Path.Combine(CurrentDir, CoverImagePath);
    public static readonly string FullProfileImagePath = Path.Combine(CurrentDir, ProfileImagePath);

    /// <summary>
    /// Maps a stored web-relative image path (e.g. "/images/cover/foo.jpg") to its physical path on disk.
    /// Returns null if the resolved path would fall outside the images root (path-traversal guard).
    /// </summary>
    public static string? MapImageWebPathToPhysical(string webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath))
        {
            return null;
        }

        var relative = webPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(CurrentDir, relative));

        var imagesRoot = Path.GetFullPath(FullRootImagePath);
        if (!fullPath.StartsWith(imagesRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        return fullPath;
    }
}