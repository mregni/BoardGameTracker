using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateLocationCommand
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }
}

public class UpdateLocationCommand : CreateLocationCommand
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
