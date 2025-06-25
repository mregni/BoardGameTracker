using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class CreateSessionViewModel
{
    public string? Id { get; set; }
    public string? Comment { get; set; }
    public string GameId { get; set; }
    public List<PlayerSessionViewModel> PlayerSessions { get; set; }
    public DateTime Start { get; set; }
    public double Minutes { get; set; }
    public string? LocationId  { get; set; }
    public List<SessionFlag>? Flags { get; set; }
    public List<int> ExpansionIds { get; set; }
}