namespace BoardGameTracker.Common.Models.Bgg;

public class CsvImportResult
{
    public IList<BggImportGame> Games { get; set; } = [];
    public IList<CsvRejectedRow> Rejected { get; set; } = [];
}

public class CsvRejectedRow
{
    public int Line { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? BggId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
