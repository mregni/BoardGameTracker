using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreatePlayerCommand
{
    [Required(ErrorMessage = "Player name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Player name must be between 1 and 100 characters")]
    public required string Name { get; set; }

    [Url(ErrorMessage = "Image must be a valid URL")]
    public string? Image { get; set; }
}

public class UpdatePlayerCommand : CreatePlayerCommand
{
    public int Id { get; set; }
}
