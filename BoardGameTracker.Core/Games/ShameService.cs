using BoardGameTracker.Common;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class ShameService : IShameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IConfigRepository _configRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ShameService> _logger;

    public ShameService(
        IGameRepository gameRepository,
        IConfigRepository configRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ShameService> logger)
    {
        _gameRepository = gameRepository;
        _configRepository = configRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<int> CountShelfOfShameGames(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting shelf of shame games");
        var cutoffDate = await GetCutoffDateAsync();
        if (cutoffDate == null)
        {
            return 0;
        }

        return await _gameRepository.CountGamesWithNoRecentSessions(cutoffDate.Value, cancellationToken);
    }

    public async Task<List<ShameGame>> GetShameGames()
    {
        _logger.LogDebug("Getting shame games");
        var cutoffDate = await GetCutoffDateAsync();
        if (cutoffDate == null)
        {
            return [];
        }

        return await _gameRepository.GetShameGames(cutoffDate.Value);
    }

    private async Task<DateTime?> GetCutoffDateAsync()
    {
        var enabled = await _configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false);
        if (!enabled)
        {
            return null;
        }

        var months = await _configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameMonths, Constants.AppConfig.DefaultShelfOfShameMonths);
        return _dateTimeProvider.UtcNow.AddMonths(-months);
    }

    public async Task<ShameStatistics> GetShameStatistics()
    {
        _logger.LogDebug("Getting shame statistics");
        var shameGames = await GetShameGames();

        var count = shameGames.Count;
        var totalValue = shameGames
            .Where(g => g.Price.HasValue)
            .Sum(g => g.Price!.Value);

        var gamesWithPrice = shameGames.Where(g => g.Price.HasValue).ToList();
        decimal? averageValue = gamesWithPrice.Count > 0
            ? gamesWithPrice.Average(g => g.Price!.Value)
            : null;

        return new ShameStatistics
        {
            Count = count,
            TotalValue = totalValue > 0 ? totalValue : null,
            AverageValue = averageValue
        };
    }
}
