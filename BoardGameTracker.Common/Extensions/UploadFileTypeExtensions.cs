using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Helpers;

namespace BoardGameTracker.Common.Extensions;

public static class UploadFileTypeExtensions
{
    public static string ConvertToPath(this UploadFileType type)
    {
        return type switch
        {
            UploadFileType.Profile => PathHelper.FullProfileImagePath,
            _ => string.Empty
        };
    }
}