using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Compares.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;

namespace BoardGameTracker.Core.Compares;

public class CompareService : ICompareService
{
    private readonly ICompareRepository _compareRepository;
    private readonly IPlayerRepository _playerRepository;
    

    public CompareService(ICompareRepository compareRepository, IPlayerRepository playerRepository)
    {
        _compareRepository = compareRepository;
        _playerRepository = playerRepository;
    }

    public async Task<CompareResult> GetPlayerComparisation(int playerOne, int playerTwo)
    {
        var sessionCountPlayerOne = await _playerRepository.GetTotalPlayCount(playerOne);
        var sessionCountPlayerTwo = await _playerRepository.GetTotalPlayCount(playerTwo);
        
        var durationPlayerOne = await _playerRepository.GetPlayLengthInMinutes(playerOne);
        var durationPlayerTwo = await _playerRepository.GetPlayLengthInMinutes(playerTwo);

        var winCountPlayerOne = await _playerRepository.GetTotalWinCount(playerOne);
        var winCountPlayerTwo = await _playerRepository.GetTotalWinCount(playerTwo);
        
        var totalSessionPlayerOne = await _playerRepository.GetTotalPlayCount(playerOne);
        var totalSessionPlayerTwo = await _playerRepository.GetTotalPlayCount(playerTwo);
        
        var winPercentagePlayerOne = totalSessionPlayerOne > 0 ? (double)winCountPlayerOne / totalSessionPlayerOne : 0;
        var winPercentagePlayerTwo = totalSessionPlayerTwo > 0 ? (double)winCountPlayerTwo / totalSessionPlayerTwo : 0;
        
        var result = new CompareResult
        {
            DirectWins = await _compareRepository.GetDirectWins(playerOne, playerTwo),
            MostWonGame = await _compareRepository.GetMostWonGame(playerOne, playerTwo),
            SessionCounts = new CompareRow<int>(sessionCountPlayerOne, sessionCountPlayerTwo),
            TotalDuration = new CompareRow<double>(durationPlayerOne, durationPlayerTwo),
            WinCount = new CompareRow<int>(winCountPlayerOne, winCountPlayerTwo),
            WinPercentageCount = new CompareRow<double>(winPercentagePlayerOne, winPercentagePlayerTwo)
        };

        return result;
    }
}