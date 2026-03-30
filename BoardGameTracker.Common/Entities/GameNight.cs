using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class GameNight: HasId
{
    public string Title { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public Guid LinkId { get; private set; }
    public DateTime StartDate { get; private set; }

    public int HostId { get; private set; }
    public Player Host { get; private set; } = null!;
    
    public int LocationId { get; private set; }
    public Location Location { get; private set; } = null!;

    public ICollection<Game> SuggestedGames { get; private set; } = [];
    public ICollection<GameNightRsvp> InvitedPlayers { get; private set; } = [];

    public static GameNight Create(string title, string notes, DateTime startDate, int hostId, int locationId)
    {
        return new GameNight()
        {
            Title = title,
            Notes =  notes,
            StartDate = startDate,
            HostId = hostId,
            LocationId = locationId,
            LinkId = Guid.NewGuid(),
        };
    }

    public void Update(string title, string notes, DateTime startDate, int hostId, int locationId)
    {
        Title = title;
        Notes = notes;
        StartDate = startDate;
        HostId = hostId;
        LocationId = locationId;
    }

    public void SetInvitedPlayers(IEnumerable<GameNightRsvp> gameNightRsvps)
    {
        InvitedPlayers = new List<GameNightRsvp>();
        foreach (var gameNightRsvp in gameNightRsvps)
        {
            if (InvitedPlayers.Any(x => x.PlayerId == gameNightRsvp.PlayerId))
            {
                continue;
            }
        
            InvitedPlayers.Add(gameNightRsvp);
        }
    }

    public void RemoveInvitedPlayers(IEnumerable<GameNightRsvp> gameNightRsvps)
    {
        foreach (var rsvp in gameNightRsvps)
        {
            InvitedPlayers.Remove(rsvp);
        }
    }

    public void AddInvitedPlayers(IEnumerable<int> playerIds)
    {
        foreach (var playerId in playerIds)
        {
            if (InvitedPlayers.Any(x => x.PlayerId == playerId))
            {
                continue;
            }
            
            InvitedPlayers.Add(GameNightRsvp.Create(playerId, GameNightRsvpState.Pending));
        }
    }

    public void SetSuggestedGames(IEnumerable<Game> suggestedGames)
    {
        SuggestedGames = new List<Game>();
        foreach (var suggestedGame in suggestedGames)
        {
            if (SuggestedGames.Any(x => x.Id == suggestedGame.Id))
            {
                continue;
            }
        
            SuggestedGames.Add(suggestedGame);
        }
    }
}