using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class ExpansionDtoExtensions
{
    public static ExpansionDto ToDto(this Expansion expansion)
    {
        return new ExpansionDto
        {
            Id = expansion.Id,
            Title = expansion.Title,
            BggId = expansion.BggId,
            GameId = expansion.GameId,
            Image = expansion.Image,
            State = expansion.State,
            BuyingPrice = expansion.BuyingPrice?.Amount,
            SoldPrice = expansion.SoldPrice?.Amount,
            AdditionDate = expansion.AdditionDate,
            StoredAtLocationId = expansion.StoredAtLocationId,
            StorageLocationId = expansion.StorageLocationId,
            StoragePosition = expansion.StoragePosition,
            BoxOrientation = expansion.BoxOrientation
        };
    }

    public static List<ExpansionDto> ToListDto(this IEnumerable<Expansion> expansions)
    {
        return expansions.Select(e => e.ToDto()).ToList();
    }
}
