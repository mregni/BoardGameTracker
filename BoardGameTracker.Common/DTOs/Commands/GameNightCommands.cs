using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateGameNightCommand
{
    public required string Title { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int HostId { get; set; }
    public int LocationId { get; set; }
    public List<int> SuggestedGameIds { get; set; } = [];
    public List<int> InvitedPlayerIds { get; set; } = [];
}

public class UpdateGameNightCommand : CreateGameNightCommand
{
    public int Id { get; set; }
}

public class UpdateRsvpCommand
{
    public int Id { get; set; }
    public GameNightRsvpState State { get; set; }
}
