using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Common.Entities;

public class GameCategory : HasId
{
    public string Name { get; set; }
    public ICollection<Game> Games { get; set; }
}