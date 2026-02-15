namespace BoardGameTracker.Common.DTOs;

public class ExpansionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BggId { get; set; }
    public int? GameId { get; set; }
}
