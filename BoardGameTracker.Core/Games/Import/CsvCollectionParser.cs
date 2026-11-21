using System.Globalization;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Games.Import;

public record ParsedCsv(List<BggImportGame> Games, HashSet<int> ExpansionIds, Dictionary<int, int> Lines, List<CsvRejectedRow> Rejected);

public static class CsvCollectionParser
{
    public const int MaxRows = 2000;

    private static readonly string[] OwnedColumns = ["own"];
    private static readonly string[] PreviouslyOwnedColumns = ["prevowned"];
    private static readonly string[] ForTradeColumns = ["fortrade"];
    private static readonly string[] WantedColumns = ["want", "wanttobuy", "wishlist", "preordered"];

    public static ParsedCsv Parse(string text, DateTime today)
    {
        var rows = CsvReader.Read(text);
        if (rows.Count == 0)
        {
            throw new DomainException(Constants.Errors.CsvImportEmpty);
        }

        var header = rows[0]
            .Select((name, index) => (Name: name.Trim().ToLowerInvariant(), Index: index))
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.First().Index);
        if (!header.ContainsKey("objectid"))
        {
            throw new DomainException(Constants.Errors.CsvImportMissingObjectId);
        }

        var dataRows = rows
            .Select((cells, index) => (Cells: cells, Line: index + 1))
            .Skip(1)
            .Where(x => x.Cells.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToList();
        if (dataRows.Count > MaxRows)
        {
            throw new DomainException(Constants.Errors.CsvImportTooManyRows);
        }

        var hasStateColumns = OwnedColumns.Concat(PreviouslyOwnedColumns).Concat(ForTradeColumns).Concat(WantedColumns)
            .Any(header.ContainsKey);
        var result = new ParsedCsv([], [], [], []);
        foreach (var (cells, line) in dataRows)
        {
            ParseRow(new Row(header, cells), line, hasStateColumns, today, result);
        }

        foreach (var game in result.Games.Where(x => x.BaseGameBggId != null))
        {
            game.BaseGameTitle = result.Games.FirstOrDefault(x => x.BggId == game.BaseGameBggId)?.Title;
        }

        return result;
    }

    private static void ParseRow(Row row, int line, bool hasStateColumns, DateTime today, ParsedCsv result)
    {
        var title = row.Get("objectname");
        void Reject(string reason, int? bggId = null) =>
            result.Rejected.Add(new CsvRejectedRow { Line = line, Title = title, BggId = bggId, Reason = reason });

        if (!TryParseId(row.Get("objectid"), out var bggId))
        {
            Reject(CsvRejectReasons.InvalidId);
            return;
        }

        if (result.Games.Any(x => x.BggId == bggId))
        {
            Reject(CsvRejectReasons.Duplicate, bggId);
            return;
        }

        int? baseGameBggId = null;
        var baseGame = row.Get("basegame");
        if (baseGame.Length > 0)
        {
            if (!TryParseId(baseGame, out var parsedBase) || parsedBase == bggId)
            {
                Reject(CsvRejectReasons.InvalidBaseGame, bggId);
                return;
            }

            baseGameBggId = parsedBase;
        }

        var state = hasStateColumns
            ? StatusExtensions.ToGameState(
                row.Flag(OwnedColumns),
                row.Flag(PreviouslyOwnedColumns),
                row.Flag(ForTradeColumns),
                row.Flag(WantedColumns))
            : GameState.Owned;
        if (state == null)
        {
            Reject(CsvRejectReasons.NoState, bggId);
            return;
        }

        if (!TryParsePrice(row.Get("pricepaid"), out var price))
        {
            Reject(CsvRejectReasons.InvalidPrice, bggId);
            return;
        }

        if (!TryParseDate(row.Get("acquisitiondate"), today, out var addedDate))
        {
            Reject(CsvRejectReasons.InvalidDate, bggId);
            return;
        }

        result.Games.Add(new BggImportGame
        {
            BggId = bggId,
            Title = title,
            State = state.Value,
            Price = price,
            LastModified = addedDate,
            BaseGameBggId = baseGameBggId
        });
        result.Lines[bggId] = line;

        if (baseGameBggId != null || string.Equals(row.Get("itemtype"), "expansion", StringComparison.OrdinalIgnoreCase))
        {
            result.ExpansionIds.Add(bggId);
        }
    }

    private static bool TryParseId(string value, out int id)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
    }

    private static bool TryParsePrice(string value, out decimal? price)
    {
        price = null;
        if (value.Length == 0)
        {
            return true;
        }

        var normalized = value.Contains('.') ? value.Replace(",", string.Empty) : value.Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        price = parsed > 0 ? parsed : null;
        return true;
    }

    private static bool TryParseDate(string value, DateTime today, out DateTime date)
    {
        date = today;
        if (value.Length == 0 || value == "0000-00-00")
        {
            return true;
        }

        if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        date = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        return true;
    }

    private sealed class Row(Dictionary<string, int> header, string[] cells)
    {
        public string Get(string column)
        {
            return header.TryGetValue(column, out var index) && index < cells.Length ? cells[index].Trim() : string.Empty;
        }

        public bool Flag(string[] columns)
        {
            return columns.Any(column => Get(column).ToLowerInvariant() is "1" or "true" or "yes" or "x");
        }
    }
}

public static class CsvRejectReasons
{
    public const string InvalidId = "invalid-id";
    public const string Duplicate = "duplicate";
    public const string InvalidBaseGame = "invalid-base-game";
    public const string NoState = "no-state";
    public const string InvalidPrice = "invalid-price";
    public const string InvalidDate = "invalid-date";
    public const string NoBaseGame = "no-base-game";
    public const string NotFound = "not-found";
}
