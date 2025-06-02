using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Extensions;

public static class StringExtensions
{
    public static PersonType ToPersonTypeEnum(this string? type)
    {
        return type switch
        {
            Constants.Bgg.Artist => PersonType.Artist,
            Constants.Bgg.Designer => PersonType.Designer,
            _ => PersonType.Publisher
        };
    }

    public static string GenerateUniqueFileName(this string fileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var randomPart = Path.GetRandomFileName();
        var extension = Path.GetExtension(fileName);

        return $"{fileNameWithoutExtension}_{randomPart}{extension}";
    }
    
    public static string FirstCharToUpper(this string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(input.First()) + input[1..];
    }
}