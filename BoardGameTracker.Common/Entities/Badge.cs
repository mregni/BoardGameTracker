using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Badge : HasId
{
    private string _descriptionKey = string.Empty;
    private string _titleKey = string.Empty;

    public string DescriptionKey
    {
        get => _descriptionKey;
        private set => _descriptionKey = Guard.Against.NullOrWhiteSpace(value, nameof(DescriptionKey));
    }

    public string TitleKey
    {
        get => _titleKey;
        private set => _titleKey = Guard.Against.NullOrWhiteSpace(value, nameof(TitleKey));
    }

    public BadgeType Type { get; private set; }
    public BadgeLevel? Level { get; private set; }
    public string Image { get; private set; }

    public ICollection<Player> Players { get; private set; }

    internal Badge(string titleKey, string descriptionKey, BadgeType type, string image, BadgeLevel? level = null)
    {
        TitleKey = titleKey;
        DescriptionKey = descriptionKey;
        Type = type;
        Image = image;
        Level = level;
        Players = new List<Player>();
    }

    internal static Badge CreateWithId(int id, string titleKey, string descriptionKey, BadgeType type, string image, BadgeLevel? level = null)
    {
        return new Badge(titleKey, descriptionKey, type, image, level) { Id = id };
    }
}