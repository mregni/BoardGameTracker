using System.Globalization;
using System.Text.RegularExpressions;
using BoardGameTracker.Common.Models.ChangeDetection;

namespace BoardGameTracker.Core.ChangeDetection;

public static partial class ChangeDetectionSnapshotParser
{
    [GeneratedRegex(@"In Stock:\s*(?<stock>True|False|None)\s*-\s*Price:\s*(?<price>[0-9][0-9.,]*)?",
        RegexOptions.IgnoreCase)]
    private static partial Regex SnapshotRegex();

    public static ChangeDetectionResult Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ChangeDetectionResult.Unavailable(ChangeDetectionStatus.ParseError);
        }

        var match = SnapshotRegex().Match(content);
        if (!match.Success)
        {
            return ChangeDetectionResult.Unavailable(ChangeDetectionStatus.ParseError);
        }

        var stockToken = match.Groups["stock"].Value;
        bool? inStock = stockToken.Equals("None", StringComparison.OrdinalIgnoreCase)
            ? null
            : bool.Parse(stockToken);

        return new ChangeDetectionResult
        {
            Status = ChangeDetectionStatus.Ok,
            InStock = inStock,
            Price = ParsePrice(match.Groups["price"].Value)
        };
    }

    private static decimal? ParsePrice(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var hasDot = raw.Contains('.');
        var hasComma = raw.Contains(',');

        string normalized;
        if (hasDot && hasComma)
        {
            return null;
        }
        else if (hasComma)
        {
            if (raw.IndexOf(',') != raw.LastIndexOf(','))
            {
                return null;
            }

            normalized = raw.Replace(',', '.');
        }
        else
        {
            if (raw.IndexOf('.') != raw.LastIndexOf('.'))
            {
                return null;
            }

            normalized = raw;
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
