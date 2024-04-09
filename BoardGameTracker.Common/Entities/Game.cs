﻿using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Game : BaseGame
{
    public bool HasScoring { get; set; }
    public ICollection<Expansion> Expansions { get; set; }
    public ICollection<GameAccessory> Accessories { get; set; }
    public ICollection<GameCategory> Categories { get; set; }
    public ICollection<GameMechanic> Mechanics { get; set; }
    public ICollection<Person> People { get; set; }
}