using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameSessionRepository : IGameSessionRepository
{
    private readonly MainDbContext _context;

    public GameSessionRepository(MainDbContext context)
    {
        _context = context;
    }

    public Task<List<Session>> GetSessions(int gameId, int skip, int? take)
    {
        var query = _context.Sessions
            .AsNoTracking()
            .Include(x => x.Location)
            .Include(x => x.PlayerSessions)
            .ThenInclude(x => x.Player)
            .Where(x => x.GameId == gameId)
            .OrderByDescending(x => x.Start)
            .Skip(skip);

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return query.ToListAsync();
    }

    public Task<List<Session>> GetSessions(int gameId, int dayCount)
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId && x.Start > DateTime.UtcNow.AddDays(dayCount))
            .OrderBy(x => x.Start)
            .ToListAsync();
    }

    public Task<List<Session>> GetSessionsByGameId(int gameId, int? count)
    {
        var query = _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId)
            .OrderByDescending(x => x.Start)
            .AsQueryable();

        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }

        return query.ToListAsync();
    }
    
    public Task<List<Session>> GetSessionsByPlayerId(int playerId, int? count)
    {
        var query = _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId))
            .OrderByDescending(x => x.Start)
            .AsQueryable();

        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }

        return query.ToListAsync();
    }

    public Task<int> GetPlayCount(int gameId)
    {
        return _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .CountAsync();
    }

    public async Task<double> GetTotalPlayedTime(int gameId)
    {
        var totalDurationInMinutes = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .SumAsync(session => (session.End - session.Start).TotalMinutes);

        return totalDurationInMinutes;
    }

    public async Task<DateTime?> GetLastPlayedDateTime(int gameId)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderByDescending(x => x.Start)
            .Select(x => (DateTime?)x.Start)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetShortestPlay(int gameId)
    {
        var result = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderBy(x => (x.End - x.Start).TotalSeconds)
            .FirstOrDefaultAsync();

        return result?.Id;
    }

    public async Task<int?> GetLongestPlay(int gameId)
    {
        var result = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .OrderByDescending(x => (x.End - x.Start).TotalSeconds)
            .FirstOrDefaultAsync();

        return result?.Id;
    }
}