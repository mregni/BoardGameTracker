using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Image: HasId
{
    public string Path { get; set; }
    public int? GamePlayId { get; set; }
    public Play? Play { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; }
}