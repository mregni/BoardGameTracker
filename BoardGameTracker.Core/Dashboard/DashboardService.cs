using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Specifications;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Dashboard.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Dashboard;

public class DashboardService : IDashboardService
{
    private const int RecentSessionCount = 5;
    private const int MostPlayedCount = 4;
    private const int TopPlayerCount = 3;
    private const int RecentlyAddedCount = 3;

    private readonly IGameRepository _gameRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IReadRepository<GameNight> _gameNightRepository;
    private readonly IReadRepository<ApplicationUser> _userRepository;
    private readonly IShameService _shameService;
    private readonly IConfigRepository _configRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IGameRepository gameRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        IPlayerRepository playerRepository,
        ISessionRepository sessionRepository,
        IReadRepository<GameNight> gameNightRepository,
        IReadRepository<ApplicationUser> userRepository,
        IShameService shameService,
        IConfigRepository configRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<DashboardService> logger)
    {
        _gameRepository = gameRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _playerRepository = playerRepository;
        _sessionRepository = sessionRepository;
        _gameNightRepository = gameNightRepository;
        _userRepository = userRepository;
        _shameService = shameService;
        _configRepository = configRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<DashboardStatisticsDto> GetStatistics(string? userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calculating dashboard statistics");
        var localNow = _dateTimeProvider.Now;
        var monthStart = _dateTimeProvider.ConvertToUtc(new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified));

        var totalGames = await _gameRepository.CountAsync(cancellationToken);
        var activePlayers = await _playerRepository.CountAsync(cancellationToken);
        var sessionsPlayed = await _sessionRepository.CountAsync(cancellationToken);
        var totalPlayedTime = await _sessionRepository.GetTotalPlayTime(cancellationToken);
        var totalCollectionValue = await _gameStatisticsRepository.GetTotalPayedAsync(cancellationToken);
        var pricedGameCount = await _gameStatisticsRepository.CountPricedOwnedGamesAsync(cancellationToken);
        var expansionsOwned = await _gameRepository.GetTotalExpansionCount(cancellationToken);
        var avgSessionTime = await _sessionRepository.GetMeanPlayTime(cancellationToken);
        var sessionsThisMonth = await _sessionRepository.CountAsync(new SessionsStartedSinceSpec(monthStart), cancellationToken);
        var activePlayersThisMonth = await _playerRepository.CountActivePlayersSince(monthStart, cancellationToken);

        var recentSessions = await _sessionRepository.GetRecentSessions(RecentSessionCount, cancellationToken);
        var gameStates = await _gameStatisticsRepository.GetGameStateCounts(cancellationToken);
        var mostPlayedGames = await _gameStatisticsRepository.GetMostPlayedGames(MostPlayedCount, cancellationToken);
        var mostPlayedThisMonth = await _gameStatisticsRepository.GetMostPlayedGamesSince(MostPlayedCount, monthStart, cancellationToken);
        var topPlayers = await _playerRepository.GetTopWinners(TopPlayerCount, cancellationToken);
        var hasCoopWins = await _sessionRepository.AnyAsync(new WonCoopSessionsSpec(), cancellationToken);
        var recentlyAddedGames = await _gameRepository.GetRecentlyAddedGames(RecentlyAddedCount, cancellationToken);

        var linkedPlayer = await GetLinkedPlayer(userId, monthStart, cancellationToken);
        var (nextGameNight, plannedGameNights) = await GetUpcomingGameNights(_dateTimeProvider.UtcNow, cancellationToken);
        var shelfOfShame = await _shameService.GetShameSummary(cancellationToken);

        return new DashboardStatisticsDto
        {
            TotalGames = totalGames,
            OwnedGames = gameStates.FirstOrDefault(x => x.Type == GameState.Owned)?.GameCount ?? 0,
            ActivePlayers = activePlayers,
            SessionsPlayed = sessionsPlayed,
            TotalPlayedTime = totalPlayedTime,
            TotalCollectionValue = totalCollectionValue,
            PricedGameCount = pricedGameCount,
            ExpansionsOwned = expansionsOwned,
            AvgSessionTime = avgSessionTime,
            SessionsThisMonth = sessionsThisMonth,
            ActivePlayersThisMonth = activePlayersThisMonth,
            LinkedPlayer = linkedPlayer,
            NextGameNight = nextGameNight,
            PlannedGameNights = plannedGameNights,
            ShelfOfShame = shelfOfShame?.ToDashboardDto(),
            RecentActivities = recentSessions.ToRecentActivityListDto(),
            Collection = gameStates,
            MostPlayedGames = mostPlayedGames.ToListDto(),
            MostPlayedGamesThisMonth = mostPlayedThisMonth.ToListDto(),
            TopPlayers = topPlayers.ToListDto(),
            TopPlayersIncludeCoopWins = hasCoopWins,
            RecentAddedGames = recentlyAddedGames.ToRecentAddedGameListDto()
        };
    }

    private async Task<DashboardLinkedPlayerDto?> GetLinkedPlayer(string? userId, DateTime monthStart, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var linkedPlayer = await _userRepository.FirstOrDefaultAsync(new LinkedPlayerByUserIdSpec(userId), cancellationToken);
        if (linkedPlayer == null)
        {
            return null;
        }

        var (playCount, winCount) = await _playerRepository.GetPlayCountsSince(linkedPlayer.Id, monthStart, cancellationToken);
        linkedPlayer.SessionsThisMonth = playCount;
        linkedPlayer.WinsThisMonth = winCount;
        return linkedPlayer;
    }

    private async Task<(DashboardGameNightDto? Next, int Planned)> GetUpcomingGameNights(DateTime now, CancellationToken cancellationToken)
    {
        var enabled = await _configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true);
        if (!enabled)
        {
            return (null, 0);
        }

        var next = await _gameNightRepository.FirstOrDefaultAsync(new NextGameNightSpec(now), cancellationToken);
        if (next == null)
        {
            return (null, 0);
        }

        var planned = await _gameNightRepository.CountAsync(new FutureGameNightsSpec(now), cancellationToken);
        return (next, planned);
    }
}
