using BoardGameTracker.Common.Models.Bgg;
using Refit;

namespace BoardGameTracker.Core.Bgg;

public interface IBggApi
{
    [Get("/thing?type=boardgame")]
    public Task<ApiResponse<BggApiGames>> SearchGame([Query] int id, [Query] int stats);

    [Get("/thing?type=boardgameexpansion")]
    public Task<ApiResponse<BggApiGames>> SearchExpansion([Query] int id, [Query] int stats);

    [Get("/collection?brief=0&stats=0")]
    public Task<ApiResponse<BggApiCollection>> ImportCollection([Query] string username,
        [Query, AliasAs("subtype")] string subType, [Query, AliasAs("excludesubtype")] string? excludeSubType);
}