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
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
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

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting game {GameId}", id);
        var game = await _gameRepository.GetByIdAsync(id);
        if (game == null)
        {
            throw new EntityNotFoundException(nameof(Game), id);
        }

        _imageService.DeleteImage(game.Image);
        await _manualService.DeleteManualFilesForGame(game.Id);
        await _gameRepository.DeleteAsync(game.Id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Game {GameId} deleted", id);
    }

    public Task<int> CountAsync()
    {
        return _gameRepository.CountAsync();
    }

    public async Task<Game> CreateGameFromCommand(CreateGameCommand command)
    {
        _logger.LogDebug("Creating game {Title}", command.Title);
        var game = new Game(command.Title, command.HasScoring, command.State);
        game.UpdateYearPublished(command.YearPublished);
        game.UpdateImage(command.Image);
        game.UpdateShopUrl(command.ShopUrl);
        game.UpdateChangeDetectionWatchId(command.ChangeDetectionWatchId);
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

        game.UpdateTitle(command.Title);
        game.UpdateHasScoring(command.HasScoring);
        game.UpdateState(command.State);
        game.UpdateYearPublished(command.YearPublished);
        game.UpdateImage(command.Image);
        game.UpdateShopUrl(command.ShopUrl);
        game.UpdateChangeDetectionWatchId(command.ChangeDetectionWatchId);
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
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
        {
            return [];
        }

        var expansionsToRemove = game.Expansions.Where(x => !expansionIds.Contains(x.BggId)).ToList();
        foreach (var expansion in expansionsToRemove)
        {
            game.RemoveExpansion(expansion.BggId);
        }

        var newExpansionsIds = expansionIds
            .Where(x => !game.Expansions.Select(y => y.BggId).Contains(x))
            .ToList();

        var expansionRequests = newExpansionsIds.Select(async expansionId =>
        {
            var request = new ThingRequest([expansionId], types: ["boardgameexpansion"]);
            var response = await _bggClient.GetThingAsync(request);
            return response.Result?.FirstOrDefault();
        });

        var expansionResults = await Task.WhenAll(expansionRequests);

        foreach (var firstResult in expansionResults)
        {
            if (firstResult == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(firstResult.Name) || firstResult.Id <= 0)
            {
                _logger.LogWarning("Skipping malformed BGG expansion (id {BggId}) for game {GameId}", firstResult.Id, game.Id);
                continue;
            }

            var expansion = new Expansion(firstResult.Name, firstResult.Id, game.Id);
            game.AddExpansion(expansion);
        }

        await _unitOfWork.SaveChangesAsync();
        return game.Expansions.ToList();
    }

    public Task<List<Expansion>> GetGameExpansions(List<int> expansionIds)
    {
        return _gameRepository.GetExpansions(expansionIds);
    }

    public async Task DeleteExpansion(int gameId, int expansionId)
    {
        _logger.LogDebug("Deleting expansion {ExpansionId} from game {GameId}", expansionId, gameId);
        await _gameRepository.DeleteExpansion(gameId, expansionId);
        await  _unitOfWork.SaveChangesAsync();
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
            return new GamePriceDto { GameId = watchInfo.Id, Available = false };
        }

        var result = await _changeDetectionClient.GetLatestAsync(watchInfo.WatchId, forceRefresh, cancellationToken);
        return MapPrice(watchInfo.Id, watchInfo.WatchId, result);
    }

    public async Task<List<GamePriceDto>> GetWantedPricesAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching prices for wanted games");
        var games = await _gameRepository.GetWantedGamesWithWatchId();

        var watchIds = games.Select(game => game.ChangeDetectionWatchId!).ToList();
        var results = await _changeDetectionClient.GetLatestAsync(watchIds, forceRefresh, cancellationToken);

        return games
            .Select(game =>
            {
                results.TryGetValue(game.ChangeDetectionWatchId!, out var result);
                return MapPrice(game.Id, game.ChangeDetectionWatchId, result ?? ChangeDetectionResult.Unavailable());
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
            InStock = result.InStock,
            Price = result.Price,
            FetchedAt = result.FetchedAt
        };
    }

    private async Task EnsureBggConfiguredAsync()
    {
        var apiKey = await _settingsService.GetBggApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new BggFeatureDisabledException();
    }
}
