using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Plays.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Plays;

public class PlayRepository : IPlayRepository
{
    private readonly MainDbContext _context;

    public PlayRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task<Play> Create(Play play)
    {
        await _context.Plays.AddAsync(play);
        await _context.SaveChangesAsync();
        return play;
    }

    public async Task Delete(int id)
    {
        var play = await _context.Plays.FirstOrDefaultAsync(x => x.Id == id);
        if (play == null)
        {
            return;
        }
        
        _context.Remove(play);
        await _context.SaveChangesAsync();
    }

    public async Task<Play> Update(Play play)
    {
        var dbPlay = await _context.Plays
            .Include(x => x.Players)
            .SingleOrDefaultAsync(x => x.Id == play.Id);
        if (dbPlay != null)
        {
            dbPlay.Players = play.Players;
            dbPlay.Comment = play.Comment;
            dbPlay.End = play.End;
            dbPlay.Start = play.Start;
            dbPlay.LocationId = play.LocationId;
            await _context.SaveChangesAsync();
        }

        return play;
    }
}