﻿using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.ViewModels;

public class SessionViewModel
{
    public int? Id { get; set; }
    public string? Comment { get; set; }
    public int GameId { get; set; }
    public List<PlayerSessionViewModel> PlayerSessions { get; set; }
    public DateTime Start { get; set; }
    public double Minutes { get; set; }
    public int? LocationId  { get; set; }
    public List<SessionFlag>? Flags { get; set; }
}

public class PlayerSessionViewModel
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public bool Won { get; set; }
    public bool FirstPlay { get; set; }
    public double? Score { get; set; }
}