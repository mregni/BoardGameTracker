using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IBggApi _bggApi;
    private readonly IImageService _imageService;
    private readonly IBggGameTranslator _bggGameTranslator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GameService> _logger;

    public GameService(
        IGameRepository gameRepository,
        IGameSessionRepository gameSessionRepository,
        IImageService imageService,
        IBggApi bggApi,
        IBggGameTranslator bggGameTranslator,
        IUnitOfWork unitOfWork,
        ILogger<GameService> logger)
    {
        _gameRepository = gameRepository;
        _gameSessionRepository = gameSessionRepository;
        _imageService = imageService;
        _bggApi = bggApi;
        _bggGameTranslator = bggGameTranslator;
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
        return _gameRepository.GetByIdAsync(id);
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
        return _gameSessionRepository.GetSessionsByGameId(id, count);
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
        game.UpdateDescription(command.Description ?? string.Empty);
        game.UpdatePlayerCount(command.MinPlayers, command.MaxPlayers);
        game.UpdatePlayTime(command.MinPlayTime, command.MaxPlayTime);
        game.UpdateMinAge(command.MinAge);
        game.UpdateBggId(command.BggId);
        game.UpdateBuyingPrice(command.BuyingPrice);
        game.UpdateSoldPrice(command.SoldPrice);
        game.UpdateRating(command.Rating);
        game.UpdateWeight(command.Weight);
        game.UpdateAdditionDate(command.AdditionDate);

        await _unitOfWork.SaveChangesAsync();
        return game;
    }

    public async Task<BggLink[]> SearchExpansionsForGame(int id)
    {
        _logger.LogDebug("Searching expansions for game {GameId}", id);
        var dbGame = await _gameRepository.GetByIdAsync(id);
        if (dbGame is not {BggId: not null})
        {
            return [];
        }
        var response = await _bggApi.SearchGame(dbGame.BggId.Value, 0);
        var firstResult = response.Content?.Games?.FirstOrDefault();
        if (!response.IsSuccessStatusCode || firstResult == null)
        {
            return [];
        }

        var game = _bggGameTranslator.TranslateRawGame(firstResult);
        return game.Expansions;
    }

    public async Task<List<Expansion>> UpdateGameExpansions(int gameId, int[] expansionIds)
    {
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

        var expansionResults = await Task.WhenAll(
            newExpansionsIds.Select(id => _bggApi.SearchExpansion(id, 0)));

        foreach (var expansionResult in expansionResults)
        {
            var firstResult = expansionResult.Content?.Games?.FirstOrDefault();
            if (!expansionResult.IsSuccessStatusCode || firstResult == null)
            {
                continue;
            }

            var expansion = new Expansion(
                firstResult.Names.FirstOrDefault()?.Value ?? string.Empty,
                firstResult.Id,
                game.Id
            );
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
}
