using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateSessionCommand
{
    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string? Comment { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid game ID")]
    public required int GameId { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime Start { get; set; }

    [Range(1, 10000, ErrorMessage = "Duration must be between 1 and 10000 minutes")]
    public int Minutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid location ID")]
    public int? LocationId { get; set; }

    [Required(ErrorMessage = "At least one player is required")]
    [MinLength(1, ErrorMessage = "At least one player is required")]
    public List<CreatePlayerSessionCommand> PlayerSessions { get; set; } = [];

    public List<int> ExpansionIds { get; set; } = [];
}

public class CreatePlayerSessionCommand
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid player ID")]
    public int PlayerId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Score cannot be negative")]
    public double? Score { get; set; }

    public bool FirstPlay { get; set; }

    public bool Won { get; set; }
}

public class UpdateSessionCommand : CreateSessionCommand
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid session ID")]
    public int Id { get; set; }
}
