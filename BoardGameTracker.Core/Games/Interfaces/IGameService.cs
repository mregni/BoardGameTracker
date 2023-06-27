using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameService
{
    Task<Game> ProcessBggGameData(BggGame rawGame);
    Task<Game?> GetGameByBggId(int bggId);
}