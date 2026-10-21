using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Player : HasId
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        private set => _name = Guard.Against.NullOrWhiteSpace(value);
    }

    public string? Image { get; private set; }
    public PlayerImageSource ImageSource { get; private set; }
    public string? Email { get; private set; }
    public ICollection<PlayerSession> PlayerSessions { get; private set; }
    public ICollection<Badge> Badges { get; private set; }
    public ICollection<Loan> Loans { get; private set; }
    public ICollection<GameNightRsvp> GameNightRsvps { get; private set; }

    public Player(string name, string? image = null, string? email = null)
    {
        Name = name;
        UpdateImage(image);
        Email = email;
        PlayerSessions = new List<PlayerSession>();
        Badges = new List<Badge>();
        Loans = new List<Loan>();
        GameNightRsvps = new List<GameNightRsvp>();
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateImage(string? imageUrl)
    {
        Image = string.IsNullOrEmpty(imageUrl) ? null : imageUrl;
        ImageSource = Image == null ? PlayerImageSource.None : PlayerImageSource.Upload;
    }

    public bool FollowsAccountImage => ImageSource != PlayerImageSource.Upload;

    public void UseAccountImage(string image)
    {
        Image = image;
        ImageSource = PlayerImageSource.Account;
    }

    public void ClearAccountImage()
    {
        if (ImageSource != PlayerImageSource.Account)
        {
            return;
        }

        Image = null;
        ImageSource = PlayerImageSource.None;
    }

    public void UpdateEmail(string? email)
    {
        Email = email;
    }
}