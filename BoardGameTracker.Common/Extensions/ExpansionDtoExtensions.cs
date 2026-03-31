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
            GameId = expansion.GameId
        };
    }

    public static List<ExpansionDto> ToListDto(this IEnumerable<Expansion> expansions)
    {
        return expansions.Select(e => e.ToDto()).ToList();
    }
}
