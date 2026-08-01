using System.Net;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class BggImportService : IBggImportService
{
    private readonly IBoardGameGeekXmlApi2Client _bggClient;
    private readonly ISettingsService _settingsService;
    private readonly IGameFactory _gameFactory;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BggImportService> _logger;

    public BggImportService(
        IBoardGameGeekXmlApi2Client bggClient,
        ISettingsService settingsService,
        IGameFactory gameFactory,
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        ILogger<BggImportService> logger)
    {
        _bggClient = bggClient;
        _settingsService = settingsService;
        _gameFactory = gameFactory;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Game?> ImportGameFromBgg(BggSearch search)
    {
        await EnsureBggConfiguredAsync();

        var existingGame = await _gameRepository.GetGameByBggId(search.BggId);
        if (existingGame != null)
        {
            return existingGame;
        }

        _logger.LogDebug("Searching BGG for game with id {BggId}", search.BggId);
        var item = await FetchThingFromBgg(search.BggId);
        if (item == null)
        {
            return null;
        }

        var game = await _gameFactory.CreateFromBggAsync(
            item,
            search.HasScoring,
            search.State,
            search.Price.HasValue ? (decimal?)search.Price.Value : null,
            search.AdditionDate,
            search.ShopUrl);

        await _gameRepository.CreateAsync(game);
        await _unitOfWork.SaveChangesAsync();
        return game;
    }

    public async Task<IList<BggImportGame>> ImportBggCollection(string userName)
    {
        await EnsureBggConfiguredAsync();
        _logger.LogInformation("Starting BGG collection import for user {UserName}", userName);

        CollectionResponse response;
        try
        {
            var request = new CollectionRequest(userName, subType: "boardgame");
            response = await _bggClient.GetCollectionAsync(request);
        }
        catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(ex, "BGG API key is invalid or expired");
            throw new ValidationException("Invalid BGG API key. Please check your API key in settings.");
        }
        catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(ex, "BGG rate-limited the collection request for user {UserName}", userName);
            throw new BggRateLimitException();
        }
        catch (BoardGameGeekHttpException ex)
        {
            _logger.LogWarning(ex, "BGG API request failed for collection import of user {UserName}", userName);
            throw;
        }
        catch (Exception ex) when (ex.Message == "Retries exhausted")
        {
            _logger.LogWarning(ex, "BGG collection for user {UserName} was still being prepared after retries", userName);
            throw new BggCollectionPreparingException();
        }

        if (response.Result == null || response.Result.Count == 0)
        {
            return new List<BggImportGame>();
        }

        return response.Result
            .OrderBy(x => x.Name)
            .Select(collectionItem => new BggImportGame
            {
                BggId = collectionItem.ObjectId,
                Title = collectionItem.Name,
                State = collectionItem.Status.ToGameState(),
                ImageUrl = collectionItem.Image ?? string.Empty,
                LastModified = collectionItem.Status.LastModified
            })
            .ToList();
    }

    public async Task ImportList(IList<ImportGame> games)
    {
        await EnsureBggConfiguredAsync();
        _logger.LogInformation("Importing {Count} games from BGG", games.Count);

        var imported = 0;
        foreach (var importGame in games)
        {
            try
            {
                var existingGame = await _gameRepository.GetGameByBggId(importGame.BggId);
                if (existingGame != null)
                {
                    _logger.LogDebug("BGG game with id {BggId} already imported, skipping", importGame.BggId);
                    continue;
                }

                var item = await FetchThingFromBgg(importGame.BggId);
                if (item == null)
                {
                    _logger.LogWarning("BGG game with id {BggId} not found, skipping", importGame.BggId);
                    continue;
                }

                var game = await _gameFactory.CreateFromBggAsync(
                    item,
                    importGame.HasScoring,
                    importGame.State,
                    ToSafeDecimalPrice(importGame.Price),
                    importGame.AddedDate);

                await _gameRepository.CreateAsync(game);
                imported++;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import BGG game {BggId}, skipping", importGame.BggId);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("BGG import completed, {Imported}/{Count} games imported", imported, games.Count);
    }

    private static decimal? ToSafeDecimalPrice(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return null;
        }

        try
        {
            return (decimal)value;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private async Task<ThingResponse.Item?> FetchThingFromBgg(int bggId)
    {
        try
        {
            var request = new ThingRequest([bggId], stats: true);
            var response = await _bggClient.GetThingAsync(request);
            if (!response.Succeeded)
            {
                return null;
            }

            return response.Result?.FirstOrDefault();
        }
        catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(ex, "BGG API key is invalid or expired");
            throw new ValidationException("Invalid BGG API key. Please check your API key in settings.");
        }
        catch (BoardGameGeekHttpException ex)
        {
            _logger.LogWarning(ex, "BGG API request failed for game {BggId}", bggId);
            return null;
        }
    }

    private async Task EnsureBggConfiguredAsync()
    {
        var enabled = await _settingsService.IsBggEnabled();
        if (!enabled)
        {
            throw new BggFeatureDisabledException();
        }
    }
}
