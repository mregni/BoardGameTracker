using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Player : HasId
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        private set => _name = Guard.Against.NullOrWhiteSpace(value, nameof(Name));
    }

    public string? Image { get; private set; }
    public ICollection<PlayerSession> PlayerSessions { get; private set; }
    public ICollection<Badge> Badges { get; private set; }
    public ICollection<Loan> Loans { get; private set; }

    public Player(string name, string? image = null)
    {
        Name = name;
        Image = image;
        PlayerSessions = new List<PlayerSession>();
        Badges = new List<Badge>();
        Loans = new List<Loan>();
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateImage(string? imageUrl)
    {
        Image = imageUrl;
    }
}