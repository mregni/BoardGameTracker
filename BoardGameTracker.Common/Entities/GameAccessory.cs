using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class GameAccessory : HasId
{
    public string Title { get; set; }
    public string Description { get; set; }
    public Game Game { get; set; }
    public int GameId { get; set; }
}