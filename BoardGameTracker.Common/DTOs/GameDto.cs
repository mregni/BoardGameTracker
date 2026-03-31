using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class GameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? YearPublished { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? MinPlayTime { get; set; }
    public int? MaxPlayTime { get; set; }
    public int? MinAge { get; set; }
    public bool isLoaned { get; set; }
    public double? Rating { get; set; }
    public double? Weight { get; set; }
    public int? BggId { get; set; }
    public GameState State { get; set; }
    public bool HasScoring { get; set; }
    public decimal? BuyingPrice { get; set; }
    public decimal? SoldPrice { get; set; }
    public DateTime? AdditionDate { get; set; }
    public List<ExpansionDto>? Expansions { get; set; }
    public List<GameLinkDto>? Categories { get; set; }
    public List<GameLinkDto>? Mechanics { get; set; }
    public List<GamePersonDto>? People { get; set; }
}

public class GameLinkDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GamePersonDto : GameLinkDto
{
    public PersonType Type { get; set; }
}
