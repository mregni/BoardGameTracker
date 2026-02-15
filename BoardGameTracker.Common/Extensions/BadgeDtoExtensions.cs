using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

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
