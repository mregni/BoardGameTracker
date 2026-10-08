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

    public Task<double> GetTotalPlayTime(CancellationToken cancellationToken = default)
    {
        return _context.Sessions
            .SumAsync(x => (x.End - x.Start).TotalMinutes, cancellationToken);
    }

    public async Task<double> GetMeanPlayTime(CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AverageAsync(x => (double?)(x.End - x.Start).TotalMinutes, cancellationToken) ?? 0;
    }

    public async Task<Dictionary<int, List<Session>>> GetByPlayerBatchAsync(IEnumerable<int> playerIds)
    {
        var playerIdsList = playerIds.ToList();

        var sessions = await _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
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

    public Task<List<Session>> GetRecentSessions(int count, CancellationToken cancellationToken = default)
    {
        return ListAsync(new RecentSessionsSpec(count), cancellationToken);
    }

    public Task<List<DateTime>> GetSessionStartTimes(CancellationToken cancellationToken = default)
    {
        return _context.Sessions
            .AsNoTracking()
            .Select(x => x.Start)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByPlayerIdAsync(int playerId)
    {
        var sessions = await _context.Sessions
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerId))
            .ToListAsync();

        _context.Sessions.RemoveRange(sessions);
    }
}
