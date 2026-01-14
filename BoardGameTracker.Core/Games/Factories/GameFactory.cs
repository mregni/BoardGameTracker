using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Core.Bgg.AntiCorruption;
using BoardGameTracker.Core.Games.Interfaces;

namespace BoardGameTracker.Core.Games.Factories;

public class GameFactory : IGameFactory
{
    private readonly IGameRepository _gameRepository;

    public GameFactory(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Game> CreateFromImportDataAsync(GameImportData data, bool hasScoring, GameState state, decimal? price, DateTime? additionDate)
    {
        var categories = data.Categories.Select(c => new GameCategory(c.Name)).ToList();
        await _gameRepository.AddGameCategoriesIfNotExists(categories);

        var mechanics = data.Mechanics.Select(m => new GameMechanic(m.Name)).ToList();
        await _gameRepository.AddGameMechanicsIfNotExists(mechanics);

        var people = data.People.Select(p => new Person(p.Name, Enum.Parse<PersonType>(p.Type))).ToList();
        await _gameRepository.AddPeopleIfNotExists(people);

        var game = new Game(data.Title, hasScoring, state);
        game.UpdateImage(data.ImageUrl);
        game.UpdateDescription(data.Description);
        game.UpdateYearPublished(data.YearPublished);
        game.UpdatePlayerCount(data.MinPlayers, data.MaxPlayers);
        game.UpdatePlayTime(data.MinPlayTime, data.MaxPlayTime);
        game.UpdateMinAge(data.MinAge);
        game.UpdateRating(data.Rating);
        game.UpdateWeight(data.Weight);
        game.UpdateBggId(data.BggId);
        game.UpdateBuyingPrice(price);

        if (additionDate.HasValue)
        {
            game.UpdateAdditionDate(additionDate.Value);
        }

        return game;
    }
}
