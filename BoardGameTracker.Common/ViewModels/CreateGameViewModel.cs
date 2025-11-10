namespace BoardGameTracker.Common.ViewModels;

public class CreateGameViewModel
{
    public required string Title { get; set; }
    public int? YearPublished { get; set; }
    public string? Image { get; set; }
    public string? Description { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? MinPlayTime { get; set; }
    public int? MaxPlayTime { get; set; }
    public int? MinAge { get; set; }
    public int? BggId { get; set; }
    public int State { get; set; }
    public bool HasScoring { get; set; }
    public double? BuyingPrice { get; set; }
    public DateTime? AdditionDate { get; set; }
}