using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateGameCommand
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public required string Title { get; set; }

    [Range(1900, 2100, ErrorMessage = "Year published must be between 1900 and 2100")]
    public int? YearPublished { get; set; }

    [Url(ErrorMessage = "Image must be a valid URL")]
    public string? Image { get; set; }

    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    [Range(1, 100, ErrorMessage = "Minimum players must be between 1 and 100")]
    public int? MinPlayers { get; set; }

    [Range(1, 100, ErrorMessage = "Maximum players must be between 1 and 100")]
    public int? MaxPlayers { get; set; }

    [Range(1, 10000, ErrorMessage = "Minimum play time must be between 1 and 10000 minutes")]
    public int? MinPlayTime { get; set; }

    [Range(1, 10000, ErrorMessage = "Maximum play time must be between 1 and 10000 minutes")]
    public int? MaxPlayTime { get; set; }

    [Range(1, 99, ErrorMessage = "Minimum age must be between 1 and 99")]
    public int? MinAge { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BGG ID must be positive")]
    public int? BggId { get; set; }

    public GameState State { get; set; }

    public bool HasScoring { get; set; }

    [Range(0, 1000000, ErrorMessage = "Buying price must be between 0 and 1,000,000")]
    public decimal? BuyingPrice { get; set; }

    public DateTime? AdditionDate { get; set; }
}

public class UpdateGameCommand : CreateGameCommand
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid game ID")]
    public int Id { get; set; }
}
