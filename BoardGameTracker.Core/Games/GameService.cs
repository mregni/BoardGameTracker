using System.Net;
using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IBggApi _bggApi;
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;

    public GameService(IGameRepository gameRepository, IMapper mapper, IImageService imageService, IBggApi bggApi,
        IPlayerRepository playerRepository)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
        _imageService = imageService;
        _bggApi = bggApi;
        _playerRepository = playerRepository;
    }

    public async Task<Game> ProcessBggGameData(BggGame rawGame, BggSearch search)
    {
        var categories = _mapper.Map<IList<GameCategory>>(rawGame.Categories);
        await _gameRepository.AddGameCategoriesIfNotExists(categories);

        var mechanics = _mapper.Map<IList<GameMechanic>>(rawGame.Mechanics);
        await _gameRepository.AddGameMechanicsIfNotExists(mechanics);

        var people = _mapper.Map<IList<Person>>(rawGame.People);
        await _gameRepository.AddPeopleIfNotExists(people);

        var game = _mapper.Map<Game>(rawGame);
        game.Image = await _imageService.DownloadImage(rawGame.Image, rawGame.BggId.ToString());

        game.State = search.State;
        game.BuyingPrice = search.Price;
        game.AdditionDate = search.AdditionDate;
        game.HasScoring = search.HasScoring;

        return await _gameRepository.CreateAsync(game);
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
        var shortestPlay = await _gameRepository.GetShortestPlay(id);
        var longestPlay = await _gameRepository.GetLongestPlay(id);
        var highestScore = await _gameRepository.GetHighScorePlay(id);
        var lowestScore = await _gameRepository.GetLowestScorePlay(id);

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
        return _gameRepository.GetTotalPlayCount(id);
    }

    public async Task<IEnumerable<PlayByDay>> GetPlayByDayChart(int id)
    {
        var list = await _gameRepository.GetPlayByDayChart(id);
        return Enum.GetValues(typeof(DayOfWeek))
            .Cast<DayOfWeek>()
            .ToDictionary(day => day, day => list.SingleOrDefault(y => y.Key == day)?.Count() ?? 0)
            .Select(x => new PlayByDay {DayOfWeek = x.Key, PlayCount = x.Value});
    }

    public async Task<IEnumerable<PlayerCount>> GetPlayerCountChart(int id)
    {
        var list = await _gameRepository.GetPlayerCountChart(id);
        return list.Select(x => new PlayerCount {PlayCount = x.Count(), Players = x.Key});
    }

    public async Task<GameStatistics> GetStats(int id)
    {
        var stats = new GameStatistics
        {
            PlayCount = await _gameRepository.GetPlayCount(id),
            TotalPlayedTime = await _gameRepository.GetTotalPlayedTime(id),
            PricePerPlay = await _gameRepository.GetPricePerPlay(id),
            HighScore = await _gameRepository.GetHighestScore(id),
            AveragePlayTime = await _gameRepository.GetAveragePlayTime(id),
            AverageScore = await _gameRepository.GetAverageScore(id),
            LastPlayed = await _gameRepository.GetLastPlayedDateTime(id),
            ExpansionCount = await _gameRepository.GetExpansionCount(id),
        };

        var mostWinPlayer = await _gameRepository.GetMostWins(id);
        if (mostWinPlayer != null)
        {
            var wins = await _playerRepository.GetWinCount(mostWinPlayer.Id, id);
            stats.MostWinsPlayer = new MostWinningPlayer
            {
                Id = mostWinPlayer.Id,
                Image = mostWinPlayer.Image,
                Name = mostWinPlayer.Name,
                TotalWins = wins
            };
        }

        return stats;
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

        return _mapper.Map<BggGame>(firstResult);
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

        var game = _mapper.Map<BggGame>(firstResult);
        return game.Expansions;
    }

    public async Task<List<TopPlayer>> GetTopPlayers(int id)
    {
        var sessions = await _gameRepository.GetSessions(id, 0, null);
        var playerSessions = sessions
            .SelectMany(x => x.PlayerSessions)
            .GroupBy(x => x.PlayerId)
            .ToList();

        return playerSessions
            .Select(TopPlayer.CreateTopPlayer)
            .Where(x => x.Wins > 0)
            .OrderByDescending(x => x.Wins)
            .Take(5)
            .ToList();
    }

    public async Task<Dictionary<DateTime, XValue[]>?> GetPlayerScoringChart(int id)
    {
        var game = await _gameRepository.GetByIdAsync(id);
        if (game is not {HasScoring: true})
        {
            return null;
        }
        
        var sessions = await _gameRepository.GetSessions(id, -200);
        var uniquePlayers = sessions
            .SelectMany(x => x.PlayerSessions)
            .GroupBy(x => x.PlayerId)
            .Select(x => x.Key)
            .ToList();

        var dict = new Dictionary<DateTime, XValue[]>();
        foreach (var play in sessions)
        {
            var players = play.PlayerSessions.Select(x =>
                new XValue
                {
                    Id = x.PlayerId,
                    Value = x.Score ?? null
                }
            );

            var missingPlayers = uniquePlayers
                .Where(x => !play.PlayerSessions.Select(y => y.PlayerId).Contains(x))
                .Select(x => new XValue
                {
                    Id = x,
                    Value = null
                });


            var xValues = new List<XValue>();
            xValues.AddRange(players);
            xValues.AddRange(missingPlayers);
            dict.TryAdd(play.Start, xValues.ToArray());
        }

        return dict;
    }

    public async Task<List<ScoreRank>> GetScoringRankedChart(int id)
    {
        var list = new List<ScoreRank>();
        var highestScoring = await _gameRepository.GetHighestScoringPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeHighestScoreRank(highestScoring));

        var highestLosing = await _gameRepository.GetHighestLosingPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeHighestLosingRank(highestLosing));

        var average = await _gameRepository.GetAverageScore(id);
        list.AddIfNotNull(ScoreRank.MakeAverageRank(average));

        var lowestWinning = await _gameRepository.GetLowestWinning(id);
        list.AddIfNotNull(ScoreRank.MakeLowestWinningRank(lowestWinning));

        var lowest = await _gameRepository.GetLowestScoringPlayer(id);
        list.AddIfNotNull(ScoreRank.MakeLowestScoreRank(lowest));

        return list;
    }

    public Task<Game> CreateGame(Game game)
    {
        return _gameRepository.CreateAsync(game);
    }

    public Task<List<Session>> GetSessionsForGame(int id)
    {
        return _gameRepository.GetSessionsByGameId(id);
    }

    public Task<Game> UpdateGame(Game game)
    {
        return _gameRepository.UpdateAsync(game);
    }

    public async Task<List<Expansion>> UpdateGameExpansions(int gameId, int[] expansionIds)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null)
        {
            return [];
        }

        game.Expansions = game.Expansions.Where(x => expansionIds.Contains(x.BggId)).ToList();
        
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

            var expansion = new Expansion
            {
                GameId = game.Id,
                BggId = firstResult.Id,
                Title = firstResult.Names.FirstOrDefault()?.Value ?? string.Empty
            };
            game.Expansions.Add(expansion);
        }

        await _gameRepository.UpdateAsync(game);
        return game.Expansions.ToList();
    }

    public Task<List<Expansion>> GetGameExpansions(List<int> expansionIds)
    {
        return _gameRepository.GetExpansions(expansionIds);
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
        result.Games = _mapper.Map<List<BggImportGame>>(list);

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
    }
}