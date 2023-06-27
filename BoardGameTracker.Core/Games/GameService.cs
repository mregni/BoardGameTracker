using System.Collections;
using System.Reflection.Metadata;
using AutoMapper;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Games.Interfaces;

namespace BoardGameTracker.Core.Games;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;
    public GameService(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<Game> ProcessBggGameData(BggGame rawGame)
    {
        var categories = _mapper.Map<IList<GameCategory>>(rawGame.Categories);
        await _gameRepository.AddGameCategoriesIfNotExists(categories);
        
        var mechanics = _mapper.Map<IList<GameMechanic>>(rawGame.Mechanics);
        await _gameRepository.AddGameMechanicsIfNotExists(mechanics);

        var people = _mapper.Map<IList<Person>>(rawGame.People);
        await _gameRepository.AddPeopleIfNotExists(people);
        
        var game = _mapper.Map<Game>(rawGame);
        return await _gameRepository.InsertGame(game);
    }

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return _gameRepository.GetGameByBggId(bggId);
    }
}