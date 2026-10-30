using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Session : HasId
{
    public string Comment { get; private set; }
    public int GameId { get; private set; }
    public bool IsCoop { get; private set; }
    public bool? TeamWon { get; private set; }
    public double? TeamScore { get; private set; }

    private DateTime _start;
    private DateTime _end;

    public DateTime Start
    {
        get => _start;
        private set => _start = value;
    }

    public DateTime End
    {
        get => _end;
        private set
        {
            if (value < _start)
            {
                throw new ArgumentException("End time cannot be before start time.");
            }

            _end = value;
        }
    }

    public Game Game { get; private set; } = null!;
    public ICollection<Expansion> Expansions { get; private set; }
    public int? LocationId { get; private set; }
    public Location? Location { get; private set; }
    public int? GameNightId { get; private set; }
    public GameNight? GameNight { get; private set; }
    public ICollection<PlayerSession> PlayerSessions { get; private set; }

    public Session(int gameId, DateTime start, DateTime end, string comment)
    {
        Guard.Against.NegativeOrZero(gameId);
        Guard.Against.Null(comment);

        GameId = gameId;
        _start = start;
        End = end; // Use property setter for validation
        Comment = comment;
        Expansions = new List<Expansion>();
        PlayerSessions = new List<PlayerSession>();
    }

    public void UpdateComment(string comment)
    {
        Comment = Guard.Against.Null(comment);
    }

    public void UpdateTimes(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new ArgumentException("End time cannot be before start time.");
        }

        _start = start;
        _end = end;
    }

    public void SetLocation(Location? location)
    {
        Location = location;
        LocationId = location?.Id;
    }

    public void SetGameNight(int? gameNightId)
    {
        if (gameNightId.HasValue)
        {
            Guard.Against.NegativeOrZero(gameNightId.Value);
        }

        GameNightId = gameNightId;
    }

    public void AddExpansion(Expansion expansion)
    {
        Guard.Against.Null(expansion);
        if (!Expansions.Contains(expansion))
        {
            Expansions.Add(expansion);
        }
    }

    public void RemoveExpansion(Expansion expansion)
    {
        Guard.Against.Null(expansion);
        Expansions.Remove(expansion);
    }

    public void AddPlayerSession(int playerId, double? score, bool firstPlay, bool won)
    {
        Guard.Against.NegativeOrZero(playerId);

        var playerSession = IsCoop
            ? new PlayerSession(playerId, null, firstPlay, TeamWon == true)
            : new PlayerSession(playerId, score, firstPlay, won);
        if (PlayerSessions.All(ps => ps.PlayerId != playerSession.PlayerId))
        {
            PlayerSessions.Add(playerSession);
        }
    }

    public void RemovePlayerSession(int playerId)
    {
        var playerSession = PlayerSessions.FirstOrDefault(ps => ps.PlayerId == playerId);
        if (playerSession != null)
        {
            PlayerSessions.Remove(playerSession);
        }
    }

    public void MarkAsCoop(bool teamWon, double? teamScore)
    {
        IsCoop = true;
        TeamWon = teamWon;
        TeamScore = teamScore;
        foreach (var playerSession in PlayerSessions)
        {
            playerSession.UpdateScore(null);
            if (teamWon)
            {
                playerSession.MarkAsWinner();
            }
            else
            {
                playerSession.MarkAsLoser();
            }
        }
    }

    public void MarkAsCompetitive()
    {
        IsCoop = false;
        TeamWon = null;
        TeamScore = null;
    }

    public TimeSpan GetDuration() => End - Start;
}