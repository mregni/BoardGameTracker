using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Loans.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class LoansControllerTests
{
    private readonly Mock<ILoanService> _loanServiceMock;
    private readonly LoansController _controller;

    public LoansControllerTests()
    {
        _loanServiceMock = new Mock<ILoanService>();
        _controller = new LoansController(_loanServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _loanServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLoans_ShouldReturnOkWithLoans_WhenLoansExist()
    {
        // Arrange
        var loans = new List<Loan>
        {
            new Loan(1, 1, DateTime.UtcNow.AddDays(-5)) { Id = 1 },
            new Loan(2, 2, DateTime.UtcNow.AddDays(-3)) { Id = 2 }
        };

        _loanServiceMock
            .Setup(x => x.GetLoans())
            .ReturnsAsync(loans);

        // Act
        var result = await _controller.GetLoans();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLoans = okResult.Value.Should().BeAssignableTo<List<LoanDto>>().Subject;

        returnedLoans.Should().HaveCount(2);
        returnedLoans[0].Id.Should().Be(1);
        returnedLoans[0].GameId.Should().Be(1);
        returnedLoans[0].PlayerId.Should().Be(1);
        returnedLoans[1].Id.Should().Be(2);
        returnedLoans[1].GameId.Should().Be(2);
        returnedLoans[1].PlayerId.Should().Be(2);

        _loanServiceMock.Verify(x => x.GetLoans(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLoans_ShouldReturnOkWithEmptyList_WhenNoLoansExist()
    {
        // Arrange
        _loanServiceMock
            .Setup(x => x.GetLoans())
            .ReturnsAsync(new List<Loan>());

        // Act
        var result = await _controller.GetLoans();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLoans = okResult.Value.Should().BeAssignableTo<List<LoanDto>>().Subject;
        returnedLoans.Should().BeEmpty();

        _loanServiceMock.Verify(x => x.GetLoans(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLoanById_ShouldReturnOkWithLoan_WhenLoanExists()
    {
        // Arrange
        var loanId = 1;
        var loan = new Loan(1, 1, DateTime.UtcNow.AddDays(-5)) { Id = loanId };

        _loanServiceMock
            .Setup(x => x.GetLoanById(loanId))
            .ReturnsAsync(loan);

        // Act
        var result = await _controller.GetLoanById(loanId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLoan = okResult.Value.Should().BeAssignableTo<LoanDto>().Subject;

        returnedLoan.Id.Should().Be(loanId);
        returnedLoan.GameId.Should().Be(1);
        returnedLoan.PlayerId.Should().Be(1);

        _loanServiceMock.Verify(x => x.GetLoanById(loanId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLoanById_ShouldReturnNotFound_WhenLoanDoesNotExist()
    {
        // Arrange
        var loanId = 999;

        _loanServiceMock
            .Setup(x => x.GetLoanById(loanId))
            .ReturnsAsync((Loan?)null);

        // Act
        var result = await _controller.GetLoanById(loanId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _loanServiceMock.Verify(x => x.GetLoanById(loanId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnCreatedAtAction_WhenLoanIsCreated()
    {
        // Arrange
        var command = new CreateLoanCommand
        {
            GameId = 1,
            PlayerId = 1,
            LoanDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        var createdLoan = new Loan(command.GameId, command.PlayerId, command.LoanDate) { Id = 1 };

        _loanServiceMock
            .Setup(x => x.LoanGameToPlayer(command))
            .ReturnsAsync(createdLoan);

        // Act
        var result = await _controller.CreateLoan(command);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(LoansController.GetLoanById));
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues!["id"].Should().Be(1);

        var returnedLoan = createdResult.Value.Should().BeAssignableTo<LoanDto>().Subject;
        returnedLoan.Id.Should().Be(1);
        returnedLoan.GameId.Should().Be(command.GameId);
        returnedLoan.PlayerId.Should().Be(command.PlayerId);

        _loanServiceMock.Verify(x => x.LoanGameToPlayer(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.CreateLoan(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLoan_ShouldReturnOkWithUpdatedLoan_WhenLoanIsUpdated()
    {
        // Arrange
        var command = new UpdateLoanCommand
        {
            Id = 1,
            GameId = 1,
            PlayerId = 1,
            LoanDate = DateTime.UtcNow.AddDays(-5),
            DueDate = DateTime.UtcNow.AddDays(25)
        };

        var updatedLoan = new Loan(command.GameId, command.PlayerId, command.LoanDate) { Id = command.Id };

        _loanServiceMock
            .Setup(x => x.Update(command))
            .ReturnsAsync(updatedLoan);

        // Act
        var result = await _controller.UpdateLoan(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLoan = okResult.Value.Should().BeAssignableTo<LoanDto>().Subject;

        returnedLoan.Id.Should().Be(command.Id);
        returnedLoan.GameId.Should().Be(command.GameId);
        returnedLoan.PlayerId.Should().Be(command.PlayerId);

        _loanServiceMock.Verify(x => x.Update(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLoan_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.UpdateLoan(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnLoan_ShouldReturnOkWithUpdatedLoan_WhenLoanIsReturned()
    {
        // Arrange
        var command = new ReturnLoanCommand
        {
            Id = 1,
            ReturnDate = DateTime.UtcNow
        };

        var returnedLoan = new Loan(1, 1, DateTime.UtcNow.AddDays(-5)) { Id = command.Id };
        returnedLoan.MarkAsReturned(command.ReturnDate);

        _loanServiceMock
            .Setup(x => x.ReturnLoan(command))
            .ReturnsAsync(returnedLoan);

        // Act
        var result = await _controller.ReturnLoan(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var loan = okResult.Value.Should().BeAssignableTo<LoanDto>().Subject;

        loan.Id.Should().Be(command.Id);
        loan.ReturnedDate.Should().Be(command.ReturnDate);

        _loanServiceMock.Verify(x => x.ReturnLoan(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnLoan_ShouldReturnNotFound_WhenLoanDoesNotExist()
    {
        // Arrange
        var command = new ReturnLoanCommand
        {
            Id = 999,
            ReturnDate = DateTime.UtcNow
        };

        _loanServiceMock
            .Setup(x => x.ReturnLoan(command))
            .ReturnsAsync((Loan?)null);

        // Act
        var result = await _controller.ReturnLoan(command);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _loanServiceMock.Verify(x => x.ReturnLoan(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnLoan_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.ReturnLoan(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteLoan_ShouldReturnNoContent_WhenLoanIsDeleted()
    {
        // Arrange
        var loanId = 1;
        var existingLoan = new Loan(1, 1, DateTime.UtcNow.AddDays(-5)) { Id = loanId };

        _loanServiceMock
            .Setup(x => x.GetLoanById(loanId))
            .ReturnsAsync(existingLoan);

        _loanServiceMock
            .Setup(x => x.Delete(loanId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteLoan(loanId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _loanServiceMock.Verify(x => x.GetLoanById(loanId), Times.Once);
        _loanServiceMock.Verify(x => x.Delete(loanId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteLoan_ShouldReturnNotFound_WhenLoanDoesNotExist()
    {
        // Arrange
        var loanId = 999;

        _loanServiceMock
            .Setup(x => x.GetLoanById(loanId))
            .ReturnsAsync((Loan?)null);

        // Act
        var result = await _controller.DeleteLoan(loanId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _loanServiceMock.Verify(x => x.GetLoanById(loanId), Times.Once);
        VerifyNoOtherCalls();
    }
}
