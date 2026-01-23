using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Sessions;

public class SessionRepository : CrudHelper<Session>, ISessionRepository
{
    private readonly MainDbContext _context;

    public SessionRepository(MainDbContext context): base(context)
    {
        _context = context;
    }
    
    public Task<int> CountAsync()
    {
        return _context.Sessions.CountAsync();
    }
    
    public Task<int> CountByPlayer(int playerId)
    {
        return _context.Sessions
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .CountAsync();
    }

    public Task<int> CountByPlayerAndGame(int playerId, int gameId)
    {
        return _context.Sessions
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .Where(x => x.GameId == gameId)
            .CountAsync();
    }

    public Task<List<Session>> GetByPlayer(int playerId, bool? won = null)
    {
        var query = _context.Sessions
            .Include(x => x.PlayerSessions)
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId));

        if (won.HasValue)
        {
            query = query.Where(x => x.PlayerSessions.Any(y => y.Won == won));
        }

        return query.ToListAsync();
    }

    public Task<List<Session>> GetByPlayerAndGame(int playerId, int gameId)
    {
        return _context.Sessions
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .Where(x => x.GameId == gameId)
            .ToListAsync();
    }

    public async Task<double> GetTotalPlayTime()
    {
        var sessions = await _context.Sessions
            .AsNoTracking()
            .ToListAsync();

        if (!sessions.Any())
        {
            return 0;
        }

        return sessions.Sum(x => (x.End - x.Start).TotalMinutes);
    }

    public async Task<double> GetMeanPlayTime()
    {
        var sessions = await _context.Sessions
            .AsNoTracking()
            .ToListAsync();

        if (!sessions.Any())
        {
            return 0;
        }

        return sessions.Average(x => (x.End - x.Start).TotalMinutes);
    }

    public async Task<Dictionary<int, List<Session>>> GetByPlayerBatchAsync(IEnumerable<int> playerIds)
    {
        var playerIdsList = playerIds.ToList();

        var sessions = await _context.Sessions
            .Include(x => x.PlayerSessions)
            .Include(x => x.Game)
            .Include(x => x.Expansions)
            .Where(s => s.PlayerSessions.Any(ps => playerIdsList.Contains(ps.PlayerId)))
            .Select(s => new
            {
                Session = s,
                PlayerIds = s.PlayerSessions.Where(ps => playerIdsList.Contains(ps.PlayerId)).Select(ps => ps.PlayerId)
            })
            .ToListAsync();

        var result = playerIdsList.ToDictionary(id => id, _ => new List<Session>());

        foreach (var item in sessions)
        {
            foreach (var playerId in item.PlayerIds)
            {
                result[playerId].Add(item.Session);
            }
        }

        return result;
    }

    public override Task<Session?> GetByIdAsync(int id)
    {
        return _context.Sessions
            .Include(x => x.PlayerSessions)
            .Include(x => x.Expansions)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<Session>> GetRecentSessions(int count)
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(x => x.Game)
            .Include(x => x.PlayerSessions)
                .ThenInclude(ps => ps.Player)
            .OrderByDescending(x => x.Start)
            .Take(count)
            .ToListAsync();
    }

    public Task<List<IGrouping<DayOfWeek, Session>>> GetSessionsByDayOfWeek()
    {
        return _context.Sessions
            .AsNoTracking()
            .GroupBy(x => x.Start.DayOfWeek)
            .ToListAsync();
    }

    public override async Task<Session> UpdateAsync(Session entity)
    {
        var existing = await _context.Sessions
            .Include(s => s.PlayerSessions)
            .Include(s => s.Expansions)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.Id == entity.Id);

        Guard.Against.Null(existing);

        existing.UpdateTimes(entity.Start, entity.End);
        existing.UpdateComment(entity.Comment);

        if (entity.LocationId != existing.LocationId)
        {
            if (entity.LocationId.HasValue)
            {
                var location = await _context.Locations.FindAsync(entity.LocationId.Value);
                existing.SetLocation(location);
            }
            else
            {
                existing.SetLocation(null);
            }
        }

        var existingPlayerIds = existing.PlayerSessions.Select(ps => ps.PlayerId).ToList();
        var newPlayerIds = entity.PlayerSessions.Select(ps => ps.PlayerId).ToList();

        var playersToRemove = existingPlayerIds.Except(newPlayerIds).ToList();
        foreach (var playerId in playersToRemove)
        {
            existing.RemovePlayerSession(playerId);
        }

        foreach (var playerSession in entity.PlayerSessions)
        {
            var existingPs = existing.PlayerSessions
                .FirstOrDefault(ps => ps.PlayerId == playerSession.PlayerId);

            if (existingPs == null)
            {
                existing.AddPlayerSession(
                    playerSession.PlayerId,
                    playerSession.Score,
                    playerSession.FirstPlay,
                    playerSession.Won);
            }
            else
            {
                existingPs.UpdateScore(playerSession.Score);
                if (playerSession.FirstPlay)
                    existingPs.MarkAsFirstPlay();
                if (playerSession.Won)
                    existingPs.MarkAsWinner();
                else
                    existingPs.MarkAsLoser();
            }
        }

        var existingExpansionIds = existing.Expansions.Select(e => e.Id).ToList();
        var newExpansionIds = entity.Expansions.Select(e => e.Id).ToList();

        var expansionsToRemove = existing.Expansions
            .Where(e => !newExpansionIds.Contains(e.Id))
            .ToList();
        foreach (var expansion in expansionsToRemove)
        {
            existing.RemoveExpansion(expansion);
        }

        var expansionsToAdd = entity.Expansions
            .Where(e => !existingExpansionIds.Contains(e.Id))
            .ToList();
        foreach (var expansion in expansionsToAdd)
        {
            var trackedExpansion = await _context.Expansions.FindAsync(expansion.Id);
            if (trackedExpansion != null)
            {
                existing.AddExpansion(trackedExpansion);
            }
        }

        return existing;
    }
}