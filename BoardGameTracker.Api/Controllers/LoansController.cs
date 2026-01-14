using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Loans.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLoans()
    {
        var loans = await _loanService.GetLoans();
        var boe = loans.ToListDto();
        return Ok(boe);
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
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var createdLoan = await _loanService.LoanGameToPlayer(command);
        return CreatedAtAction(nameof(GetLoanById), new { id = createdLoan.Id }, createdLoan.ToDto());
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLoan([FromBody] UpdateLoanCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var updatedLoan = await _loanService.Update(command);
        return Ok(updatedLoan.ToDto());
    }
    
    [HttpPut("return")]
    public async Task<IActionResult> ReturnLoan([FromBody] ReturnLoanCommand? command)
    {
        if (command == null)
        {
            return BadRequest();
        }

        var updatedLoan = await _loanService.ReturnLoan(command);
        if (updatedLoan == null)
        {
            return NotFound();
        }
        
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
