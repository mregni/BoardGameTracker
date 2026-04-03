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
            search.AdditionDate);

        await _gameRepository.CreateAsync(game);
        await _unitOfWork.SaveChangesAsync();
        return game;
    }

    public async Task<BggImportResult?> ImportBggCollection(string userName)
    {
        await EnsureBggConfiguredAsync();
        _logger.LogInformation("Starting BGG collection import for user {UserName}", userName);

        CollectionResponse response;
        try
        {
            var request = new CollectionRequest(userName, subType: "boardgame,boardgameexpansion");
            response = await _bggClient.GetCollectionAsync(request);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(ex, "BGG API key is invalid or expired");
            throw new ValidationException("Invalid BGG API key. Please check your API key in settings.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "BGG API request failed for collection import of user {UserName}", userName);
            return null;
        }

        if (!response.Succeeded)
        {
            return null;
        }

        var result = new BggImportResult
        {
            StatusCode = HttpStatusCode.OK
        };

        if (response.Result == null || response.Result.Count == 0)
        {
            return result;
        }

        var list = response.Result.OrderBy(x => x.Name).ToList();
        result.Games = list.Select(collectionItem => new BggImportGame
        {
            BggId = collectionItem.ObjectId,
            Title = collectionItem.Name,
            State = collectionItem.Status.ToGameState(),
            ImageUrl = collectionItem.Image ?? string.Empty,
            LastModified = collectionItem.Status.LastModified,
            IsExpansion = collectionItem.SubType == "boardgameexpansion"
        }).ToList();

        return result;
    }

    public async Task ImportList(IList<ImportGame> games)
    {
        await EnsureBggConfiguredAsync();
        _logger.LogInformation("Importing {Count} games from BGG", games.Count);

        foreach (var importGame in games)
        {
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
                (decimal)importGame.Price,
                importGame.AddedDate);

            await _gameRepository.CreateAsync(game);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("BGG import completed, {Count} games processed", games.Count);
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
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(ex, "BGG API key is invalid or expired");
            throw new ValidationException("Invalid BGG API key. Please check your API key in settings.");
        }
        catch (HttpRequestException ex)
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
