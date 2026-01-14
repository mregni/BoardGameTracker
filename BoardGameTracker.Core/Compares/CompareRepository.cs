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
        var player1Wins = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .CountAsync(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne && ps.Won));

        var player2Wins = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .CountAsync(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo && ps.Won));

        return new CompareRow<int>(player1Wins, player2Wins);
    }

    public async Task<CompareRow<MostWonGame?>> GetMostWonGame(int playerOne, int playerTwo)
    {
        var player1Game = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerOne && ps.Won))
            .GroupBy(s => s.GameId)
            .OrderByDescending(g => g.Count())
            .Select(g => new MostWonGame
            {
                GameId = g.Key,
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

        var player2Game = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo && ps.Won))
            .GroupBy(s => s.GameId)
            .OrderByDescending(g => g.Count())
            .Select(g => new MostWonGame
            {
                GameId = g.Key,
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

        return new CompareRow<MostWonGame?>(player1Game, player2Game);
    }

    public async Task<int> GetTotalSessionsTogether(int playerOne, int playerTwo)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .CountAsync();
    }

    public async Task<double> GetMinutesPlayedTogether(int playerOne, int playerTwo)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .SumAsync(s => (s.End - s.Start).TotalMinutes);
    }

    public async Task<PreferredGame?> GetPreferredGame(int playerOne, int playerTwo)
    {
        var result = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .GroupBy(s => s.GameId)
            .OrderByDescending(g => g.Count())
            .Select(g => new { GameId = g.Key, Count = g.Count() })
            .FirstOrDefaultAsync();

        if (result == null)
        {
            return null;
        }

        return new PreferredGame
        {
            GameId = result.GameId,
            SessionCount = result.Count
        };
    }

    public async Task<LastWonGame?> GetLastWonGame(int playerOne, int playerTwo)
    {
        var lastSession = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .Include(s => s.PlayerSessions)
            .OrderByDescending(s => s.Start)
            .FirstOrDefaultAsync();

        if (lastSession == null)
        {
            return null;
        }

        var winningPlayer = lastSession.PlayerSessions
            .FirstOrDefault(ps => (ps.PlayerId == playerOne || ps.PlayerId == playerTwo) && ps.Won);

        if (winningPlayer == null)
        {
            return null;
        }

        return new LastWonGame
        {
            PlayerId = winningPlayer.PlayerId,
            GameId = lastSession.GameId
        };
    }

    public async Task<int?> GetLongestSessionTogether(int playerOne, int playerTwo)
    {
        var longestDuration = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .Select(s => (int?)(s.End - s.Start).TotalMinutes)
            .OrderByDescending(duration => duration)
            .FirstOrDefaultAsync();

        return longestDuration;
    }

    public async Task<FirstGameTogether?> GetFirstGameTogether(int playerOne, int playerTwo)
    {
        var firstSession = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo))
            .OrderBy(s => s.Start)
            .FirstOrDefaultAsync();

        if (firstSession == null)
        {
            return null;
        }

        return new FirstGameTogether
        {
            GameId = firstSession.GameId,
            StartDate = firstSession.Start
        };
    }

    public async Task<ClosestGame?> GetClosestGame(int playerOne, int playerTwo)
    {
        var sessionScores = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.PlayerSessions.Any(ps => ps.PlayerId == playerOne) &&
                        s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo) &&
                        s.Game.HasScoring)
            .Select(s => new
            {
                GameId = s.GameId,
                PlayerOneScore = s.PlayerSessions
                    .Where(ps => ps.PlayerId == playerOne)
                    .Select(ps => ps.Score)
                    .FirstOrDefault(),
                PlayerTwoScore = s.PlayerSessions
                    .Where(ps => ps.PlayerId == playerTwo)
                    .Select(ps => ps.Score)
                    .FirstOrDefault(),
                PlayerOneWon = s.PlayerSessions
                    .Where(ps => ps.PlayerId == playerOne)
                    .Select(ps => ps.Won)
                    .FirstOrDefault(),
                PlayerTwoWon = s.PlayerSessions
                    .Where(ps => ps.PlayerId == playerTwo)
                    .Select(ps => ps.Won)
                    .FirstOrDefault()
            })
            .Where(x => x.PlayerOneScore != null && x.PlayerTwoScore != null)
            .ToListAsync();

        var closestSession = sessionScores
            .Select(x => new
            {
                x.GameId,
                ScoreDifference = Math.Abs(x.PlayerOneScore!.Value - x.PlayerTwoScore!.Value),
                WinnerId = x.PlayerOneWon ? playerOne : (x.PlayerTwoWon ? playerTwo : (int?)null)
            })
            .Where(x => x.WinnerId != null)
            .OrderBy(x => x.ScoreDifference)
            .FirstOrDefault();

        if (closestSession == null)
        {
            return null;
        }

        return new ClosestGame
        {
            PlayerId = closestSession.WinnerId,
            GameId = closestSession.GameId,
            ScoringDifference = closestSession.ScoreDifference
        };
    }
}