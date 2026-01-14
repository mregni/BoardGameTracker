using System.ComponentModel.DataAnnotations.Schema;
using Ardalis.GuardClauses;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ValueObjects;

namespace BoardGameTracker.Common.Entities.Helpers;

public abstract class BaseGame : HasId
{
    private string _title = string.Empty;
    private GameState _state;

    public string Title
    {
        get => _title;
        private set => _title = Guard.Against.NullOrWhiteSpace(value, nameof(Title));
    }

    public int? YearPublished { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Image { get; private set; }

    public PlayerCountRange? PlayerCount { get; private set; }
    public PlayTimeRange? PlayTime { get; private set; }

    public int? MinAge { get; private set; }
    public Rating? Rating { get; private set; }
    public Weight? Weight { get; private set; }
    public int? BggId { get; private set; }

    public GameState State
    {
        get => _state;
        private set => _state = Guard.Against.EnumOutOfRange(value, nameof(State));
    }

    [NotMapped] public bool IsLoaned => IsCurrentlyLoaned();

    public Price? BuyingPrice { get; private set; }
    public Price? SoldPrice { get; private set; }
    public DateTime? AdditionDate { get; private set; }
    public ICollection<Session> Sessions { get; private set; } = [];
    public ICollection<Loan> Loans { get; private set; } = [];

    protected BaseGame()
    {
        // For EF Core
    }

    protected BaseGame(string title, GameState state = GameState.Owned)
    {
        Title = title;
        State = state;
        AdditionDate = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
    }

    public void UpdateImage(string? imageUrl)
    {
        Image = imageUrl;
    }

    public void UpdatePlayerCount(int? minPlayers, int? maxPlayers)
    {
        if (minPlayers.HasValue && maxPlayers.HasValue)
        {
            PlayerCount = new PlayerCountRange(minPlayers.Value, maxPlayers.Value);
        }
        else
        {
            PlayerCount = null;
        }
    }

    public void UpdatePlayTime(int? minPlayTime, int? maxPlayTime)
    {
        if (minPlayTime.HasValue && maxPlayTime.HasValue)
        {
            PlayTime = new PlayTimeRange(minPlayTime.Value, maxPlayTime.Value);
        }
        else
        {
            PlayTime = null;
        }
    }

    public void UpdateMinAge(int? minAge)
    {
        if (minAge.HasValue)
        {
            Guard.Against.NegativeOrZero(minAge.Value, nameof(minAge));
        }
        MinAge = minAge;
    }

    public void UpdateRating(double? rating)
    {
        Rating = rating.HasValue ? new Rating(rating.Value) : null;
    }

    public void UpdateWeight(double? weight)
    {
        Weight = weight.HasValue ? new Weight(weight.Value) : null;
    }

    public void UpdateYearPublished(int? year)
    {
        if (year.HasValue)
        {
            Guard.Against.OutOfRange(year.Value, nameof(year), 1900, DateTime.UtcNow.Year + 5);
        }
        YearPublished = year;
    }

    public void UpdateBggId(int? bggId)
    {
        if (bggId.HasValue)
        {
            Guard.Against.NegativeOrZero(bggId.Value, nameof(bggId));
        }
        BggId = bggId;
    }

    public void UpdateState(GameState newState)
    {
        State = newState;
    }

    public void UpdateBuyingPrice(decimal? price)
    {
        BuyingPrice = price.HasValue ? new Price(price.Value) : null;
    }

    public void UpdateSoldPrice(decimal? price)
    {
        SoldPrice = price.HasValue ? new Price(price.Value) : null;
    }

    public void UpdateAdditionDate(DateTime? date)
    {
        AdditionDate = date;
    }

    public bool IsCurrentlyLoaned() => Loans.Any(l => l.IsCurrentlyOnLoan());
}