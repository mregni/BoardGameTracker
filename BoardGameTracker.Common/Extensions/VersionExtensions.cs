namespace BoardGameTracker.Common.Extensions;

public static class VersionExtensions
{
    public static string ToVersionString(this Version? version)
    {
        if (version == null)
        {
            return string.Empty;
        }
        
        var major = version.Major != -1 ? version.Major : 0;
        var minor = version.Minor != -1 ? version.Minor : 0;
        var build = version.Build != -1 ? version.Build : 0;
        return $"{major}.{minor}.{build}";
    }
}