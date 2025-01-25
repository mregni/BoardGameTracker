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
}