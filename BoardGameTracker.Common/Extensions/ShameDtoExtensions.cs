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
            LastSessionDate = shameGame.LastSessionDate,
            PlayCount = shameGame.PlayCount,
            MinPlayers = shameGame.MinPlayers,
            MaxPlayers = shameGame.MaxPlayers,
            MinPlayTime = shameGame.MinPlayTime,
            MaxPlayTime = shameGame.MaxPlayTime,
            PlannedGameNightId = shameGame.PlannedGameNightId,
            PlannedFor = shameGame.PlannedFor
        };
    }

    public static ShameStatisticsDto ToDto(this ShameStatistics statistics)
    {
        return new ShameStatisticsDto
        {
            Count = statistics.Count,
            TotalValue = statistics.TotalValue,
            AverageValue = statistics.AverageValue,
            PricedGameCount = statistics.PricedGameCount,
            Months = statistics.Months,
            OwnedCount = statistics.OwnedCount,
            CollectionValue = statistics.CollectionValue,
            NeverPlayedCount = statistics.NeverPlayedCount,
            NeverPlayedValue = statistics.NeverPlayedValue,
            PlannedCount = statistics.PlannedCount,
            NextGameNight = statistics.NextGameNight == null
                ? null
                : new ShameGameNightDto
                {
                    Id = statistics.NextGameNight.Id,
                    Title = statistics.NextGameNight.Title,
                    StartDate = statistics.NextGameNight.StartDate
                }
        };
    }

    public static List<ShameDto> ToListDto(this IEnumerable<ShameGame> shameGames)
    {
        return shameGames.Select(ToDto).ToList();
    }
}
