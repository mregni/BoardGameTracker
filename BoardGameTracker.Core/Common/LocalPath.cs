namespace BoardGameTracker.Core.Common;

public static class LocalPath
{
    public const string Root = "/";

    public static bool IsSafe(string? path)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/')
        {
            return false;
        }

        if (path.Length > 1 && (path[1] == '/' || path[1] == '\\'))
        {
            return false;
        }

        if (path.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
        {
            return false;
        }

        return !Uri.TryCreate(path, UriKind.Absolute, out var uri) || uri.Scheme == Uri.UriSchemeFile;
    }

    public static string Normalize(string? path) => IsSafe(path) ? path! : Root;
}
