using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities.Helpers;

public abstract class BaseGame : HasId
{
    public string Title { get; set; }
    public int? YearPublished { get; set; }
    public string Description { get; set; }
    public string? Image { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? MinPlayTime { get; set; }
    public int? MaxPlayTime { get; set; }
    public int? MinAge { get; set; }
    public double? Rating { get; set; }
    public double? Weight { get; set; }
    public int? BggId { get; set; }
    public GameState State { get; set; }
    public double? BuyingPrice { get; set; }
    public double? SoldPrice { get; set; }
    public DateTime? AdditionDate { get; set; }
    public ICollection<Session> Sessions { get; set; }
}