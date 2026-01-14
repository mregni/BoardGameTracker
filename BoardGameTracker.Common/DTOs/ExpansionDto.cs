using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.DTOs;

public class ExpansionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BggId { get; set; }
    public int? GameId { get; set; }
}

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
