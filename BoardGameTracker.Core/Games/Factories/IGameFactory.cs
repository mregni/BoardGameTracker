using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Games.Factories;

public interface IGameFactory
{
    Task<Game> CreateFromBggAsync(ThingResponse.Item item, bool hasScoring, GameState state, decimal? price, DateTime? additionDate);
}
