using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.DTOs;

public class LoanDto
{
    public int Id { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public bool IsActive { get; set; }
}

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
