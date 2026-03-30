using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Common.Extensions;

public static class ShameDtoExtensions
{
    private static ShameDto ToDto(this ShameGame shameGame)
    {
        return new ShameDto
        {
            Id = shameGame.Id,
            Title = shameGame.Title,
            Image = shameGame.Image,
            AdditionDate = shameGame.AdditionDate,
            Price = shameGame.Price,
            LastSessionDate = shameGame.LastSessionDate
        };
    }

    public static ShameStatisticsDto ToDto(this ShameStatistics statistics)
    {
        return new ShameStatisticsDto
        {
            Count = statistics.Count,
            TotalValue = statistics.TotalValue,
            AverageValue = statistics.AverageValue
        };
    }

    public static List<ShameDto> ToListDto(this IEnumerable<ShameGame> shameGames)
    {
        return shameGames.Select(ToDto).ToList();
    }
}
