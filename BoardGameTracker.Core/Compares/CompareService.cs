using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Compares;

public class CompareService : ICompareService
{
    private readonly ICompareRepository _compareRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ILogger<CompareService> _logger;

    public CompareService(ICompareRepository compareRepository, IPlayerRepository playerRepository, ILogger<CompareService> logger)
    {
        _compareRepository = compareRepository;
        _playerRepository = playerRepository;
        _logger = logger;
    }

    public async Task<CompareResultDto> GetPlayerComparison(int playerOne, int playerTwo)
    {
        _logger.LogDebug("Comparing players {PlayerOneId} and {PlayerTwoId}", playerOne, playerTwo);
        var sessionCountPlayerOne = await _playerRepository.GetTotalPlayCount(playerOne);
        var sessionCountPlayerTwo = await _playerRepository.GetTotalPlayCount(playerTwo);

        var durationPlayerOne = await _playerRepository.GetPlayLengthInMinutes(playerOne);
        var durationPlayerTwo = await _playerRepository.GetPlayLengthInMinutes(playerTwo);

        var winCountPlayerOne = await _playerRepository.GetTotalWinCount(playerOne);
        var winCountPlayerTwo = await _playerRepository.GetTotalWinCount(playerTwo);

        var winPercentagePlayerOne = sessionCountPlayerOne > 0 ? (double)winCountPlayerOne / sessionCountPlayerOne : 0;
        var winPercentagePlayerTwo = sessionCountPlayerTwo > 0 ? (double)winCountPlayerTwo / sessionCountPlayerTwo : 0;

        var sessionsTogether = await _compareRepository.GetSessionsTogether(playerOne, playerTwo);

        var result = new CompareResultDto
        {
            SessionCounts = new CompareRow<int>(sessionCountPlayerOne, sessionCountPlayerTwo),
            TotalDuration = new CompareRow<double>(durationPlayerOne, durationPlayerTwo),
            WinCount = new CompareRow<int>(winCountPlayerOne, winCountPlayerTwo),
            WinPercentage = new CompareRow<double>(winPercentagePlayerOne, winPercentagePlayerTwo)
        };
        HeadToHeadCalculator.Fill(result, sessionsTogether, playerOne, playerTwo);

        return result;
    }

    public Task<ComparePairDto?> GetDefaultPair()
    {
        return _compareRepository.GetPairWithMostSessions();
    }
}
