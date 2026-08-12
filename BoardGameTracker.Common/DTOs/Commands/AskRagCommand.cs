namespace BoardGameTracker.Common.DTOs.Commands;

public class AskRagCommand
{
    public string Question { get; set; } = string.Empty;
    public int? ManualId { get; set; }
}
