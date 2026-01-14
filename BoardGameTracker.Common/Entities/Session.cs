using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Session : HasId
{
    public string Comment { get; private set; } = string.Empty;
    public int GameId { get; private set; }

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
                throw new ArgumentException("End time cannot be before start time.");
            _end = value;
        }
    }

    public Game Game { get; private set; } = null!;
    public ICollection<Expansion> Expansions { get; private set; }
    public int? LocationId { get; private set; }
    public Location? Location { get; private set; }
    public ICollection<Image> ExtraImages { get; private set; }
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
        ExtraImages = new List<Image>();
        PlayerSessions = new List<PlayerSession>();
    }

    public void UpdateComment(string comment)
    {
        Comment = Guard.Against.Null(comment);
    }

    public void UpdateTimes(DateTime start, DateTime end)
    {
        if (end < start)
            throw new ArgumentException("End time cannot be before start time.");

        _start = start;
        _end = end;
    }

    public void SetLocation(Location? location)
    {
        Location = location;
        LocationId = location?.Id;
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
        Guard.Against.Null(playerId);
        Guard.Against.Null(firstPlay);
        Guard.Against.Null(won);

        var playerSession = new PlayerSession(playerId, score, firstPlay, won);
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

    public void AddImage(Image image)
    {
        Guard.Against.Null(image);
        ExtraImages.Add(image);
    }

    public TimeSpan GetDuration() => End - Start;

    public int GetPlayerCount() => PlayerSessions.Count;

    public Player? GetWinner()
    {
        return PlayerSessions
            .FirstOrDefault(ps => ps.Won)
            ?.Player;
    }

    public IEnumerable<Player> GetPlayers()
    {
        return PlayerSessions.Select(ps => ps.Player);
    }

    public bool HasFirstTimePlayers() => PlayerSessions.Any(ps => ps.FirstPlay);

    public double? GetHighestScore()
    {
        if (!Game.HasScoring)
            return null;

        return PlayerSessions
            .Where(ps => ps.Score.HasValue)
            .Max(ps => ps.Score);
    }

    public double? GetLowestScore()
    {
        if (!Game.HasScoring)
            return null;

        return PlayerSessions
            .Where(ps => ps.Score.HasValue)
            .Min(ps => ps.Score);
    }

    public double? GetAverageScore()
    {
        if (!Game.HasScoring)
            return null;

        var scores = PlayerSessions
            .Where(ps => ps.Score.HasValue)
            .Select(ps => ps.Score!.Value)
            .ToList();

        return scores.Any() ? scores.Average() : null;
    }
}