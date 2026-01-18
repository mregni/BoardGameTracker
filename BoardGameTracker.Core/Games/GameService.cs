using System.Globalization;
using System.Net;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Common.ValueObjects;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameStatisticsRepository _gameStatisticsRepository;
    private readonly IBggApi _bggApi;
    private readonly IImageService _imageService;
    private readonly IBggGameTranslator _bggGameTranslator;
    private readonly IGameFactory _gameFactory;
    private readonly IUnitOfWork _unitOfWork;
    
    public GameService(
        IGameRepository gameRepository,
        IGameSessionRepository gameSessionRepository,
        IGameStatisticsRepository gameStatisticsRepository,
        IImageService imageService,
        IBggApi bggApi,
        IBggGameTranslator bggGameTranslator,
        IGameFactory gameFactory, IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _gameSessionRepository = gameSessionRepository;
        _gameStatisticsRepository = gameStatisticsRepository;
        _imageService = imageService;
        _bggApi = bggApi;
        _bggGameTranslator = bggGameTranslator;
        _gameFactory = gameFactory;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Game> SearchOnBgg(BggGame rawGame, BggSearch search)
    {
        var result = await ProcessBggGameData(rawGame, search);
        await _unitOfWork.SaveChangesAsync();
        return result;
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

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return _gameRepository.GetGameByBggId(bggId);
    }

    public Task<List<Game>> GetGames()
    {
        return _gameRepository.GetGamesOverviewList();
    }

    public Task<Game?> GetGameById(int id)
    {
        return _gameRepository.GetByIdAsync(id);
    }

    public async Task Delete(int id)
    {
        var game = await _gameRepository.GetByIdAsync(id);
        if (game == null)
        {
            return;
        }

        _imageService.DeleteImage(game.Image);
        await _gameRepository.DeleteAsync(game.Id);
    }

    public async Task<Dictionary<SessionFlag, int?>> GetPlayFlags(int id)
    {
        var shortestPlay = await _gameSessionRepository.GetShortestPlay(id);
        var longestPlay = await _gameSessionRepository.GetLongestPlay(id);
        var highestScore = await _gameStatisticsRepository.GetHighScorePlay(id);
        var lowestScore = await _gameStatisticsRepository.GetLowestScorePlay(id);

        var dict = new Dictionary<SessionFlag, int?>
        {
            {SessionFlag.ShortestGame, shortestPlay},
            {SessionFlag.HighestScore, highestScore}
        };

        if (shortestPlay != longestPlay)
        {
            dict.Add(SessionFlag.LongestGame, longestPlay);
        }

        if (highestScore != lowestScore)
        {
            dict.Add(SessionFlag.LowestScore, lowestScore);
        }

        return dict;
    }

    public Task<int> GetTotalPlayCount(int id)
    {
        return _gameSessionRepository.GetPlayCount(id);
    }

    public async Task<IEnumerable<PlayByDay>> GetPlayByDayChart(int id)
    {
        var list = await _gameStatisticsRepository.GetPlayByDayChart(id);
        return Enum.GetValues(typeof(DayOfWeek))
            .Cast<DayOfWeek>()
            .OrderBy(day => ((int)day + 6) % 7)
            .ToDictionary(day => day, day => list.SingleOrDefault(y => y.Key == day)?.Count() ?? 0)
            .Select(x => new PlayByDay {DayOfWeek = x.Key, PlayCount = x.Value});
    }

    public async Task<IEnumerable<PlayerCount>> GetPlayerCountChart(int id)
    {
        var list = await _gameStatisticsRepository.GetPlayerCountChart(id);
        return list.Select(x => new PlayerCount {PlayCount = x.Count(), Players = x.Key});
    }

    public Task<int> CountAsync()
    {
        return _gameRepository.CountAsync();
    }

    public async Task<BggGame?> SearchGame(int id)
    {
        var response = await _bggApi.SearchGame(id, 1);
        var firstResult = response.Content?.Games?.FirstOrDefault();
        if (!response.IsSuccessStatusCode || firstResult == null)
        {
            return null;
        }

        return _bggGameTranslator.TranslateRawGame(firstResult);
    }

    public async Task<BggLink[]> SearchExpansionsForGame(int id)
    {
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

    public async Task<List<TopPlayerDto>> GetTopPlayers(int id)
    {
        var sessions = await _gameSessionRepository.GetSessions(id, 0, null);
        var playerSessions = sessions
            .SelectMany(x => x.PlayerSessions)
            .GroupBy(x => x.PlayerId)
            .ToList();

        return playerSessions
            .Select(TopPlayerDto.CreateTopPlayer)
            .Where(x => x.Wins > 0)
            .OrderByDescending(x => x.Wins)
            .Take(Constants.Game.TopPlayersCount)
            .ToList();
    }

    public async Task<Dictionary<DateTime, XValue[]>?> GetPlayerScoringChart(int id)
    {
        var game = await _gameRepository.GetByIdAsync(id);
        if (game is not {HasScoring: true})
        {
            return null;
        }

        var sessions = await _gameSessionRepository.GetSessions(id, -Constants.Game.ChartHistoryDays);

        var uniquePlayerIds = sessions
            .SelectMany(session => session.PlayerSessions)
            .Select(ps => ps.PlayerId)
            .Distinct()
            .ToList();

        var chartData = new Dictionary<DateTime, XValue[]>();

        foreach (var session in sessions)
        {
            var playerIdsInSession = session.PlayerSessions.Select(ps => ps.PlayerId).ToHashSet();

            var participatingPlayers = session.PlayerSessions
                .Select(ps => new XValue
                {
                    Id = ps.PlayerId,
                    Value = ps.Score
                });

            var nonParticipatingPlayers = uniquePlayerIds
                .Where(playerId => !playerIdsInSession.Contains(playerId))
                .Select(playerId => new XValue
                {
                    Id = playerId,
                    Value = null
                });

            var allPlayerValues = participatingPlayers.Concat(nonParticipatingPlayers).ToArray();
            chartData.TryAdd(session.Start, allPlayerValues);
        }

        return chartData;
    }

    public async Task<List<ScoreRank>> GetScoringRankedChart(int id)
    {
        var list = new List<ScoreRank>();
        var highestScoring = await _gameStatisticsRepository.GetHighestScoringPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeHighestScoreRank(highestScoring));

        var highestLosing = await _gameStatisticsRepository.GetHighestLosingPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeHighestLosingRank(highestLosing));

        var average = await _gameStatisticsRepository.GetAverageScore(id);
        list.AddIfNotNull(ScoreRank.MakeAverageRank(average));

        var lowestWinning = await _gameStatisticsRepository.GetLowestWinning(id);
        list.AddIfNotNull(ScoreRank.MakeLowestWinningRank(lowestWinning));

        var lowest = await _gameStatisticsRepository.GetLowestScoringPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeLowestScoreRank(lowest));

        return list;
    }

    public async Task<Game> CreateGame(Game game)
    {
        await _gameRepository.CreateAsync(game);
        await _unitOfWork.SaveChangesAsync();
        return game;
    }

    public async Task<Game> CreateGameFromCommand(CreateGameCommand command)
    {
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
        return game;
    }

    public Task<List<Session>> GetSessionsForGame(int id, int? count)
    {
        return _gameSessionRepository.GetSessionsByGameId(id, count);
    }

    public async Task<Game> UpdateGame(Game game)
    {
        var updatedGame = await _gameRepository.UpdateAsync(game);
        await _unitOfWork.SaveChangesAsync();
        return updatedGame;
    }

    public async Task<List<Expansion>> UpdateGameExpansions(int gameId, int[] expansionIds)
    {
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
            .Where(x => !game.Expansions.Select(y => y.BggId).Contains(x));
        foreach (var expansionId in newExpansionsIds)
        {
            var expansionResult = await _bggApi.SearchExpansion(expansionId, 0);
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

        await _gameRepository.UpdateAsync(game);
        await _unitOfWork.SaveChangesAsync();
        return game.Expansions.ToList();
    }

    public Task<List<Expansion>> GetGameExpansions(List<int> expansionIds)
    {
        return _gameRepository.GetExpansions(expansionIds);
    }

    public async Task DeleteExpansion(int gameId, int expansionId)
    {
        await _gameRepository.DeleteExpansion(gameId, expansionId);
        await  _unitOfWork.SaveChangesAsync();
    }

    public async Task<BggImportResult?> ImportBggCollection(string userName)
    {
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
        foreach (var game in games)
        {
            var bggGame = await SearchGame(game.BggId);
            if (bggGame == null)
            {
                continue;
            }

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
    }
}