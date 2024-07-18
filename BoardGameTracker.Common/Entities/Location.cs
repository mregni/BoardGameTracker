using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Location: HasId
{
    public string Name { get; set; }
    public ICollection<Session> Sessions { get; set; }
}