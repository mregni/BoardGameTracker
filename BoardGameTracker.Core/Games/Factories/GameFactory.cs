using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;

namespace BoardGameTracker.Core.Games.Factories;

public class GameFactory : IGameFactory
{
    private readonly IGameRepository _gameRepository;
    private readonly IImageService _imageService;

    public GameFactory(IGameRepository gameRepository, IImageService imageService)
    {
        _gameRepository = gameRepository;
        _imageService = imageService;
    }

    public async Task<Game> CreateFromBggAsync(ThingResponse.Item item, bool hasScoring, GameState state, decimal? price, DateTime? additionDate, string? shopUrl = null)
    {
        var name = item.Name;
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Game must have a valid name from BGG");

        int? minPlayers = item.MinPlayers > 0 ? item.MinPlayers : null;
        int? maxPlayers = item.MaxPlayers > 0 ? item.MaxPlayers : null;
        if (minPlayers.HasValue && maxPlayers.HasValue && minPlayers > maxPlayers)
            throw new InvalidOperationException($"Invalid player count range from BGG: {minPlayers}-{maxPlayers}");

        var minPlayTime = item.MinPlayingTime ?? 0;
        var maxPlayTime = item.MaxPlayingTime ?? 0;
        if (minPlayTime > maxPlayTime)
            throw new InvalidOperationException($"Invalid play time range from BGG: {minPlayTime}-{maxPlayTime}");

        var links = item.Links ?? [];

        var imageUrl = await _imageService.DownloadImage(
            item.Image ?? string.Empty,
            item.Id.ToString());

        var categories = links
            .Where(l => l.Type == Constants.Bgg.Category && !string.IsNullOrWhiteSpace(l.Value))
            .Select(c => new GameCategory(c.Value))
            .ToList();
        await _gameRepository.AddGameCategoriesIfNotExists(categories);

        var mechanics = links
            .Where(l => l.Type == Constants.Bgg.Mechanic && !string.IsNullOrWhiteSpace(l.Value))
            .Select(m => new GameMechanic(m.Value))
            .ToList();
        await _gameRepository.AddGameMechanicsIfNotExists(mechanics);

        var people = links
            .Where(l => l.Type is Constants.Bgg.Artist or Constants.Bgg.Designer or Constants.Bgg.Publisher)
            .Where(l => !string.IsNullOrWhiteSpace(l.Value))
            .Select(p => new Person(p.Value, p.Type.ToPersonTypeEnum()))
            .ToList();
        await _gameRepository.AddPeopleIfNotExists(people);

        var game = new Game(name, hasScoring, state);
        game.UpdateImage(imageUrl);
        game.UpdateDescription(item.Description ?? string.Empty);
        game.UpdateYearPublished(item.YearPublished);
        game.UpdatePlayerCount(minPlayers, maxPlayers);
        game.UpdatePlayTime(minPlayTime, maxPlayTime);
        game.UpdateMinAge(item.MinAge > 0 ? item.MinAge : null);
        game.UpdateRating(item.Statistics?.Ratings?.Average);
        game.UpdateWeight(item.Statistics?.Ratings?.AverageWeight);
        game.UpdateBggId(item.Id);
        game.UpdateBuyingPrice(price);
        game.UpdateShopUrl(shopUrl);

        if (additionDate.HasValue)
        {
            game.UpdateAdditionDate(additionDate.Value);
        }

        return game;
    }
}
