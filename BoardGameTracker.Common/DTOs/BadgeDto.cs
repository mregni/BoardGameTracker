using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class BadgeDto
{
    public int Id { get; set; }
    public string DescriptionKey { get; set; } = string.Empty;
    public string TitleKey { get; set; } = string.Empty;
    public BadgeType Type { get; set; }
    public BadgeLevel? Level { get; set; }
    public string Image { get; set; } = string.Empty;
}

public static class BadgeDtoExtensions
{
    public static BadgeDto ToDto(this Badge badge)
    {
        return new BadgeDto
        {
            Id = badge.Id,
            DescriptionKey = badge.DescriptionKey,
            TitleKey = badge.TitleKey,
            Type = badge.Type,
            Level = badge.Level,
            Image = badge.Image
        };
    }

    public static List<BadgeDto> ToListDto(this IEnumerable<Badge> badges)
    {
        return badges.Select(ToDto).ToList();
    }
}
