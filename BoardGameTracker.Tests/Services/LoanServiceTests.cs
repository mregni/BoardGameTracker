using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Loans;
using BoardGameTracker.Core.Loans.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly LoanService _loanService;

    public LoanServiceTests()
    {
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _loanService = new LoanService(
            _loanRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _loanRepositoryMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    #region GetLoans Tests

    [Fact]
    public async Task GetLoans_ShouldReturnAllLoans_WhenLoansExist()
    {
        // Arrange
        var loans = new List<Loan>
        {
            new Loan(1, 1, DateTime.UtcNow.AddDays(-10)) { Id = 1 },
            new Loan(2, 2, DateTime.UtcNow.AddDays(-5)) { Id = 2 }
        };

        _loanRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(loans);

        // Act
        var result = await _loanService.GetLoans();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(loans);

        _loanRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLoans_ShouldReturnEmptyList_WhenNoLoansExist()
    {
        // Arrange
        _loanRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Loan>());

        // Act
        var result = await _loanService.GetLoans();

        // Assert
        result.Should().BeEmpty();

        _loanRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetLoanById Tests

    [Fact]
    public async Task GetLoanById_ShouldReturnLoan_WhenLoanExists()
    {
        // Arrange
        var loanId = 1;
        var loan = new Loan(1, 1, DateTime.UtcNow.AddDays(-10)) { Id = loanId };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(loanId))
            .ReturnsAsync(loan);

        // Act
        var result = await _loanService.GetLoanById(loanId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(loanId);

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLoanById_ShouldReturnNull_WhenLoanDoesNotExist()
    {
        // Arrange
        var loanId = 999;

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(loanId))
            .ReturnsAsync((Loan?)null);

        // Act
        var result = await _loanService.GetLoanById(loanId);

        // Assert
        result.Should().BeNull();

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region LoanGameToPlayer Tests

    [Fact]
    public async Task LoanGameToPlayer_ShouldCreateLoan_WhenGameExists()
    {
        // Arrange
        var gameId = 1;
        var playerId = 2;
        var loanDate = DateTime.UtcNow;
        var dueDate = DateTime.UtcNow.AddDays(14);

        var game = new Game("Test Game") { Id = gameId };
        var command = new CreateLoanCommand
        {
            GameId = gameId,
            PlayerId = playerId,
            LoanDate = loanDate,
            DueDate = dueDate
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _loanRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Loan>()))
            .ReturnsAsync((Loan l) => l);

        _gameRepositoryMock
            .Setup(x => x.UpdateAsync(game))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.LoanGameToPlayer(command);

        // Assert
        result.Should().NotBeNull();
        result.GameId.Should().Be(gameId);
        result.PlayerId.Should().Be(playerId);
        result.LoanDate.Should().Be(loanDate);
        result.DueDate.Should().Be(dueDate);

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
        _loanRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Loan>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.UpdateAsync(game), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoanGameToPlayer_ShouldThrowException_WhenGameDoesNotExist()
    {
        // Arrange
        var command = new CreateLoanCommand
        {
            GameId = 999,
            PlayerId = 1,
            LoanDate = DateTime.UtcNow
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(command.GameId))
            .ReturnsAsync((Game?)null);

        // Act
        var action = async () => await _loanService.LoanGameToPlayer(command);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();

        _gameRepositoryMock.Verify(x => x.GetByIdAsync(command.GameId), Times.Once);
    }

    [Fact]
    public async Task LoanGameToPlayer_ShouldCreateLoanWithoutDueDate_WhenDueDateIsNull()
    {
        // Arrange
        var gameId = 1;
        var playerId = 2;
        var loanDate = DateTime.UtcNow;

        var game = new Game("Test Game") { Id = gameId };
        var command = new CreateLoanCommand
        {
            GameId = gameId,
            PlayerId = playerId,
            LoanDate = loanDate,
            DueDate = null
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdAsync(gameId))
            .ReturnsAsync(game);

        _loanRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Loan>()))
            .ReturnsAsync((Loan l) => l);

        _gameRepositoryMock
            .Setup(x => x.UpdateAsync(game))
            .ReturnsAsync(game);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.LoanGameToPlayer(command);

        // Assert
        result.DueDate.Should().BeNull();
    }

    #endregion

    #region ReturnLoan Tests

    [Fact]
    public async Task ReturnLoan_ShouldMarkLoanAsReturned_WhenLoanExists()
    {
        // Arrange
        var loanId = 1;
        var loanDate = DateTime.UtcNow.AddDays(-10);
        var returnDate = DateTime.UtcNow;
        var loan = new Loan(1, 1, loanDate) { Id = loanId };

        var command = new ReturnLoanCommand
        {
            Id = loanId,
            ReturnDate = returnDate
        };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(loanId))
            .ReturnsAsync(loan);

        _loanRepositoryMock
            .Setup(x => x.UpdateAsync(loan))
            .ReturnsAsync(loan);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.ReturnLoan(command);

        // Assert
        result.Should().NotBeNull();
        result!.ReturnedDate.Should().Be(returnDate);

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId), Times.Once);
        _loanRepositoryMock.Verify(x => x.UpdateAsync(loan), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnLoan_ShouldReturnNull_WhenLoanDoesNotExist()
    {
        // Arrange
        var command = new ReturnLoanCommand
        {
            Id = 999,
            ReturnDate = DateTime.UtcNow
        };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync((Loan?)null);

        // Act
        var result = await _loanService.ReturnLoan(command);

        // Assert
        result.Should().BeNull();

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateLoanDates_WhenLoanExists()
    {
        // Arrange
        var loanId = 1;
        var originalLoanDate = DateTime.UtcNow.AddDays(-10);
        var newLoanDate = DateTime.UtcNow.AddDays(-5);
        var newDueDate = DateTime.UtcNow.AddDays(10);
        var loan = new Loan(1, 1, originalLoanDate) { Id = loanId };

        var command = new UpdateLoanCommand
        {
            Id = loanId,
            GameId = 1,
            PlayerId = 1,
            LoanDate = newLoanDate,
            DueDate = newDueDate
        };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(loanId))
            .ReturnsAsync(loan);

        _loanRepositoryMock
            .Setup(x => x.UpdateAsync(loan))
            .ReturnsAsync(loan);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.Update(command);

        // Assert
        result.Should().NotBeNull();
        result!.LoanDate.Should().Be(newLoanDate);
        result.DueDate.Should().Be(newDueDate);

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId), Times.Once);
        _loanRepositoryMock.Verify(x => x.UpdateAsync(loan), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldReturnNull_WhenLoanDoesNotExist()
    {
        // Arrange
        var command = new UpdateLoanCommand
        {
            Id = 999,
            GameId = 1,
            PlayerId = 1,
            LoanDate = DateTime.UtcNow
        };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync((Loan?)null);

        // Act
        var result = await _loanService.Update(command);

        // Assert
        result.Should().BeNull();

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldUpdateWithNullDueDate()
    {
        // Arrange
        var loanId = 1;
        var originalLoanDate = DateTime.UtcNow.AddDays(-10);
        var loan = new Loan(1, 1, originalLoanDate) { Id = loanId };
        loan.SetDueDate(DateTime.UtcNow.AddDays(5)); // Initially had a due date

        var command = new UpdateLoanCommand
        {
            Id = loanId,
            GameId = 1,
            PlayerId = 1,
            LoanDate = originalLoanDate,
            DueDate = null
        };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(loanId))
            .ReturnsAsync(loan);

        _loanRepositoryMock
            .Setup(x => x.UpdateAsync(loan))
            .ReturnsAsync(loan);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.Update(command);

        // Assert
        result.Should().NotBeNull();
        result!.DueDate.Should().BeNull();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeleteLoan_WhenCalled()
    {
        // Arrange
        var loanId = 1;

        _loanRepositoryMock
            .Setup(x => x.DeleteAsync(loanId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _loanService.Delete(loanId);

        // Assert
        _loanRepositoryMock.Verify(x => x.DeleteAsync(loanId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldCallDeleteAsync_WithCorrectId()
    {
        // Arrange
        var loanId = 42;

        _loanRepositoryMock
            .Setup(x => x.DeleteAsync(loanId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _loanService.Delete(loanId);

        // Assert
        _loanRepositoryMock.Verify(x => x.DeleteAsync(42), Times.Once);
    }

    #endregion

    #region CountActiveLoans Tests

    [Fact]
    public async Task CountActiveLoans_ShouldReturnCount_FromRepository()
    {
        // Arrange
        var expectedCount = 5;

        _loanRepositoryMock
            .Setup(x => x.CountActiveLoans())
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _loanService.CountActiveLoans();

        // Assert
        result.Should().Be(expectedCount);

        _loanRepositoryMock.Verify(x => x.CountActiveLoans(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CountActiveLoans_ShouldReturnZero_WhenNoActiveLoans()
    {
        // Arrange
        _loanRepositoryMock
            .Setup(x => x.CountActiveLoans())
            .ReturnsAsync(0);

        // Act
        var result = await _loanService.CountActiveLoans();

        // Assert
        result.Should().Be(0);

        _loanRepositoryMock.Verify(x => x.CountActiveLoans(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
