using BoardGameTracker.Common.Models.Bgg;
using Refit;

namespace BoardGameTracker.Core.Bgg.Interfaces;

public interface IBggApi
{
    [Get("/thing?type=boardgame")]
    Task<ApiResponse<BggApiGames>> SearchGame([Query] int id, [Query] int stats, CancellationToken cancellationToken = default);

    [Get("/thing?type=boardgameexpansion")]
    Task<ApiResponse<BggApiGames>> SearchExpansion([Query] int id, [Query] int stats, CancellationToken cancellationToken = default);

    [Get("/collection?brief=0&stats=0")]
    Task<ApiResponse<BggApiCollection>> ImportCollection([Query] string username,
        [Query, AliasAs("subtype")] string subType, [Query, AliasAs("excludesubtype")] string? excludeSubType,
        CancellationToken cancellationToken = default);
}