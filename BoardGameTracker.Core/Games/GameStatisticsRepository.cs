using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameStatisticsRepository : IGameStatisticsRepository
{
    private readonly MainDbContext _context;

    public GameStatisticsRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task<double?> GetPricePerPlay(int gameId)
    {
        var gameData = await _context.Games
            .AsNoTracking()
            .Where(x => x.Id == gameId)
            .Select(x => new
            {
                BuyingPrice = x.BuyingPrice != null ? (double?)x.BuyingPrice.Amount : null,
                SessionCount = x.Sessions.Count
            })
            .FirstOrDefaultAsync();

        if (gameData == null || gameData.BuyingPrice == null || gameData.SessionCount == 0)
        {
            return null;
        }

        return Math.Round(gameData.BuyingPrice.Value / gameData.SessionCount, 2);
    }

    public async Task<double?> GetHighestScore(int gameId)
    {
        var hasAnySessions = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .AnyAsync();

        if (!hasAnySessions)
        {
            return null;
        }

        return await _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId)
            .SelectMany(x => x.PlayerSessions)
            .MaxAsync(x => x.Score);
    }

    public Task<Player?> GetMostWins(int gameId)
    {
        return GetMostWinsInternal(gameId);
    }

    public Task<Player?> GetMostWins()
    {
        return GetMostWinsInternal(null);
    }

    private async Task<Player?> GetMostWinsInternal(int? gameId)
    {
        var sessionsQuery = _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .AsQueryable();

        if (gameId.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(x => x.GameId == gameId.Value);
        }

        var playerSession = await sessionsQuery
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Won)
            .GroupBy(x => x.PlayerId)
            .Select(x => new { PlayerId = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync();

        if (playerSession == null)
        {
            return null;
        }

        return await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == playerSession.PlayerId);
    }

    public async Task<double?> GetAverageScore(int gameId)
    {
        var hasAnySessions = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .AnyAsync();

        if (!hasAnySessions)
        {
            return null;
        }

        return await _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId)
            .SelectMany(x => x.PlayerSessions)
            .AverageAsync(x => x.Score);
    }

    public async Task<int?> GetExpansionCount(int gameId)
    {
        var count = await _context.Expansions
            .AsNoTracking()
            .CountAsync(x => x.GameId == gameId);

        return count > 0 ? count : null;
    }

    public async Task<double> GetAveragePlayTime(int gameId)
    {
        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .ToListAsync();

        if (!sessions.Any())
        {
            return 0;
        }

        return sessions.Average(x => (x.End - x.Start).TotalMinutes);
    }

    public async Task<double?> GetMeanPayedAsync()
    {
        var count = await _context.Games
            .AsNoTracking()
            .CountAsync(x => x.BuyingPrice != null);
        if (count == 0)
            return null;

        return await _context.Games
            .AsNoTracking()
            .Where(x => x.BuyingPrice != null)
            .AverageAsync(x => (double?)x.BuyingPrice!.Amount);
    }

    public async Task<double?> GetTotalPayedAsync()
    {
        return await _context.Games
            .AsNoTracking()
            .Where(x => x.BuyingPrice != null)
            .SumAsync(x => (double?)x.BuyingPrice!.Amount);
    }

    public Task<List<IGrouping<GameState, Game>>> GetGamesGroupedByState()
    {
        return _context.Games
            .AsNoTracking()
            .GroupBy(x => x.State)
            .ToListAsync();
    }

    public async Task<int?> GetHighScorePlay(int gameId)
    {
        var result = await _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Include(x => x.Game)
            .Where(x => x.GameId == gameId && x.Game.HasScoring)
            .SelectMany(x => x.PlayerSessions)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync();

        return result?.SessionId;
    }

    public async Task<int?> GetLowestScorePlay(int gameId)
    {
        var result = await _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Include(x => x.Game)
            .Where(x => x.GameId == gameId && x.Game.HasScoring)
            .SelectMany(x => x.PlayerSessions)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync();

        return result?.SessionId;
    }

    public Task<List<IGrouping<DayOfWeek, Session>>> GetPlayByDayChart(int gameId)
    {
        return _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .GroupBy(x => x.Start.DayOfWeek)
            .ToListAsync();
    }

    public Task<List<IGrouping<int, int>>> GetPlayerCountChart(int gameId)
    {
        return _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .Select(x => x.PlayerSessions.Count())
            .GroupBy(x => x)
            .ToListAsync();
    }

    public Task<PlayerSession?> GetHighestScoringPlayer(int gameId)
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId)
            .SelectMany(x => x.PlayerSessions)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync();
    }

    public Task<PlayerSession?> GetHighestLosingPlayer(int gameId)
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => !x.Won)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync();
    }

    public Task<PlayerSession?> GetLowestWinning(int gameId)
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Won)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync();
    }

    public Task<PlayerSession?> GetLowestScoringPlayer(int gameId)
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions)
            .Where(x => x.GameId == gameId)
            .SelectMany(x => x.PlayerSessions)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync();
    }
}
