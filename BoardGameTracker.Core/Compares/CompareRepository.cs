using Ardalis.Specification.EntityFrameworkCore;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Compares.Specifications;
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

    public async Task<List<SessionTogether>> GetSessionsTogether(int playerOne, int playerTwo)
    {
        return await _context.Sessions
            .WithSpecification(new SessionsWithBothPlayersSpec(playerOne, playerTwo))
            .OrderByDescending(s => s.Start)
            .ThenByDescending(s => s.Id)
            .Select(s => new SessionTogether
            {
                SessionId = s.Id,
                GameId = s.GameId,
                HasScoring = s.Game.HasScoring,
                Start = s.Start,
                End = s.End,
                PlayerOneScore = s.PlayerSessions
                    .Where(ps => ps.PlayerId == playerOne)
                    .Select(ps => ps.Score)
                    .FirstOrDefault(),
                PlayerOneWon = s.PlayerSessions.Any(ps => ps.PlayerId == playerOne && ps.Won),
                PlayerTwoScore = s.PlayerSessions
                    .Where(ps => ps.PlayerId == playerTwo)
                    .Select(ps => ps.Score)
                    .FirstOrDefault(),
                PlayerTwoWon = s.PlayerSessions.Any(ps => ps.PlayerId == playerTwo && ps.Won),
                OtherWinnerId = s.PlayerSessions
                    .Where(ps => ps.Won && ps.PlayerId != playerOne && ps.PlayerId != playerTwo)
                    .OrderBy(ps => ps.PlayerId)
                    .Select(ps => (int?)ps.PlayerId)
                    .FirstOrDefault(),
                IsCoop = s.IsCoop,
                TeamWon = s.TeamWon
            })
            .ToListAsync();
    }

    public async Task<ComparePairDto?> GetPairWithMostSessions()
    {
        return await (
                from one in _context.PlayerSessions
                join two in _context.PlayerSessions on one.SessionId equals two.SessionId
                where one.PlayerId < two.PlayerId
                group one by new { One = one.PlayerId, Two = two.PlayerId }
                into pair
                orderby pair.Count() descending, pair.Key.One, pair.Key.Two
                select new ComparePairDto
                {
                    PlayerOneId = pair.Key.One,
                    PlayerTwoId = pair.Key.Two,
                    SessionCount = pair.Count()
                })
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
}
