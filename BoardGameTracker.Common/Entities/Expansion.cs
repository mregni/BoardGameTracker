using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.ValueObjects;

namespace BoardGameTracker.Common.Entities;

public class Expansion : HasId
{
    private string _title = string.Empty;
    private GameState _state = GameState.Owned;

    public string Title
    {
        get => _title;
        private set => _title = Guard.Against.NullOrWhiteSpace(value);
    }

    public int? BggId { get; private set; }
    public string? Image { get; private set; }

    public GameState State
    {
        get => _state;
        private set => _state = Guard.Against.EnumOutOfRange(value);
    }

    public Price? BuyingPrice { get; private set; }
    public Price? SoldPrice { get; private set; }
    public DateTime? AdditionDate { get; private set; }
    public int? StoredAtLocationId { get; private set; }
    public int? StorageLocationId { get; private set; }
    public int StoragePosition { get; private set; }
    public BoxOrientation BoxOrientation { get; private set; }

    [JsonIgnore]
    public Game Game { get; private set; } = null!;
    public int GameId { get; private set; }
    public ICollection<Session> Sessions { get; private set; }

    public Expansion(string title, int? bggId, int gameId)
    {
        Title = title;
        BggId = bggId == null ? null : Guard.Against.NegativeOrZero(bggId.Value);
        GameId = Guard.Against.NegativeOrZero(gameId);
        AdditionDate = DateTime.UtcNow;
        Sessions = [];
    }

    public Expansion(string title, int? bggId, Game game)
    {
        Title = title;
        BggId = bggId == null ? null : Guard.Against.NegativeOrZero(bggId.Value);
        Game = Guard.Against.Null(game);
        GameId = game.Id;
        AdditionDate = DateTime.UtcNow;
        Sessions = [];
    }

    public bool Matches(Expansion other)
    {
        return BggId != null
            ? BggId == other.BggId
            : other.BggId == null && string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateImage(string? image)
    {
        Image = string.IsNullOrWhiteSpace(image) ? null : image;
    }

    public void UpdateState(GameState state)
    {
        State = state;
        if (!StorageLocation.CanStore(state))
        {
            ClearStorage();
        }
    }

    public void Store(int locationId, int? storageLocationId, int position)
    {
        if (!StorageLocation.CanStore(State))
        {
            throw new DomainException(Constants.Errors.StorageNotAllowedForState);
        }

        StoredAtLocationId = Guard.Against.NegativeOrZero(locationId);
        StorageLocationId = storageLocationId;
        StoragePosition = position;
    }

    public void ClearStorage()
    {
        StoredAtLocationId = null;
        StorageLocationId = null;
        StoragePosition = 0;
    }

    public void UpdateBoxOrientation(BoxOrientation orientation)
    {
        BoxOrientation = Guard.Against.EnumOutOfRange(orientation);
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
}
