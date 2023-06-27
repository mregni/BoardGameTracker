namespace BoardGameTracker.Core.Extensions;

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(input.First()) + input[1..];
    }
}