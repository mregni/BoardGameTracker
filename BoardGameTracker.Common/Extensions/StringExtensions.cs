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
}