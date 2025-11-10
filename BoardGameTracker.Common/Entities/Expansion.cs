using System.Text.Json.Serialization;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Expansion : HasId
{
    public string Title { get; set; } = string.Empty;
    public int BggId { get; set; }
    [JsonIgnore]
    public Game Game { get; set; } = null!;
    public int? GameId { get; set; }
    public ICollection<Session> Sessions { get; set; } = [];
}