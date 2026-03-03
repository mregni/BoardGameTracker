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
