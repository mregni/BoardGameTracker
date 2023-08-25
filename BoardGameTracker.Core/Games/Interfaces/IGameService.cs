﻿using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameService
{
    Task<Game> ProcessBggGameData(BggGame rawGame, GameState gameState);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGames();
    Task<Game?> GetGameById(int id);
    Task Delete(int id);
    Task<List<Play>> GetPlays(int id);
    Task<GameStatistics> GetStats(int id);
}