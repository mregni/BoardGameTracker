using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateGameCommand
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
    public GameState State { get; set; }
    public bool HasScoring { get; set; }
    public decimal? BuyingPrice { get; set; }

    public DateTime? AdditionDate { get; set; }
}

public class UpdateGameCommand : CreateGameCommand
{
    public int Id { get; set; }
}
