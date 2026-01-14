using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateLocationCommand
{
    [Required(ErrorMessage = "Location name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Location name must be between 1 and 100 characters")]
    public required string Name { get; set; }
}

public class UpdateLocationCommand : CreateLocationCommand
{
    public int Id { get; set; }
}
