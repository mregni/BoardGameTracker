using System.Globalization;
using System.Net;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class BggImportService : IBggImportService
{
    private readonly IBggApi _bggApi;
    private readonly IBggGameTranslator _bggGameTranslator;
    private readonly IGameFactory _gameFactory;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BggImportService> _logger;

    public BggImportService(
        IBggApi bggApi,
        IBggGameTranslator bggGameTranslator,
        IGameFactory gameFactory,
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        ILogger<BggImportService> logger)
    {
        _bggApi = bggApi;
        _bggGameTranslator = bggGameTranslator;
        _gameFactory = gameFactory;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BggGame?> SearchGame(int searchBggId)
    {
        _logger.LogDebug("Searching BGG for game with id {BggId}", searchBggId);
        var response = await _bggApi.SearchGame(searchBggId, 1);
        var firstResult = response.Content?.Games?.FirstOrDefault();
        if (!response.IsSuccessStatusCode || firstResult == null)
        {
            return null;
        }

        return _bggGameTranslator.TranslateRawGame(firstResult);
    }

    public async Task<Game> SearchOnBgg(BggGame rawGame, BggSearch search)
    {
        var result = await ProcessBggGameData(rawGame, search);
        await _unitOfWork.SaveChangesAsync();
        return result;
    }

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return _gameRepository.GetGameByBggId(bggId);
    }

    public async Task<BggImportResult?> ImportBggCollection(string userName)
    {
        _logger.LogInformation("Starting BGG collection import for user {UserName}", userName);
        var importGameResult = await _bggApi.ImportCollection(userName, "boardgame", "boardgameexpansion");
        if (!importGameResult.IsSuccessStatusCode)
        {
            return null;
        }

        var result = new BggImportResult
        {
            StatusCode = importGameResult.StatusCode
        };

        if (importGameResult.StatusCode != HttpStatusCode.OK)
        {
            return result;
        }

        if (importGameResult.Content?.Item == null)
        {
            return result;
        }

        var list = importGameResult.Content.Item.OrderBy(x => x.Name.Text).ToList();
        result.Games = list.Select(item => new BggImportGame
        {
            BggId = item.Objectid,
            Title = item.Name.Text,
            State = item.Status.ToGameState(),
            ImageUrl = item.Image.Text,
            LastModified = DateTime.ParseExact(
                item.Status.LastModified,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            IsExpansion = item.Subtype == "boardgameexpansion"
        }).ToList();

        return result;
    }

    public async Task ImportList(IList<ImportGame> games)
    {
        _logger.LogInformation("Importing {Count} games from BGG", games.Count);
        var bggGames = await Task.WhenAll(games.Select(g => SearchGame(g.BggId)));

        for (var i = 0; i < games.Count; i++)
        {
            var bggGame = bggGames[i];
            if (bggGame == null)
            {
                _logger.LogWarning("BGG game with id {BggId} not found, skipping", games[i].BggId);
                continue;
            }

            var game = games[i];
            var search = new BggSearch
            {
                BggId = game.BggId,
                HasScoring = game.HasScoring,
                State = game.State,
                AdditionDate = game.AddedDate,
                Price = game.Price
            };
            await ProcessBggGameData(bggGame, search);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("BGG import completed, {Count} games processed", games.Count);
    }

    private async Task<Game> ProcessBggGameData(BggGame rawGame, BggSearch search)
    {
        var gameImportData = await _bggGameTranslator.TranslateFromBggAsync(rawGame);

        var game = await _gameFactory.CreateFromImportDataAsync(
            gameImportData,
            search.HasScoring,
            search.State,
            search.Price.HasValue ? (decimal?)search.Price.Value : null,
            search.AdditionDate);

        await _gameRepository.CreateAsync(game);
        return game;
    }
}
