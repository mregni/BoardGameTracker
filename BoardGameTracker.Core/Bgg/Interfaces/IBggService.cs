using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Bgg.Interfaces;

public interface IBggService
{
    Task<BggGame?> SearchGame(int id);
}