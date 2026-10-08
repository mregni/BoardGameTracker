using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateExpansionCommand
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; set; }
}
