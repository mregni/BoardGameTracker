using System.Net;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
    private const int BggThingBatchSize = 20;
    private readonly IGameRepository _gameRepository;
    private readonly IReadRepository<Session> _sessionRepository;
    private readonly IBoardGameGeekXmlApi2Client _bggClient;
    private readonly ISettingsService _settingsService;
    private readonly IImageService _imageService;
    private readonly IManualService _manualService;
    private readonly IChangeDetectionClient _changeDetectionClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GameService> _logger;

    public GameService(
        IGameRepository gameRepository,
        IReadRepository<Session> sessionRepository,
        IImageService imageService,
        IManualService manualService,
        IBoardGameGeekXmlApi2Client bggClient,
        ISettingsService settingsService,
        IChangeDetectionClient changeDetectionClient,
        IUnitOfWork unitOfWork,
        ILogger<GameService> logger)
    {
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _imageService = imageService;
        _manualService = manualService;
        _bggClient = bggClient;
        _settingsService = settingsService;
        _changeDetectionClient = changeDetectionClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<Game>> GetGames()
    {
        _logger.LogDebug("Fetching all games");
        return _gameRepository.GetGamesOverviewList();
    }

    public Task<Game?> GetGameById(int id)
    {
        _logger.LogDebug("Fetching game {GameId}", id);
        return _gameRepository.SingleOrDefaultAsync(new GameByIdWithDetailsForReadSpec(id));
    }

    public Task<bool> ExistsAsync(int id)
    {
        return _gameRepository.AnyAsync(new GameByIdSpec(id));
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting game {GameId}", id);
        var game = await _gameRepository.GetByIdAsync(id);
        if (game == null)
        {
            throw new EntityNotFoundException(nameof(Game), id);
        }

        var manuals = await _manualService.GetManualsForGame(game.Id);
        await _gameRepository.DeleteAsync(game.Id);
        await _unitOfWork.SaveChangesAsync();

        _imageService.DeleteImage(game.Image);
        _manualService.DeleteManualFiles(manuals);
        _logger.LogInformation("Game {GameId} deleted", id);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return _gameRepository.CountAsync(cancellationToken);
    }

    public async Task<Game> CreateGameFromCommand(CreateGameCommand command)
    {
        _logger.LogDebug("Creating game {Title}", command.Title);
        var game = new Game(command.Title, command.HasScoring, command.State);
        game.UpdateYearPublished(command.YearPublished);
        game.UpdateImage(command.Image);
        game.UpdateShopUrl(command.ShopUrl);
        game.UpdateChangeDetectionWatchId(command.ChangeDetectionWatchId);
        if (game.ChangeDetectionWatchId != null)
        {
            await SyncShopUrlFromWatchAsync(game);
        }

        game.UpdateLanguage(command.Language);
        game.UpdateDescription(command.Description ?? string.Empty);
        game.UpdatePlayerCount(command.MinPlayers, command.MaxPlayers);
        game.UpdatePlayTime(command.MinPlayTime, command.MaxPlayTime);
        game.UpdateMinAge(command.MinAge);
        game.UpdateBggId(command.BggId);
        game.UpdateBuyingPrice(command.BuyingPrice);
        if (command.AdditionDate.HasValue)
        {
            game.UpdateAdditionDate(command.AdditionDate);
        }

        await _gameRepository.CreateAsync(game);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Game {GameId} ({Title}) created", game.Id, game.Title);
        return game;
    }

    public Task<List<Session>> GetSessionsForGame(int id, int? count)
    {
        _logger.LogDebug("Fetching sessions for game {GameId}", id);
        return _sessionRepository.ListAsync(new SessionsByGameSpec(id, count));
    }

    public async Task<Game> UpdateGame(UpdateGameCommand command)
    {
        _logger.LogDebug("Updating game {GameId}", command.Id);
        var game = await _gameRepository.GetByIdAsync(command.Id);
        if (game == null)
        {
            throw new EntityNotFoundException(nameof(Game), command.Id);
        }

        var previousWatchId = game.ChangeDetectionWatchId;
        var previousImage = game.Image;
        game.UpdateTitle(command.Title);
        game.UpdateHasScoring(command.HasScoring);
        game.UpdateState(command.State);
        game.UpdateYearPublished(command.YearPublished);
        game.UpdateImage(command.Image);
        game.UpdateChangeDetectionWatchId(command.ChangeDetectionWatchId);
        if (game.ChangeDetectionWatchId != null && game.ChangeDetectionWatchId != previousWatchId)
        {
            await SyncShopUrlFromWatchAsync(game);
        }

        game.UpdateLanguage(command.Language);
        game.UpdateDescription(command.Description ?? string.Empty);
        game.UpdatePlayerCount(command.MinPlayers, command.MaxPlayers);
        game.UpdatePlayTime(command.MinPlayTime, command.MaxPlayTime);
        game.UpdateMinAge(command.MinAge);
        game.UpdateBggId(command.BggId);
        game.UpdateBuyingPrice(command.BuyingPrice);
        game.UpdateSoldPrice(command.SoldPrice);
        game.UpdateRating(command.Rating);
        game.UpdateWeight(command.Weight);
        if (command.AdditionDate.HasValue)
        {
            game.UpdateAdditionDate(command.AdditionDate);
        }

        await _unitOfWork.SaveChangesAsync();
        if (!string.IsNullOrEmpty(previousImage) && previousImage != game.Image)
        {
            _imageService.DeleteImage(previousImage);
        }

        return game;
    }

    public async Task<ExpansionData[]> SearchExpansionsForGame(int id)
    {
        _logger.LogDebug("Searching expansions for game {GameId}", id);
        await EnsureBggConfiguredAsync();
        var dbGame = await _gameRepository.GetByIdAsync(id);
        if (dbGame is not {BggId: not null})
        {
            return [];
        }

        var request = new ThingRequest([dbGame.BggId.Value]);
        var response = await _bggClient.GetThingAsync(request);
        var firstResult = response.Result?.FirstOrDefault();
        if (!response.Succeeded || firstResult == null)
        {
            return [];
        }

        return (firstResult.Links ?? [])
            .Where(l => l.Type == Constants.Bgg.Expansion && !string.IsNullOrWhiteSpace(l.Value))
            .Select(l => new ExpansionData { Title = l.Value, BggId = l.Id })
            .ToArray();
    }

    public async Task<List<Expansion>> UpdateGameExpansions(int gameId, int[] expansionIds)
    {
        ArgumentNullException.ThrowIfNull(expansionIds);
        await EnsureBggConfiguredAsync();
        _logger.LogDebug("Updating expansions for game {GameId}", gameId);
        var game = await _gameRepository.SingleOrDefaultAsync(new GameWithExpansionsSpec(gameId));
        if (game == null)
        {
            throw new EntityNotFoundException(nameof(Game), gameId);
        }

        var expansionsToRemove = game.Expansions.Where(x => x.BggId != null && !expansionIds.Contains(x.BggId.Value)).ToList();
        foreach (var expansion in expansionsToRemove)
        {
            game.RemoveExpansion(expansion);
        }

        var newExpansionsIds = expansionIds
            .Where(x => !game.Expansions.Select(y => y.BggId).Contains(x))
            .Distinct()
            .ToList();

        foreach (var item in await FetchExpansionsFromBgg(game.Id, newExpansionsIds))
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.Id <= 0)
            {
                _logger.LogWarning("Skipping malformed BGG expansion (id {BggId}) for game {GameId}", item.Id, game.Id);
                continue;
            }

            game.AddExpansion(new Expansion(item.Name, item.Id, game.Id));
        }

        await _unitOfWork.SaveChangesAsync();
        return game.Expansions.ToList();
    }

    public async Task<Expansion> AddManualExpansion(int gameId, string title)
    {
        _logger.LogDebug("Adding manual expansion {Title} to game {GameId}", title, gameId);
        var game = await _gameRepository.SingleOrDefaultAsync(new GameWithExpansionsSpec(gameId));
        if (game == null)
        {
            throw new EntityNotFoundException(nameof(Game), gameId);
        }

        var expansion = new Expansion(title.Trim(), null, game.Id);
        if (game.Expansions.Any(x => x.Matches(expansion)))
        {
            throw new DomainException(BoardGameTracker.Common.Constants.Errors.ExpansionAlreadyExists);
        }

        game.AddExpansion(expansion);
        await _unitOfWork.SaveChangesAsync();
        return expansion;
    }

    private async Task<List<ThingResponse.Item>> FetchExpansionsFromBgg(int gameId, List<int> bggIds)
    {
        var items = new List<ThingResponse.Item>();
        foreach (var chunk in bggIds.Chunk(BggThingBatchSize))
        {
            try
            {
                var response = await _bggClient.GetThingAsync(new ThingRequest(chunk, types: ["boardgameexpansion"]));
                if (response.Result != null)
                {
                    items.AddRange(response.Result);
                }
            }
            catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(ex, "BGG API key is invalid or expired");
                throw new ValidationException("Invalid BGG API key. Please check your API key in settings.");
            }
            catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(ex, "BGG rate-limited the expansion request for game {GameId}", gameId);
                throw new BggRateLimitException();
            }
        }

        return items;
    }

    public async Task<List<Expansion>> GetGameExpansions(int gameId, List<int> expansionIds)
    {
        var expansions = await _gameRepository.GetExpansions(gameId, expansionIds);
        if (expansionIds.Except(expansions.Select(x => x.Id)).Any())
        {
            throw new ValidationException(BoardGameTracker.Common.Constants.Errors.InvalidExpansion);
        }

        return expansions;
    }

    public async Task DeleteExpansion(int gameId, int expansionId)
    {
        _logger.LogDebug("Deleting expansion {ExpansionId} from game {GameId}", expansionId, gameId);
        if (!await _gameRepository.DeleteExpansion(gameId, expansionId))
        {
            throw new EntityNotFoundException(nameof(Expansion), expansionId);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<GamePriceDto?> GetGamePriceAsync(
        int gameId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching price for game {GameId}", gameId);
        var watchInfo = await _gameRepository.GetWatchInfo(gameId);
        if (watchInfo == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(watchInfo.WatchId))
        {
            return new GamePriceDto
            {
                GameId = watchInfo.Id,
                Available = false,
                Status = ChangeDetectionStatus.NotConfigured
            };
        }

        var result = await _changeDetectionClient.GetLatestAsync(watchInfo.WatchId, forceRefresh, cancellationToken);
        return MapPrice(watchInfo.Id, watchInfo.WatchId, result);
    }

    public async Task<List<GamePriceDto>> GetTrackedPricesAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching prices for tracked games");
        var games = await _gameRepository.GetTrackedGames();

        var watchIds = games.Select(game => game.ChangeDetectionWatchId!).ToList();
        var results = await _changeDetectionClient.GetLatestAsync(watchIds, forceRefresh, cancellationToken);

        return games
            .Select(game =>
            {
                results.TryGetValue(game.ChangeDetectionWatchId!, out var result);
                return MapPrice(game.Id, game.ChangeDetectionWatchId,
                    result ?? ChangeDetectionResult.Unavailable(ChangeDetectionStatus.Unreachable));
            })
            .ToList();
    }

    private static GamePriceDto MapPrice(int gameId, string? watchId, ChangeDetectionResult result)
    {
        return new GamePriceDto
        {
            GameId = gameId,
            WatchId = watchId,
            Available = result.Available,
            Status = result.Status,
            InStock = result.InStock,
            Price = result.Price,
            Currency = result.Currency,
            CheckedAt = result.CheckedAt,
            ShopUrl = result.SourceUrl,
            RecheckQueued = result.RecheckQueued,
            FetchedAt = result.FetchedAt
        };
    }

    public async Task<Game> CreateWatchForGame(int gameId, string url, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating changedetection.io watch for game {GameId}", gameId);
        var game = await _gameRepository.GetByIdAsync(gameId)
            ?? throw new EntityNotFoundException(nameof(Game), gameId);

        var shopUrl = (url ?? string.Empty).Trim();
        if (!IsHttpUrl(shopUrl))
        {
            throw new ValidationException(Constants.Errors.InvalidShopUrl);
        }

        var (status, watchId) = await _changeDetectionClient.CreateWatchAsync(shopUrl, game.Title, cancellationToken);
        if (status != ChangeDetectionStatus.Ok || watchId == null)
        {
            throw new DomainException(status == ChangeDetectionStatus.NotConfigured
                ? Constants.Errors.ChangeDetectionNotConfigured
                : Constants.Errors.ChangeDetectionCreateWatchFailed);
        }

        game.UpdateChangeDetectionWatchId(watchId);
        game.UpdateShopUrl(shopUrl);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Game {GameId} linked to changedetection.io watch {WatchId}", gameId, watchId);
        return game;
    }

    private async Task SyncShopUrlFromWatchAsync(Game game)
    {
        var (status, info) = await _changeDetectionClient.GetWatchInfoAsync(game.ChangeDetectionWatchId!);
        if (status == ChangeDetectionStatus.WatchNotFound)
        {
            throw new ValidationException(Constants.Errors.ChangeDetectionWatchNotFound);
        }

        if (status == ChangeDetectionStatus.Ok && info != null && IsHttpUrl(info.Url))
        {
            game.UpdateShopUrl(info.Url);
        }
    }

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private async Task EnsureBggConfiguredAsync()
    {
        var apiKey = await _settingsService.GetBggApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new BggFeatureDisabledException();
        }
    }
}
