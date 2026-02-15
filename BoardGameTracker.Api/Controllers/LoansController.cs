using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Loans.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoanController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoanController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLoans()
    {
        var loans = await _loanService.GetLoans();
        return Ok(loans.ToListDto());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanById(int id)
    {
        var loan = await _loanService.GetLoanById(id);
        if (loan == null)
        {
            return NotFound();
        }

        return Ok(loan.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanCommand command)
    {
        var createdLoan = await _loanService.LoanGameToPlayer(command);
        return CreatedAtAction(nameof(GetLoanById), new { id = createdLoan.Id }, createdLoan.ToDto());
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLoan([FromBody] UpdateLoanCommand command)
    {
        var updatedLoan = await _loanService.Update(command);
        return Ok(updatedLoan.ToDto());
    }

    [HttpPut("return")]
    public async Task<IActionResult> ReturnLoan([FromBody] ReturnLoanCommand command)
    {
        var updatedLoan = await _loanService.ReturnLoan(command);
        return Ok(updatedLoan.ToDto());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLoan(int id)
    {
        var existingLoan = await _loanService.GetLoanById(id);
        if (existingLoan == null)
        {
            return NotFound();
        }

        await _loanService.Delete(id);
        return NoContent();
    }
}
