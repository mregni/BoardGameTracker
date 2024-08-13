using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Sessions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Sessions;

public class SessionRepository : ISessionRepository
{
    private readonly MainDbContext _context;

    public SessionRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task<Session> Create(Session session)
    {
        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task Delete(int id)
    {
        var play = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == id);
        if (play == null)
        {
            return;
        }
        
        _context.Remove(play);
        await _context.SaveChangesAsync();
    }

    public async Task<Session> Update(Session session)
    {
        var dbPlay = await _context.Sessions
            .Include(x => x.PlayerSessions)
            .SingleOrDefaultAsync(x => x.Id == session.Id);
        if (dbPlay != null)
        {
            dbPlay.PlayerSessions = session.PlayerSessions;
            dbPlay.Comment = session.Comment;
            dbPlay.End = session.End;
            dbPlay.Start = session.Start;
            dbPlay.LocationId = session.LocationId;
            await _context.SaveChangesAsync();
        }

        return session;
    }
    
    public Task<int> CountAsync()
    {
        return _context.Sessions.CountAsync();
    }

    public Task<double> GetTotalPlayTime()
    {
        return _context.Sessions.SumAsync(x => (x.End - x.Start).TotalMinutes);
    }

    public Task<double> GetMeanPlayTime()
    {
        return _context.Sessions.AverageAsync(x => (x.End - x.Start).TotalMinutes);
    }
}