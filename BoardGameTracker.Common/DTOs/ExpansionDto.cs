using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class ExpansionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? BggId { get; set; }
    public int? GameId { get; set; }
    public string? Image { get; set; }
    public GameState State { get; set; }
    public decimal? BuyingPrice { get; set; }
    public decimal? SoldPrice { get; set; }
    public DateTime? AdditionDate { get; set; }
}
