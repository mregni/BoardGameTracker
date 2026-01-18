using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;

namespace BoardGameTracker.Core.Compares;

public class PlayerComparisonService : IPlayerComparisonService
{
    private readonly ICompareRepository _compareRepository;
    private readonly IPlayerRepository _playerRepository;

    public PlayerComparisonService(ICompareRepository compareRepository, IPlayerRepository playerRepository)
    {
        _compareRepository = compareRepository;
        _playerRepository = playerRepository;
    }

    public async Task<PlayerComparison> ComparePlayersAsync(int playerOneId, int playerTwoId)
    {
        var sessionCountPlayerOne = await _playerRepository.GetTotalPlayCount(playerOneId);
        var sessionCountPlayerTwo = await _playerRepository.GetTotalPlayCount(playerTwoId);

        var durationPlayerOne = await _playerRepository.GetPlayLengthInMinutes(playerOneId);
        var durationPlayerTwo = await _playerRepository.GetPlayLengthInMinutes(playerTwoId);

        var winCountPlayerOne = await _playerRepository.GetTotalWinCount(playerOneId);
        var winCountPlayerTwo = await _playerRepository.GetTotalWinCount(playerTwoId);

        var winPercentagePlayerOne = CalculateWinPercentage(winCountPlayerOne, sessionCountPlayerOne);
        var winPercentagePlayerTwo = CalculateWinPercentage(winCountPlayerTwo, sessionCountPlayerTwo);

        var directWins = await _compareRepository.GetDirectWins(playerOneId, playerTwoId);
        var mostWonGame = await _compareRepository.GetMostWonGame(playerOneId, playerTwoId);

        return new PlayerComparison
        {
            PlayerOneId = playerOneId,
            PlayerTwoId = playerTwoId,
            SessionCounts = new CompareRow<int>(sessionCountPlayerOne, sessionCountPlayerTwo),
            TotalDuration = new CompareRow<double>(durationPlayerOne, durationPlayerTwo),
            WinCount = new CompareRow<int>(winCountPlayerOne, winCountPlayerTwo),
            WinPercentage = new CompareRow<double>(winPercentagePlayerOne, winPercentagePlayerTwo),
            DirectWins = directWins,
            MostWonGame = mostWonGame
        };
    }

    private static double CalculateWinPercentage(int winCount, int totalSessions)
    {
        return totalSessions > 0 ? (double)winCount / totalSessions * 100 : 0;
    }
}

