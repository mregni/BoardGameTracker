using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class AskRagCommand
{
    [Required]
    [StringLength(2000)]
    public string Question { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? ManualId { get; set; }
}
