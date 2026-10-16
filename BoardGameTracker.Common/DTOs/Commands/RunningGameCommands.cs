using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class StartRunningGameCommand
{
    [Range(1, int.MaxValue)]
    public int GameId { get; set; }

    [MinLength(1)]
    public List<int> PlayerIds { get; set; } = [];
}

public class FinishRunningGameCommand
{
    [MinLength(1)]
    public List<CreatePlayerSessionCommand> PlayerSessions { get; set; } = [];

    public DateTime? Start { get; set; }

    [Range(1, int.MaxValue)]
    public int? Minutes { get; set; }

    [Range(1, int.MaxValue)]
    public int? LocationId { get; set; }

    public string? Comment { get; set; }

    public List<int> ExpansionIds { get; set; } = [];
}
