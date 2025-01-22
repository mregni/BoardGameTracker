namespace BoardGameTracker.Common.Extensions;

public static class VersionExtensions
{
    public static string ToVersionString(this Version? version)
    {
        return version == null ? string.Empty : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}