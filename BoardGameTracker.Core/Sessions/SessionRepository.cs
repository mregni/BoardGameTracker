using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Sessions;

public class SessionRepository : EfRepository<Session>, ISessionRepository
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
        return CountAsync(new SessionsByPlayerSpec(playerId));
    }

    public Task<int> CountByPlayerAndGame(int playerId, int gameId)
    {
        return CountAsync(new SessionsByPlayerAndGameSpec(playerId, gameId));
    }

    public Task<List<Session>> GetByPlayer(int playerId, bool? won = null)
    {
        return ListAsync(new SessionsByPlayerSpec(playerId, won));
    }

    public Task<List<Session>> GetByPlayerAndGame(int playerId, int gameId)
    {
        return ListAsync(new SessionsByPlayerAndGameSpec(playerId, gameId));
    }

    public async Task<double> GetTotalPlayTime()
    {
        if (!await _context.Sessions.AnyAsync())
        {
            return 0;
        }

        return await _context.Sessions
            .SumAsync(x => (x.End - x.Start).TotalMinutes);
    }

    public async Task<double> GetMeanPlayTime()
    {
        if (!await _context.Sessions.AnyAsync())
        {
            return 0;
        }

        return await _context.Sessions
            .AverageAsync(x => (x.End - x.Start).TotalMinutes);
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
        return FirstOrDefaultAsync(new SessionByIdWithDetailsSpec(id));
    }

    public Task<List<Session>> GetRecentSessions(int count)
    {
        return ListAsync(new RecentSessionsSpec(count));
    }

    public Task<List<IGrouping<DayOfWeek, Session>>> GetSessionsByDayOfWeek()
    {
        return _context.Sessions
            .AsNoTracking()
            .GroupBy(x => x.Start.DayOfWeek)
            .ToListAsync();
    }

    public async Task DeleteByPlayerIdAsync(int playerId)
    {
        var sessions = await _context.Sessions
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerId))
            .ToListAsync();

        _context.Sessions.RemoveRange(sessions);
    }

    public override async Task<Session> Update(Session entity)
    {
        var existing = await _context.Sessions
            .Include(s => s.PlayerSessions)
            .Include(s => s.Expansions)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.Id == entity.Id);

        Guard.Against.Null(existing);

        existing.UpdateTimes(entity.Start, entity.End);
        existing.UpdateComment(entity.Comment);

        await UpdateLocationAsync(existing, entity.LocationId);
        SyncPlayerSessions(existing, entity);
        await SyncExpansionsAsync(existing, entity);

        return existing;
    }

    private async Task UpdateLocationAsync(Session existing, int? newLocationId)
    {
        if (newLocationId == existing.LocationId)
            return;

        if (newLocationId.HasValue)
        {
            var location = await _context.Locations.FindAsync(newLocationId.Value);
            existing.SetLocation(location);
        }
        else
        {
            existing.SetLocation(null);
        }
    }

    private static void SyncPlayerSessions(Session existing, Session entity)
    {
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
    }

    private async Task SyncExpansionsAsync(Session existing, Session entity)
    {
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
    }
}