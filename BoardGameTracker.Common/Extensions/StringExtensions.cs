using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Extensions;

public static class StringExtensions
{
    public static PersonType StringToPersonTypeEnum(this string type)
    {
        return type switch
        {
            Constants.Bgg.Artist => PersonType.Artist,
            Constants.Bgg.Designer => PersonType.Designer,
            _ => PersonType.Publisher
        };
    }
    
    public static T ToEnum<T>(this string value, bool ignoreCase = true) where T : Enum
    {
        return (T)Enum.Parse(typeof(T), value, ignoreCase);
    }

    public static string GenerateUniqueFileName(this string fileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var randomPart = Path.GetRandomFileName();
        var extension = Path.GetExtension(fileName);

        return $"{fileNameWithoutExtension}_{randomPart}{extension}";
    }
    
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(input.First()) + input[1..];
    }
    
    public static string AddFileNameSuffix(this string fileName, string suffix)
    {
        var directory = Path.GetDirectoryName(fileName) ?? "";
        var extension = Path.GetExtension(fileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var newFileName = $"{fileNameWithoutExtension}-{suffix}{extension}";
        return Path.Combine(directory, newFileName);
    }
}