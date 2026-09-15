using System.Globalization;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Models;
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
        {
            throw new InvalidOperationException("Game must have a valid name from BGG");
        }

        int? minPlayers = item.MinPlayers > 0 ? item.MinPlayers : null;
        int? maxPlayers = item.MaxPlayers > 0 ? item.MaxPlayers : null;
        if (minPlayers.HasValue && maxPlayers.HasValue && minPlayers > maxPlayers)
        {
            throw new InvalidOperationException($"Invalid player count range from BGG: {minPlayers}-{maxPlayers}");
        }

        int? minPlayTime = item.MinPlayingTime > 0 ? item.MinPlayingTime : null;
        int? maxPlayTime = item.MaxPlayingTime > 0 ? item.MaxPlayingTime : null;
        minPlayTime ??= maxPlayTime;
        maxPlayTime ??= minPlayTime;
        if (minPlayTime.HasValue && maxPlayTime.HasValue && minPlayTime > maxPlayTime)
        {
            throw new InvalidOperationException($"Invalid play time range from BGG: {minPlayTime}-{maxPlayTime}");
        }

        var links = item.Links ?? [];

        var imageUrl = await _imageService.DownloadImage(
            item.Image ?? string.Empty,
            item.Id.ToString(CultureInfo.InvariantCulture));

        var namedLinks = links.Where(l => !string.IsNullOrWhiteSpace(l.Value)).ToList();

        var categories = await _gameRepository.GetOrCreateCategoriesAsync(namedLinks
            .Where(l => l.Type == Constants.Bgg.Category)
            .Select(l => l.Value));

        var mechanics = await _gameRepository.GetOrCreateMechanicsAsync(namedLinks
            .Where(l => l.Type == Constants.Bgg.Mechanic)
            .Select(l => l.Value));

        var people = await _gameRepository.GetOrCreatePeopleAsync(namedLinks
            .Where(l => l.Type is Constants.Bgg.Artist or Constants.Bgg.Designer or Constants.Bgg.Publisher)
            .Select(l => new PersonKey(l.Value, l.Type.ToPersonTypeEnum())));

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

        foreach (var category in categories)
        {
            game.AddCategory(category);
        }

        foreach (var mechanic in mechanics)
        {
            game.AddMechanic(mechanic);
        }

        foreach (var person in people)
        {
            game.AddPerson(person);
        }

        if (additionDate.HasValue)
        {
            game.UpdateAdditionDate(additionDate.Value);
        }

        return game;
    }
}
