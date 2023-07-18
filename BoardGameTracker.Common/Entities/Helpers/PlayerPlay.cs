using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities.Helpers;

public class PlayerPlay: HasId
{
    public int? PlayerId { get; set; }
    public Player Player { get; set; }
    public int PlayId { get; set; }
    public Play Play { get; set; }

    public double? Score { get; set; }
    public bool IsBot { get; set; }
    public WonState Result { get; set; }
}