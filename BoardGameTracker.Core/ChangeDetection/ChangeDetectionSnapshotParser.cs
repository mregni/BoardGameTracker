using System.Globalization;
using System.Text.RegularExpressions;
using BoardGameTracker.Common.Models.ChangeDetection;

namespace BoardGameTracker.Core.ChangeDetection;

public static partial class ChangeDetectionSnapshotParser
{
    [GeneratedRegex(@"In Stock:\s*(?<stock>True|False)\s*-\s*Price:\s*(?<price>[0-9]+(?:[.,][0-9]+)?)?",
        RegexOptions.IgnoreCase)]
    private static partial Regex SnapshotRegex();

    public static ChangeDetectionResult Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ChangeDetectionResult.Unavailable();
        }

        var match = SnapshotRegex().Match(content);
        if (!match.Success)
        {
            return ChangeDetectionResult.Unavailable();
        }

        var inStock = bool.Parse(match.Groups["stock"].Value);

        decimal? price = null;
        var priceValue = match.Groups["price"].Value;
        if (!string.IsNullOrWhiteSpace(priceValue))
        {
            var normalized = priceValue.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                price = parsed;
            }
        }

        return new ChangeDetectionResult
        {
            Available = true,
            InStock = inStock,
            Price = price
        };
    }
}
