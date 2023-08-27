﻿using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;
    public GameService(IGameRepository gameRepository, IMapper mapper, IImageService imageService)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
        _imageService = imageService;
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

    public Task<List<Play>> GetPlays(int id)
    {
        return _gameRepository.GetPlays(id);
    }

    public async Task<GameStatistics> GetStats(int id)
    {
        return new GameStatistics
        {
            PlayCount = await _gameRepository.GetPlayCount(id),
            TotalPlayedTime = await _gameRepository.GetTotalPlayedTime(id),
            PricePerPlay = await _gameRepository.GetPricePerPlay(id),
            UniquePlayerCount = await _gameRepository.GetUniquePlayerCount(id),
            HighScore = await _gameRepository.GetHighestScore(id),
            MostWinsPlayer = await _gameRepository.GetMostWins(id),
            AverageScore = await _gameRepository.GetAverageScore(id)
        };
    }
}