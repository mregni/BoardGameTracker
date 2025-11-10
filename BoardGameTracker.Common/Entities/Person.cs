using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Person : HasId
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public PersonType Type { get; set; }
    public ICollection<Game> Games { get; set; } = [];
}