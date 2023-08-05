using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
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

    public async Task<Game> ProcessBggGameData(BggGame rawGame, GameState gameState)
    {
        var categories = _mapper.Map<IList<GameCategory>>(rawGame.Categories);
        await _gameRepository.AddGameCategoriesIfNotExists(categories);
        
        var mechanics = _mapper.Map<IList<GameMechanic>>(rawGame.Mechanics);
        await _gameRepository.AddGameMechanicsIfNotExists(mechanics);

        var people = _mapper.Map<IList<Person>>(rawGame.People);
        await _gameRepository.AddPeopleIfNotExists(people);

        var game = _mapper.Map<Game>(rawGame);
        game.Image = await _imageService.DownloadImage(rawGame.Image, rawGame.BggId.ToString());
        game.State = gameState;
        
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

    public Task<Game?> GetGameById(int id, bool includePlays)
    {
        return _gameRepository.GetGameById(id, includePlays);
    }

    public async Task Delete(int id)
    {
        var game = await _gameRepository.GetGameById(id, true);
        if (game == null)
        {
            return;
        }
        
        _imageService.DeleteImage(game.Image);
        await _gameRepository.DeleteGame(game);
    }
}