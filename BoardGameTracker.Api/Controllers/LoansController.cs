using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common;
using BoardGameTracker.Core.Loans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/loans")]
[Authorize]
public class LoanController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoanController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    [ProducesResponseType<List<LoanDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoans()
    {
        var loans = await _loanService.GetLoans();
        return Ok(loans.ToListDto());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
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
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanCommand command)
    {
        var createdLoan = await _loanService.LoanGameToPlayer(command);
        return CreatedAtAction(nameof(GetLoanById), new { id = createdLoan.Id }, createdLoan.ToDto());
    }

    [HttpPut]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLoan([FromBody] UpdateLoanCommand command)
    {
        var updatedLoan = await _loanService.Update(command);
        return Ok(updatedLoan.ToDto());
    }

    [HttpPut("return")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReturnLoan([FromBody] ReturnLoanCommand command)
    {
        var updatedLoan = await _loanService.ReturnLoan(command);
        return Ok(updatedLoan.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Constants.AuthRoles.UserOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
