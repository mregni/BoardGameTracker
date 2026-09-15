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

    public async Task<decimal?> GetPricePerPlay(int gameId, CancellationToken cancellationToken = default)
    {
        var gameData = await _context.Games
            .AsNoTracking()
            .Where(x => x.Id == gameId)
            .Select(x => new
            {
                BuyingPrice = x.BuyingPrice != null ? (decimal?)x.BuyingPrice.Amount : null,
                SessionCount = x.Sessions.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (gameData == null || gameData.BuyingPrice == null || gameData.SessionCount == 0)
        {
            return null;
        }

        return Math.Round(gameData.BuyingPrice.Value / gameData.SessionCount, 2);
    }

    public Task<double?> GetHighestScore(int gameId, CancellationToken cancellationToken = default)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .MaxAsync(x => x.Score, cancellationToken);
    }

    public async Task<(Player? Player, int WinCount)> GetMostWins(int gameId, CancellationToken cancellationToken = default)
    {
        var playerSession = await GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Won)
            .GroupBy(x => x.PlayerId)
            .Select(x => new { PlayerId = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync(cancellationToken);

        if (playerSession == null)
        {
            return (null, 0);
        }

        var player = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == playerSession.PlayerId, cancellationToken);

        return (player, playerSession.Count);
    }

    public Task<double?> GetAverageScore(int gameId, CancellationToken cancellationToken = default)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .AverageAsync(x => x.Score, cancellationToken);
    }

    public async Task<int?> GetExpansionCount(int gameId, CancellationToken cancellationToken = default)
    {
        var count = await _context.Expansions
            .AsNoTracking()
            .CountAsync(x => x.GameId == gameId, cancellationToken);

        return count > 0 ? count : null;
    }

    public async Task<double> GetAveragePlayTime(int gameId, CancellationToken cancellationToken = default)
    {
        var average = await _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .AverageAsync(x => (double?)(x.End - x.Start).TotalMinutes, cancellationToken);

        return average ?? 0;
    }

    public Task<double> GetTotalPlayedTime(int gameId, CancellationToken cancellationToken = default)
    {
        return _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .SumAsync(x => (x.End - x.Start).TotalMinutes, cancellationToken);
    }

    public async Task<decimal?> GetMeanPayedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .Where(x => x.State == GameState.Owned && x.BuyingPrice != null)
            .AverageAsync(x => (decimal?)x.BuyingPrice!.Amount, cancellationToken);
    }

    public async Task<decimal?> GetTotalPayedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .AsNoTracking()
            .Where(x => x.State == GameState.Owned && x.BuyingPrice != null)
            .SumAsync(x => (decimal?)x.BuyingPrice!.Amount, cancellationToken);
    }

    public Task<List<IGrouping<GameState, Game>>> GetGamesGroupedByState(CancellationToken cancellationToken = default)
    {
        return _context.Games
            .AsNoTracking()
            .GroupBy(x => x.State)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DateTime>> GetSessionStartTimes(int gameId, CancellationToken cancellationToken = default)
    {
        return _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .Select(x => x.Start)
            .ToListAsync(cancellationToken);
    }

    public Task<List<IGrouping<int, int>>> GetPlayerCountChart(int gameId, CancellationToken cancellationToken = default)
    {
        return _context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .Select(x => x.PlayerSessions.Count)
            .GroupBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public Task<PlayerSession?> GetHighestScoringPlayer(int gameId, CancellationToken cancellationToken = default)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Score != null)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PlayerSession?> GetHighestLosingPlayer(int gameId, CancellationToken cancellationToken = default)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => !x.Won && x.Score != null)
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PlayerSession?> GetLowestWinning(int gameId, CancellationToken cancellationToken = default)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Won && x.Score != null)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PlayerSession?> GetLowestScoringPlayer(int gameId, CancellationToken cancellationToken = default)
    {
        return GameSessionsWithPlayerSessions(gameId)
            .SelectMany(x => x.PlayerSessions)
            .Where(x => x.Score != null)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<(int GameId, string Title, string? Image, int PlayCount)>> GetMostPlayedGames(int count, CancellationToken cancellationToken = default)
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
            .ToListAsync(cancellationToken);

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
