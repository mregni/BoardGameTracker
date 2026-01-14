using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Person : HasId
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        private set => _name = Guard.Against.NullOrWhiteSpace(value, nameof(Name));
    }

    public PersonType Type { get; private set; }
    public ICollection<Game> Games { get; private set; }

    public Person(string name, PersonType type)
    {
        Name = name;
        Type = type;
        Games = new List<Game>();
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateType(PersonType type)
    {
        Type = type;
    }

    public int GetGameCount() => Games.Count;
}