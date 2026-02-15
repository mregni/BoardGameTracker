using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class LoanDtoExtensions
{
    public static LoanDto ToDto(this Loan loan)
    {
        return new LoanDto
        {
            Id = loan.Id,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnedDate = loan.ReturnedDate,
            GameId = loan.GameId,
            PlayerId = loan.PlayerId
        };
    }

    public static List<LoanDto> ToListDto(this IEnumerable<Loan> loans)
    {
        return loans.Select(l => l.ToDto()).ToList();
    }
}
