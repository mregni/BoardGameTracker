using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateSessionCommand
{
    public string? Comment { get; set; }

    [Range(1, int.MaxValue)]
    public required int GameId { get; set; }

    public DateTime Start { get; set; }

    [Range(0, int.MaxValue)]
    public int Minutes { get; set; }

    [Range(1, int.MaxValue)]
    public int? LocationId { get; set; }

    public List<CreatePlayerSessionCommand> PlayerSessions { get; set; } = [];

    public List<int> ExpansionIds { get; set; } = [];
}

public class CreatePlayerSessionCommand
{
    [Range(1, int.MaxValue)]
    public int PlayerId { get; set; }

    public double? Score { get; set; }
    public bool FirstPlay { get; set; }
    public bool Won { get; set; }
}

public class UpdateSessionCommand : CreateSessionCommand
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
