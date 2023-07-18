﻿using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Play: HasId
{
    public string Comment { get; set; }
    public bool Ended { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; }
    public ICollection<Expansion> Expansions { get; set; }
    public int? LocationId { get; set; }
    public Location Location { get; set; }
    public ICollection<GameSession> Sessions { get; set; }
    public ICollection<Image> ExtraImages { get; set; }
    public ICollection<PlayerPlay> Players { get; set; }
}