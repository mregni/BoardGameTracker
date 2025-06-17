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

    public Task<List<Session>> GetByPlayer(int playerId, bool? won = null)
    {
        var query = _context.Sessions
            .Where(x => x.PlayerSessions.Any(y => y.PlayerId == playerId));

        if (won.HasValue)
        {
            query = query.Where(x => x.PlayerSessions.Any(y => y.Won == won));
        }

        return query.ToListAsync();
    }

    public Task<double> GetTotalPlayTime()
    {
        return _context.Sessions.Select(x => (x.End - x.Start).TotalMinutes)
            .DefaultIfEmpty()
            .SumAsync();
    }

    public Task<double> GetMeanPlayTime()
    {
        return _context.Sessions.Select(x => (x.End - x.Start).TotalMinutes)
            .DefaultIfEmpty()
            .AverageAsync();
    }


    public override Task<Session?> GetByIdAsync(int id)
    {
        return _context.Sessions
            .Include(x => x.PlayerSessions)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<Session> UpdateAsync(Session entity)
    {
        await _context.PlayerSessions
            .Where(x => x.SessionId == entity.Id)
            .ExecuteDeleteAsync();
    
        _context.Update(entity);
        await _context.SaveChangesAsync();

        return entity;
    }
}