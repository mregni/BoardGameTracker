using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class ShameService : IShameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IReadRepository<GameNight> _gameNightRepository;
    private readonly IConfigRepository _configRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ShameService> _logger;

    public ShameService(
        IGameRepository gameRepository,
        IReadRepository<GameNight> gameNightRepository,
        IConfigRepository configRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ShameService> logger)
    {
        _gameRepository = gameRepository;
        _gameNightRepository = gameNightRepository;
        _configRepository = configRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<int> CountShelfOfShameGames(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Counting shelf of shame games");
        var configuredMonths = await GetConfiguredMonthsAsync();
        if (configuredMonths == null)
        {
            return 0;
        }

        return await _gameRepository.CountGamesWithNoRecentSessions(CutoffFor(configuredMonths.Value), cancellationToken);
    }

    public async Task<List<ShameGame>> GetShameGames(int? months = null)
    {
        _logger.LogDebug("Getting shame games");
        var effectiveMonths = await GetEffectiveMonthsAsync(months);
        var games = await _gameRepository.GetShameGames(CutoffFor(effectiveMonths));
        var nights = await GetUpcomingGameNightsAsync();
        MarkPlanned(games, nights);
        return games;
    }

    public async Task<ShameStatistics> GetShameStatistics(int? months = null)
    {
        _logger.LogDebug("Getting shame statistics");
        var effectiveMonths = await GetEffectiveMonthsAsync(months);
        var shameGames = await _gameRepository.GetShameGames(CutoffFor(effectiveMonths));
        var nights = await GetUpcomingGameNightsAsync();
        MarkPlanned(shameGames, nights);
        var owned = await _gameRepository.GetOwnedCollectionSummary();

        var totalValue = shameGames
            .Where(g => g.Price.HasValue)
            .Sum(g => g.Price!.Value);

        var gamesWithPrice = shameGames.Where(g => g.Price.HasValue).ToList();
        decimal? averageValue = gamesWithPrice.Count > 0
            ? gamesWithPrice.Average(g => g.Price!.Value)
            : null;

        var neverPlayed = shameGames.Where(g => g.LastSessionDate == null).ToList();

        return new ShameStatistics
        {
            Count = shameGames.Count,
            TotalValue = totalValue > 0 ? totalValue : null,
            AverageValue = averageValue,
            PricedGameCount = gamesWithPrice.Count,
            Months = effectiveMonths,
            OwnedCount = owned.Count,
            CollectionValue = owned.Value,
            NeverPlayedCount = neverPlayed.Count,
            NeverPlayedValue = neverPlayed.Sum(g => g.Price ?? 0m),
            PlannedCount = shameGames.Count(g => g.PlannedGameNightId.HasValue),
            NextGameNight = nights.FirstOrDefault()
        };
    }

    private async Task<int?> GetConfiguredMonthsAsync()
    {
        var enabled = await _configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false);
        if (!enabled)
        {
            return null;
        }

        return await _configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameMonths, Constants.AppConfig.DefaultShelfOfShameMonths);
    }

    private async Task<int> GetEffectiveMonthsAsync(int? requestedMonths)
    {
        var configuredMonths = await GetConfiguredMonthsAsync();
        if (configuredMonths == null)
        {
            throw new FeatureDisabledException(Constants.AppConfig.ShelfOfShameEnabled);
        }

        return Math.Max(requestedMonths ?? configuredMonths.Value, configuredMonths.Value);
    }

    private DateTime CutoffFor(int months)
    {
        return _dateTimeProvider.UtcNow.AddMonths(-months);
    }

    private async Task<List<UpcomingGameNight>> GetUpcomingGameNightsAsync()
    {
        var enabled = await _configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true);
        if (!enabled)
        {
            return [];
        }

        return await _gameNightRepository.ListAsync(new UpcomingGameNightsSpec(_dateTimeProvider.UtcNow));
    }

    private static void MarkPlanned(List<ShameGame> games, List<UpcomingGameNight> nights)
    {
        foreach (var game in games)
        {
            var night = nights.FirstOrDefault(n => n.SuggestedGameIds.Contains(game.Id));
            game.PlannedGameNightId = night?.Id;
            game.PlannedFor = night?.StartDate;
        }
    }
}
