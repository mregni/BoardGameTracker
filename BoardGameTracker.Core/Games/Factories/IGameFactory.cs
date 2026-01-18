using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Bgg;

namespace BoardGameTracker.Core.Games.Factories;

public interface IGameFactory
{
    Task<Game> CreateFromImportDataAsync(GameImportData data, bool hasScoring, GameState state, decimal? price, DateTime? additionDate);
}
