using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Compares;

public class CompareRepository : ICompareRepository
{
    private readonly MainDbContext _context;

    public CompareRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task<CompareRow<int>> GetDirectWins(int playerOne, int playerTwo)
    {
        var sharedSessions = await _context.Sessions
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) && 
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .Include(s => s.PlayerSessions)
            .ToListAsync();

        var player1Wins = sharedSessions.Count(s => 
            s.PlayerSessions.Any(ps => ps.PlayerId == playerOne && ps.Won));
    
        var player2Wins = sharedSessions.Count(s => 
            s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo && ps.Won));

        return new CompareRow<int>(player1Wins, player2Wins);
    }

    public async Task<CompareRow<MostWonGame?>> GetMostWonGame(int playerOne, int playerTwo)
    {
        var sharedSessions = await _context.Sessions
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) && 
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .Include(s => s.PlayerSessions)
            .ToListAsync();

        var player1Game = sharedSessions.Where(s => 
            s.PlayerSessions.Any(ps => ps.PlayerId == playerOne && ps.Won))
            .GroupBy(x => x.GameId)
            .OrderByDescending(x => x.Count())
            .Select(x => new MostWonGame()
            {
                GameId = x.Key,
                Count = x.Count(),
            })
            .FirstOrDefault();
    
        var player2Game = sharedSessions.Where(s => 
            s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo && ps.Won))
            .GroupBy(x => x.GameId)
            .OrderByDescending(x => x.Count())
            .Select(x => new MostWonGame()
            {
                GameId = x.Key,
                Count = x.Count(),
            })
            .FirstOrDefault();
        
        return new CompareRow<MostWonGame?>(player1Game, player2Game);
    }
}