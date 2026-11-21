using System.Net;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Import;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class BggImportService : IBggImportService
{
    private const int ThingRequestBatchSize = 20;
    private const int MaxBaseGameLookups = 3;
    private readonly IBoardGameGeekXmlApi2Client _bggClient;
    private readonly ISettingsService _settingsService;
    private readonly IGameFactory _gameFactory;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageService _imageService;
    private readonly ILogger<BggImportService> _logger;

    public BggImportService(
        IBoardGameGeekXmlApi2Client bggClient,
        ISettingsService settingsService,
        IGameFactory gameFactory,
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        IImageService imageService,
        ILogger<BggImportService> logger)
    {
        _bggClient = bggClient;
        _settingsService = settingsService;
        _gameFactory = gameFactory;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<Game?> ImportGameFromBgg(BggSearch search)
    {
        await EnsureBggConfiguredAsync();

        var existingGame = await _gameRepository.GetGameByBggId(search.BggId);
        if (existingGame != null)
        {
            throw new DomainException(Constants.Errors.GameAlreadyExists);
        }

        _logger.LogDebug("Searching BGG for game with id {BggId}", search.BggId);
        var item = await FetchThingFromBgg(search.BggId);
        if (item == null)
        {
            return null;
        }

        if (item.Type == Constants.Bgg.Expansion)
        {
            return await ImportExpansionFromBgg(item, search);
        }

        var game = await _gameFactory.CreateFromBggAsync(
            item,
            search.HasScoring,
            search.State,
            search.Price is > 0 ? (decimal?)search.Price.Value : null,
            search.AdditionDate,
            search.ShopUrl);

        try
        {
            await _gameRepository.CreateAsync(game);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            _imageService.DeleteImage(game.Image);
            throw;
        }

        return game;
    }

    private async Task<Game?> ImportExpansionFromBgg(ThingResponse.Item item, BggSearch search)
    {
        var existingExpansions = await _gameRepository.GetExistingExpansionBggIds([item.Id]);
        if (existingExpansions.Count > 0)
        {
            throw new DomainException(Constants.Errors.ExpansionAlreadyExists);
        }

        var candidates = BaseGameCandidates(item);
        Game? baseGame = null;
        foreach (var candidate in candidates)
        {
            baseGame = await _gameRepository.GetGameByBggId(candidate);
            if (baseGame != null)
            {
                break;
            }
        }

        Game? createdBase = null;
        if (baseGame == null)
        {
            foreach (var candidate in candidates.Take(MaxBaseGameLookups))
            {
                var baseItem = await FetchThingFromBgg(candidate);
                if (baseItem?.Type != Constants.Bgg.BoardGame)
                {
                    continue;
                }

                createdBase = await _gameFactory.CreateFromBggAsync(baseItem, search.HasScoring, search.State, null, search.AdditionDate);
                await _gameRepository.CreateAsync(createdBase);
                baseGame = createdBase;
                break;
            }
        }

        if (baseGame == null)
        {
            _logger.LogWarning("No base game found on BGG for expansion {BggId}", item.Id);
            return null;
        }

        Expansion? expansion = null;
        try
        {
            expansion = await _gameFactory.CreateExpansionFromBggAsync(
                item,
                baseGame,
                search.State,
                search.Price is > 0 ? (decimal?)search.Price.Value : null,
                search.AdditionDate);
            baseGame.AddExpansion(expansion);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            _imageService.DeleteImage(expansion?.Image);
            _imageService.DeleteImage(createdBase?.Image);
            throw;
        }

        _logger.LogInformation("Imported BGG expansion {BggId} into game {GameId}", item.Id, baseGame.Id);
        return baseGame;
    }

    public async Task<IList<BggImportGame>> ImportBggCollection(string userName, CancellationToken cancellationToken = default)
    {
        await EnsureBggConfiguredAsync();
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Starting BGG collection import for user {UserName}", userName);

        var collection = await FetchCollection(new CollectionRequest(userName, subType: Constants.Bgg.BoardGame), userName, cancellationToken);
        if (collection.Count == 0)
        {
            return [];
        }

        var games = collection
            .Select(collectionItem => (Item: collectionItem, State: collectionItem.Status.ToGameState()))
            .Where(x => x.State.HasValue)
            .OrderBy(x => x.Item.Name)
            .Select(x => new BggImportGame
            {
                BggId = x.Item.ObjectId,
                Title = x.Item.Name,
                State = x.State!.Value,
                ImageUrl = x.Item.Image ?? string.Empty,
                LastModified = x.Item.Status.LastModified
            })
            .ToList();

        var expansionIds = (await FetchCollection(new CollectionRequest(userName, subType: Constants.Bgg.Expansion), userName, cancellationToken))
            .Select(x => x.ObjectId)
            .ToHashSet();
        var expansions = games.Where(x => expansionIds.Contains(x.BggId)).ToList();
        if (expansions.Count == 0)
        {
            return games;
        }

        await LinkExpansionsToBaseGames(games, expansions);
        return games.Where(x => !expansionIds.Contains(x.BggId) || x.BaseGameBggId != null).ToList();
    }

    private async Task LinkExpansionsToBaseGames(List<BggImportGame> games, List<BggImportGame> expansions)
    {
        var items = await FetchThingsFromBgg(expansions.Select(x => x.BggId).ToList());
        var candidatesPerExpansion = expansions.ToDictionary(
            x => x.BggId,
            x => items.TryGetValue(x.BggId, out var item) ? BaseGameCandidates(item) : []);

        var allCandidates = candidatesPerExpansion.Values.SelectMany(x => x).Distinct().ToList();
        var inDatabase = (await _gameRepository.GetExistingGameBggIds(allCandidates)).ToHashSet();
        var expansionIds = expansions.Select(x => x.BggId).ToHashSet();
        var inCollection = games.Where(x => !expansionIds.Contains(x.BggId)).Select(x => x.BggId).ToHashSet();

        foreach (var expansion in expansions)
        {
            var candidates = candidatesPerExpansion[expansion.BggId];
            var baseGameBggId = expansion.BaseGameBggId
                ?? candidates.Cast<int?>().FirstOrDefault(x => inDatabase.Contains(x!.Value))
                ?? candidates.Cast<int?>().FirstOrDefault(x => inCollection.Contains(x!.Value))
                ?? candidates.Cast<int?>().FirstOrDefault();
            if (baseGameBggId == null)
            {
                _logger.LogWarning("No base game found on BGG for expansion {BggId}, leaving it out", expansion.BggId);
                continue;
            }

            items.TryGetValue(expansion.BggId, out var item);
            expansion.BaseGameBggId = baseGameBggId;
            expansion.BaseGameTitle = item?.Links?.FirstOrDefault(l => l.Id == baseGameBggId)?.Value ?? expansion.BaseGameTitle;
            if (string.IsNullOrWhiteSpace(expansion.Title))
            {
                expansion.Title = item?.Name ?? string.Empty;
            }

            if (string.IsNullOrEmpty(expansion.ImageUrl))
            {
                expansion.ImageUrl = item?.Image ?? string.Empty;
            }
        }
    }

    public async Task<CsvImportResult> ImportCsv(string csv)
    {
        await EnsureBggConfiguredAsync();
        var parsed = CsvCollectionParser.Parse(csv, DateTime.UtcNow.Date);
        var expansions = parsed.Games.Where(x => parsed.ExpansionIds.Contains(x.BggId)).ToList();
        if (expansions.Count > 0)
        {
            await LinkExpansionsToBaseGames(parsed.Games, expansions);
        }

        foreach (var expansion in expansions.Where(x => x.BaseGameBggId == null))
        {
            parsed.Rejected.Add(new CsvRejectedRow
            {
                Line = parsed.Lines[expansion.BggId],
                Title = expansion.Title,
                BggId = expansion.BggId,
                Reason = CsvRejectReasons.NoBaseGame
            });
        }

        var result = new CsvImportResult
        {
            Games = parsed.Games.Where(x => !parsed.ExpansionIds.Contains(x.BggId) || x.BaseGameBggId != null).ToList(),
            Rejected = parsed.Rejected.OrderBy(x => x.Line).ToList()
        };
        _logger.LogInformation("Read {Count} games from a CSV file, {Rejected} rows rejected", result.Games.Count, result.Rejected.Count);
        return result;
    }

    public async Task ImportList(IList<ImportGame> games)
    {
        ArgumentNullException.ThrowIfNull(games);
        await EnsureBggConfiguredAsync();
        var distinctGames = games.DistinctBy(x => x.BggId).ToList();
        _logger.LogInformation("Importing {Count} games from BGG", distinctGames.Count);

        var toImport = await GamesToImport(distinctGames.Where(x => x.BaseGameBggId == null));

        var expansionsToImport = await ExpansionsToImport(distinctGames.Where(x => x.BaseGameBggId != null).ToList());
        var baseGames = new Dictionary<int, Game>();
        var missingBaseGames = await MissingBaseGames(expansionsToImport, toImport, baseGames);

        var bggIds = toImport.Concat(missingBaseGames).Concat(expansionsToImport).Select(x => x.BggId).ToList();
        var items = bggIds.Count > 0
            ? await FetchThingsFromBgg(bggIds)
            : [];

        var created = new List<Game>();
        var createdExpansions = new List<Expansion>();
        try
        {
            var imported = 0;
            foreach (var importGame in toImport.Concat(missingBaseGames))
            {
                var game = await CreateGameAsync(importGame, items);
                if (game == null)
                {
                    continue;
                }

                created.Add(game);
                baseGames[importGame.BggId] = game;
                if (toImport.Contains(importGame))
                {
                    imported++;
                }
            }

            foreach (var importExpansion in expansionsToImport)
            {
                var expansion = await CreateExpansionAsync(importExpansion, items, baseGames);
                if (expansion != null)
                {
                    createdExpansions.Add(expansion);
                    imported++;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("BGG import completed, {Imported}/{Count} games imported", imported, distinctGames.Count);
        }
        catch
        {
            DeleteImages(created, createdExpansions);
            throw;
        }
    }

    private async Task<List<ImportGame>> GamesToImport(IEnumerable<ImportGame> games)
    {
        var toImport = new List<ImportGame>();
        foreach (var importGame in games)
        {
            var existingGame = await _gameRepository.GetGameByBggId(importGame.BggId);
            if (existingGame != null)
            {
                _logger.LogDebug("BGG game with id {BggId} already imported, skipping", importGame.BggId);
                continue;
            }

            toImport.Add(importGame);
        }

        return toImport;
    }

    private async Task<List<ImportGame>> MissingBaseGames(
        List<ImportGame> expansionsToImport,
        List<ImportGame> toImport,
        Dictionary<int, Game> baseGames)
    {
        var missingBaseGames = new List<ImportGame>();
        foreach (var expansion in expansionsToImport.DistinctBy(x => x.BaseGameBggId))
        {
            var baseGameBggId = expansion.BaseGameBggId!.Value;
            if (toImport.Any(x => x.BggId == baseGameBggId))
            {
                continue;
            }

            var existingGame = await _gameRepository.GetGameByBggId(baseGameBggId);
            if (existingGame != null)
            {
                baseGames[baseGameBggId] = existingGame;
            }
            else
            {
                missingBaseGames.Add(new ImportGame
                {
                    BggId = baseGameBggId,
                    Title = string.Empty,
                    ImageUrl = string.Empty,
                    State = expansion.State,
                    AddedDate = expansion.AddedDate
                });
            }
        }

        return missingBaseGames;
    }

    private void DeleteImages(List<Game> games, List<Expansion> expansions)
    {
        foreach (var game in games)
        {
            _imageService.DeleteImage(game.Image);
        }

        foreach (var expansion in expansions)
        {
            _imageService.DeleteImage(expansion.Image);
        }
    }

    private async Task<List<ImportGame>> ExpansionsToImport(List<ImportGame> expansions)
    {
        if (expansions.Count == 0)
        {
            return expansions;
        }

        var existing = (await _gameRepository.GetExistingExpansionBggIds(expansions.Select(x => x.BggId))).ToHashSet();
        foreach (var expansion in expansions.Where(x => existing.Contains(x.BggId)))
        {
            _logger.LogDebug("BGG expansion with id {BggId} already imported, skipping", expansion.BggId);
        }

        return expansions.Where(x => !existing.Contains(x.BggId)).ToList();
    }

    private async Task<Game?> CreateGameAsync(ImportGame importGame, Dictionary<int, ThingResponse.Item> items)
    {
        Game? game = null;
        try
        {
            if (!items.TryGetValue(importGame.BggId, out var item))
            {
                _logger.LogWarning("BGG game with id {BggId} not found, skipping", importGame.BggId);
                return null;
            }

            game = await _gameFactory.CreateFromBggAsync(
                item,
                importGame.HasScoring,
                importGame.State,
                importGame.Price is > 0 ? importGame.Price : null,
                importGame.AddedDate);

            await _gameRepository.CreateAsync(game);
            return game;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (game != null)
            {
                _imageService.DeleteImage(game.Image);
            }

            _logger.LogWarning(ex, "Failed to import BGG game {BggId}, skipping", importGame.BggId);
            return null;
        }
    }

    private async Task<Expansion?> CreateExpansionAsync(ImportGame importExpansion, Dictionary<int, ThingResponse.Item> items, Dictionary<int, Game> baseGames)
    {
        if (!baseGames.TryGetValue(importExpansion.BaseGameBggId!.Value, out var baseGame))
        {
            _logger.LogWarning("Base game {BaseGameBggId} of BGG expansion {BggId} could not be imported, skipping", importExpansion.BaseGameBggId, importExpansion.BggId);
            return null;
        }

        if (!items.TryGetValue(importExpansion.BggId, out var item))
        {
            _logger.LogWarning("BGG expansion with id {BggId} not found, skipping", importExpansion.BggId);
            return null;
        }

        try
        {
            var expansion = await _gameFactory.CreateExpansionFromBggAsync(
                item,
                baseGame,
                importExpansion.State,
                importExpansion.Price is > 0 ? importExpansion.Price : null,
                importExpansion.AddedDate);
            baseGame.AddExpansion(expansion);
            return expansion;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to import BGG expansion {BggId}, skipping", importExpansion.BggId);
            return null;
        }
    }

    private static List<int> BaseGameCandidates(ThingResponse.Item item)
    {
        return (item.Links ?? [])
            .Where(l => l.Type == Constants.Bgg.Expansion && l.Id > 0)
            .Select(l => l.Id)
            .Distinct()
            .ToList();
    }

    private async Task<List<CollectionResponse.Item>> FetchCollection(CollectionRequest request, string userName, CancellationToken cancellationToken)
    {
        CollectionResponse response;
        try
        {
            response = await _bggClient.GetCollectionAsync(request).WaitAsync(cancellationToken);
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

        return response?.Result?.ToList() ?? [];
    }

    private async Task<Dictionary<int, ThingResponse.Item>> FetchThingsFromBgg(IReadOnlyList<int> bggIds)
    {
        var items = new Dictionary<int, ThingResponse.Item>();
        foreach (var chunk in bggIds.Distinct().Chunk(ThingRequestBatchSize))
        {
            try
            {
                var request = new ThingRequest(chunk, stats: true);
                var response = await _bggClient.GetThingAsync(request);
                if (!response.Succeeded || response.Result == null)
                {
                    continue;
                }

                foreach (var item in response.Result)
                {
                    items[item.Id] = item;
                }
            }
            catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning(ex, "BGG API key is invalid or expired");
                throw new ValidationException("Invalid BGG API key. Please check your API key in settings.");
            }
            catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(ex, "BGG rate-limited the request while importing games");
                throw new BggRateLimitException();
            }
            catch (BoardGameGeekHttpException ex)
            {
                _logger.LogWarning(ex, "BGG API request failed for games {BggIds}", chunk);
            }
        }

        return items;
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
        catch (BoardGameGeekHttpException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(ex, "BGG rate-limited the request for game {BggId}", bggId);
            throw new BggRateLimitException();
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
