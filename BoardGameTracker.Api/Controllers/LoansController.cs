using AutoMapper;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Loans.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameTracker.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly ILoanService _loanService;

    public LoansController(IMapper mapper, ILoanService loanService)
    {
        _mapper = mapper;
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLoans()
    {
        var loans = await _loanService.GetLoans();
        return Ok(loans);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanById(int id)
    {
        var loan = await _loanService.GetLoanById(id);
        if (loan == null)
        {
            return NotFound();
        }
        return Ok(loan);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanViewModel? createLoan)
    {
        if (createLoan == null)
        {
            return BadRequest();
        }

        var loan = _mapper.Map<Loan>(createLoan);
        var createdLoan = await _loanService.Create(loan);
        return CreatedAtAction(nameof(GetLoanById), new { id = createdLoan.Id }, createdLoan);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLoan([FromBody] LoanViewModel? updateLoan)
    {
        if (updateLoan == null)
        {
            return BadRequest();
        }

        var existingLoan = await _loanService.GetLoanById(updateLoan.Id);
        if (existingLoan == null)
        {
            return NotFound();
        }

        var loan = _mapper.Map<Loan>(updateLoan);
        var updatedLoan = await _loanService.Update(loan);
        return Ok(updatedLoan);
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