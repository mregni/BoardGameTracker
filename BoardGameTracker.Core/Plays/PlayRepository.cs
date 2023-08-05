using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Plays.Interfaces;

namespace BoardGameTracker.Core.Plays;

public class PlayRepository : IPlayRepository
{
    private readonly MainDbContext _context;

    public PlayRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task CreatePlay(Play play)
    {
        await _context.Plays.AddAsync(play);
        await _context.SaveChangesAsync();
    }
}