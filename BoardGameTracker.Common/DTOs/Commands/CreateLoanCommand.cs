using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateLoanCommand
{
    [Range(1, int.MaxValue)]
    public int GameId { get; set; }

    [Range(1, int.MaxValue)]
    public int PlayerId { get; set; }

    public DateTime LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateLoanCommand : CreateLoanCommand
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}

public class ReturnLoanCommand
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    public DateTime ReturnDate { get; set; }
}
