using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class GameNight: HasId
{
    public static readonly TimeSpan PlayWindow = TimeSpan.FromHours(12);
    public static readonly TimeSpan PlayOpensBefore = TimeSpan.FromHours(12);

    public string Title { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public Guid LinkId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndedAt { get; private set; }

    public int HostId { get; private set; }
    public Player Host { get; private set; } = null!;
    
    public int LocationId { get; private set; }
    public Location Location { get; private set; } = null!;

    public ICollection<Game> SuggestedGames { get; private set; } = [];
    public ICollection<GameNightRsvp> InvitedPlayers { get; private set; } = [];
    public ICollection<Session> Sessions { get; private set; } = [];
    public ICollection<RunningGame> RunningGames { get; private set; } = [];
    public ICollection<GameNightAttendance> Attendance { get; private set; } = [];

    public static GameNight Create(string title, string notes, DateTime startDate, int hostId, int locationId)
    {
        return new GameNight()
        {
            Title = Guard.Against.NullOrWhiteSpace(title),
            Notes = notes ?? string.Empty,
            StartDate = Guard.Against.Default(startDate),
            HostId = Guard.Against.NegativeOrZero(hostId),
            LocationId = Guard.Against.NegativeOrZero(locationId),
            LinkId = Guid.NewGuid(),
        };
    }

    public void Update(string title, string notes, DateTime startDate, int hostId, int locationId)
    {
        Title = Guard.Against.NullOrWhiteSpace(title);
        Notes = notes ?? string.Empty;
        StartDate = Guard.Against.Default(startDate);
        HostId = Guard.Against.NegativeOrZero(hostId);
        LocationId = Guard.Against.NegativeOrZero(locationId);
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

    public bool AddSuggestedGame(Game game)
    {
        Guard.Against.Null(game);
        if (SuggestedGames.Any(x => x.Id == game.Id))
        {
            return false;
        }

        SuggestedGames.Add(game);
        return true;
    }

    public bool RemoveSuggestedGame(int gameId)
    {
        var game = SuggestedGames.FirstOrDefault(x => x.Id == gameId);
        if (game == null)
        {
            return false;
        }

        SuggestedGames.Remove(game);
        return true;
    }

    public static bool IsOverAt(DateTime startDate, DateTime? endedAt, DateTime utcNow)
    {
        return endedAt.HasValue || startDate + PlayWindow <= utcNow;
    }

    public bool IsOver(DateTime utcNow)
    {
        return IsOverAt(StartDate, EndedAt, utcNow);
    }

    public bool HasPlayOpened(DateTime utcNow)
    {
        return utcNow >= StartDate.ToUniversalTime() - PlayOpensBefore;
    }

    public void End(DateTime endedAt)
    {
        EndedAt = Guard.Against.Default(endedAt);
    }

    public void Reopen()
    {
        EndedAt = null;
    }

    public List<int> GetPlayedPlayerIds()
    {
        return Sessions
            .SelectMany(x => x.PlayerSessions.Select(p => p.PlayerId))
            .Concat(RunningGames.SelectMany(x => x.GetPlayerIds()))
            .Distinct()
            .ToList();
    }

    public List<int> GetAttendeeIds()
    {
        return NightAttendanceRule.Derive(
            HostId,
            GetPlayedPlayerIds(),
            Attendance.Select(x => new AttendanceOverride(x.PlayerId, x.Present)));
    }

    public void SetAttendance(IEnumerable<int> presentPlayerIds)
    {
        var desired = NightAttendanceRule.OverridesFor(HostId, GetPlayedPlayerIds(), presentPlayerIds);

        foreach (var stale in Attendance.Where(x => desired.All(d => d.PlayerId != x.PlayerId)).ToList())
        {
            Attendance.Remove(stale);
        }

        foreach (var entry in desired)
        {
            var existing = Attendance.FirstOrDefault(x => x.PlayerId == entry.PlayerId);
            if (existing == null)
            {
                Attendance.Add(GameNightAttendance.Create(entry.PlayerId, entry.Present));
            }
            else
            {
                existing.UpdatePresent(entry.Present);
            }
        }
    }

    public double GetTotalPlayMinutes()
    {
        return Sessions.Sum(x => x.GetDuration().TotalMinutes);
    }

    public int GetMostWins()
    {
        return DecideWinners().MostWins;
    }

    public List<int> GetNightWinnerIds()
    {
        return DecideWinners().WinnerIds;
    }

    private NightWinners DecideWinners()
    {
        return NightWinnerRule.Decide(Sessions
            .SelectMany(x => x.PlayerSessions)
            .Select(x => new NightResult(x.PlayerId, x.Won)));
    }
}
