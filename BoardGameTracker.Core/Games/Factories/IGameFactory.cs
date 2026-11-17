using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Games.Factories;

public interface IGameFactory
{
    Task<Game> CreateFromBggAsync(ThingResponse.Item item, bool hasScoring, GameState state, decimal? price, DateTime? additionDate, string? shopUrl = null);
    Task<Expansion> CreateExpansionFromBggAsync(ThingResponse.Item item, Game game, GameState state = GameState.Owned, decimal? price = null, DateTime? additionDate = null);
}
