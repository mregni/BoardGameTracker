using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Common.Extensions;

public static class ShameDtoExtensions
{
    public static ShameDto ToDto(this ShameGame shameGame)
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

    public static List<ShameDto> ToDtoList(this IEnumerable<ShameGame> shameGames)
    {
        return shameGames.Select(ToDto).ToList();
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
}
