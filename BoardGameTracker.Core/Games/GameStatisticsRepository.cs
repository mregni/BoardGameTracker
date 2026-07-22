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

    public Task<double?> GetHighestScore(int gameId)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .MaxAsync(x => x.Score);
    }

    public async Task<(Player? Player, int WinCount)> GetMostWins(int gameId)
    {
        var playerSession = await GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Won)
            .GroupBy(x => x.PlayerId)
            .Select(x => new { PlayerId = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync();

        if (playerSession == null)
        {
            return (null, 0);
        }

        var player = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == playerSession.PlayerId);

        return (player, playerSession.Count);
    }

    public Task<double?> GetAverageScore(int gameId)
    {
        return GameSessionsWithPlayerSessions(gameId)
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
        var average = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .AverageAsync(x => (double?)(x.End - x.Start).TotalMinutes);

        return average ?? 0;
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
        var result = await GameSessionsWithPlayerSessions(gameId)
            .Include(x => x.Game)
            .Where(x => x.Game.HasScoring)
            .SelectMany(x => x.PlayerSessions)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync();

        return result?.SessionId;
    }

    public async Task<int?> GetLowestScorePlay(int gameId)
    {
        var result = await GameSessionsWithPlayerSessions(gameId)
            .Include(x => x.Game)
            .Where(x => x.Game.HasScoring)
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
            .Select(x => x.PlayerSessions.Count)
            .GroupBy(x => x)
            .ToListAsync();
    }

    public Task<PlayerSession?> GetHighestScoringPlayer(int gameId)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync();
    }

    public Task<PlayerSession?> GetHighestLosingPlayer(int gameId)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => !x.Won)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync();
    }

    public Task<PlayerSession?> GetLowestWinning(int gameId)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Won)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync();
    }

    public Task<PlayerSession?> GetLowestScoringPlayer(int gameId)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync();
    }

    public async Task<List<(int GameId, string Title, string? Image, int PlayCount)>> GetMostPlayedGames(int count)
    {
        var result = await _context.Sessions
            .AsNoTracking()
            .Include(x => x.Game)
            .GroupBy(x => x.GameId)
            .Select(g => new
            {
                GameId = g.Key,
                Title = g.First().Game.Title,
                Image = g.First().Game.Image,
                PlayCount = g.Count()
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(count)
            .ToListAsync();

        return result.Select(x => (x.GameId, x.Title, x.Image, x.PlayCount)).ToList();
    }

    private IQueryable<Session> SessionsWithPlayerSessions()
    {
        return _context.Sessions
            .AsNoTracking()
            .Include(x => x.PlayerSessions);
    }

    private IQueryable<Session> GameSessionsWithPlayerSessions(int gameId)
    {
        return SessionsWithPlayerSessions()
            .Where(x => x.GameId == gameId);
    }
}
