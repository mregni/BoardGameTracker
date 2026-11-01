using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Common.Extensions;

public static class GamePlaySummaryDtoExtensions
{
    public static List<GamePlaySummaryDto> ToListDto(this IEnumerable<GamePlaySummary> summaries)
    {
        return summaries
            .Select(x => new GamePlaySummaryDto
            {
                GameId = x.GameId,
                PlayCount = x.PlayCount,
                LastPlayed = x.LastPlayed
            })
            .ToList();
    }
}
