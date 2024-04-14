using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IBggApi _bggApi;
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;

    public GameService(IGameRepository gameRepository, IMapper mapper, IImageService imageService, IBggApi bggApi)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
        _imageService = imageService;
        _bggApi = bggApi;
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

        return await _gameRepository.InsertGame(game);
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
        return _gameRepository.GetGameById(id);
    }

    public async Task Delete(int id)
    {
        var game = await _gameRepository.GetGameById(id);
        if (game == null)
        {
            return;
        }

        _imageService.DeleteImage(game.Image);
        await _gameRepository.DeleteGame(game);
    }

    public Task<List<Play>> GetPlays(int id, int skip, int? take)
    {
        return _gameRepository.GetPlays(id, skip, take);
    }

    public async Task<Dictionary<PlayFlag, int?>> GetPlayFlags(int id)
    {
        var shortestPlay = await _gameRepository.GetShortestPlay(id);
        var longestPlay = await _gameRepository.GetLongestPlay(id);
        var highestScore = await _gameRepository.GetHighScorePlay(id);
        var lowestScore = await _gameRepository.GetLowestScorePlay(id);

        var dict = new Dictionary<PlayFlag, int?>
        {
            {PlayFlag.ShortestGame, shortestPlay},
            {PlayFlag.HighestScore, highestScore}
        };

        if (shortestPlay != longestPlay)
        {
            dict.Add(PlayFlag.LongestGame, longestPlay);
        }

        if (highestScore != lowestScore)
        {
            dict.Add(PlayFlag.LowestScore, lowestScore);
        }

        return dict;
    }

    public Task<int> GetTotalPlayCount(int id)
    {
        return _gameRepository.GetTotalPlayCount(id);
    }

    public async Task<GameStatistics> GetStats(int id)
    {
        return new GameStatistics
        {
            PlayCount = await _gameRepository.GetPlayCount(id),
            TotalPlayedTime = await _gameRepository.GetTotalPlayedTime(id),
            PricePerPlay = await _gameRepository.GetPricePerPlay(id),
            HighScore = await _gameRepository.GetHighestScore(id),
            MostWinsPlayer = await _gameRepository.GetMostWins(id),
            AverageScore = await _gameRepository.GetAverageScore(id),
            LastPlayed = await _gameRepository.GetLastPlayedDateTime(id)
        };
    }

    public Task<int> CountAsync()
    {
        return _gameRepository.CountAsync();
    }

    public async Task<BggGame?> SearchAndCreateGame(int id)
    {
        var response = await _bggApi.SearchGame(id, "boardgame", 1);
        var firstResult = response.Content?.Games?.FirstOrDefault();
        if (!response.IsSuccessStatusCode || firstResult == null)
        {
            return null;
        }

        return _mapper.Map<BggGame>(firstResult);
    }

    public async Task<List<TopPlayer>> GetTopPlayers(int id)
    {
        var plays = await _gameRepository.GetPlays(id, 0, null);
        var playerPlays = plays
            .SelectMany(x => x.Players)
            .GroupBy(x => x.PlayerId)
            .ToList();

        return playerPlays
            .Select(TopPlayer.CreateTopPlayer)
            .Where(x => x.Wins > 0)
            .OrderByDescending(x => x.Wins)
            .Take(5)
            .ToList();
    }
}