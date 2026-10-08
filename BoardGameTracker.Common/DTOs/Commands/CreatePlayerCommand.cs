using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreatePlayerCommand
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    public string? Image { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}

public class UpdatePlayerCommand : CreatePlayerCommand
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
