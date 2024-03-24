using BoardGameTracker.Common.Models.Bgg;
using Refit;

namespace BoardGameTracker.Core.Bgg;

public interface IBggApi
{
    [Get("/thing")]
    public Task<ApiResponse<BggApiGames>> SearchGame([Query] int id, [Query] string type, [Query] int stats);
}