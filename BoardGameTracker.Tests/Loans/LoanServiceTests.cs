using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Games.Specifications;
using BoardGameTracker.Core.Loans;
using BoardGameTracker.Core.Loans.Specifications;
using BoardGameTracker.Core.Players.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Loans;

public class LoanServiceTests
{
    private readonly Mock<IRepository<Loan>> _loanRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IReadRepository<Player>> _playerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly LoanService _loanService;

    public LoanServiceTests()
    {
        _loanRepositoryMock = new Mock<IRepository<Loan>>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _playerRepositoryMock = new Mock<IReadRepository<Player>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _loanService = new LoanService(
            _loanRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dateTimeProviderMock.Object,
            NullLogger<LoanService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _loanRepositoryMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
    }

    private static bool MatchesGame(GameWithLoansSpec spec, int id) =>
        spec.IsSatisfiedBy(new Game("Any") { Id = id }) && !spec.IsSatisfiedBy(new Game("Any") { Id = id + 1 });

    private static bool MatchesPlayer(PlayerByIdSpec spec, int id) =>
        spec.IsSatisfiedBy(new Player("Any") { Id = id }) && !spec.IsSatisfiedBy(new Player("Any") { Id = id + 1 });

    private void SetupPlayerExists(int playerId, bool exists)
    {
        _playerRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<PlayerByIdSpec>(s => MatchesPlayer(s, playerId)), CancellationToken.None))
            .ReturnsAsync(exists);
    }

    private void VerifyPlayerChecked(int playerId)
    {
        _playerRepositoryMock.Verify(x => x.AnyAsync(It.Is<PlayerByIdSpec>(s => MatchesPlayer(s, playerId)), CancellationToken.None), Times.Once);
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
            .Setup(x => x.ListAsync(It.IsAny<LoansOrderedByDateSpec>(), CancellationToken.None))
            .ReturnsAsync(loans);

        // Act
        var result = await _loanService.GetLoans();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(loans);

        _loanRepositoryMock.Verify(x => x.ListAsync(It.IsAny<LoansOrderedByDateSpec>(), CancellationToken.None), Times.Once);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoanGameToPlayer_ShouldCreateLoan_WhenGameExists(bool hasDueDate)
    {
        // Arrange
        var gameId = 1;
        var playerId = 2;
        var loanDate = DateTime.UtcNow;
        DateTime? dueDate = hasDueDate ? DateTime.UtcNow.AddDays(14) : null;

        var game = new Game("Test Game") { Id = gameId };
        SetupPlayerExists(playerId, true);
        var command = new CreateLoanCommand
        {
            GameId = gameId,
            PlayerId = playerId,
            LoanDate = loanDate,
            DueDate = dueDate
        };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        _loanRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Loan>(l => l.GameId == gameId && l.PlayerId == playerId && l.LoanDate == loanDate && l.DueDate == dueDate)))
            .ReturnsAsync((Loan l) => l);


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

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, gameId)), CancellationToken.None), Times.Once);
        VerifyPlayerChecked(playerId);
        _loanRepositoryMock.Verify(x => x.CreateAsync(It.Is<Loan>(l => l.GameId == gameId && l.PlayerId == playerId && l.LoanDate == loanDate && l.DueDate == dueDate)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoanGameToPlayer_ShouldThrowDomainException_WhenGameAlreadyOnLoan()
    {
        var gameId = 1;
        var game = new Game("Test Game") { Id = gameId };
        game.LoanToPlayer(99, DateTime.UtcNow.AddDays(-3));

        SetupPlayerExists(2, true);
        var command = new CreateLoanCommand
        {
            GameId = gameId,
            PlayerId = 2,
            LoanDate = DateTime.UtcNow
        };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = async () => await _loanService.LoanGameToPlayer(command);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.GameAlreadyOnLoan);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, gameId)), CancellationToken.None), Times.Once);
        VerifyPlayerChecked(2);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoanGameToPlayer_ShouldThrowDomainException_WhenDueDateRunsIntoLaterLoan()
    {
        var gameId = 1;
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var game = new Game("Test Game") { Id = gameId };
        game.LoanToPlayer(99, start.AddDays(10));

        SetupPlayerExists(2, true);
        var command = new CreateLoanCommand
        {
            GameId = gameId,
            PlayerId = 2,
            LoanDate = start,
            DueDate = start.AddDays(14)
        };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, gameId)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = async () => await _loanService.LoanGameToPlayer(command);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(Constants.Errors.GameAlreadyOnLoan);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, gameId)), CancellationToken.None), Times.Once);
        VerifyPlayerChecked(2);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoanGameToPlayer_ShouldThrowEntityNotFoundException_WhenGameDoesNotExist()
    {
        // Arrange
        var command = new CreateLoanCommand
        {
            GameId = 999,
            PlayerId = 1,
            LoanDate = DateTime.UtcNow
        };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 999)), CancellationToken.None))
            .ReturnsAsync((Game?)null);

        // Act
        var action = async () => await _loanService.LoanGameToPlayer(command);

        // Assert
        var thrown = await action.Should().ThrowAsync<EntityNotFoundException>();
        thrown.Which.EntityType.Should().Be(nameof(Game));
        thrown.Which.EntityId.Should().Be(999);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 999)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoanGameToPlayer_ShouldThrowEntityNotFoundException_WhenPlayerDoesNotExist()
    {
        var game = new Game("Test Game") { Id = 1 };
        var command = new CreateLoanCommand { GameId = 1, PlayerId = 999, LoanDate = DateTime.UtcNow };

        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);
        SetupPlayerExists(999, false);

        var act = async () => await _loanService.LoanGameToPlayer(command);

        var thrown = await act.Should().ThrowAsync<EntityNotFoundException>();
        thrown.Which.EntityType.Should().Be(nameof(Player));
        thrown.Which.EntityId.Should().Be(999);

        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None), Times.Once);
        VerifyPlayerChecked(999);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
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


        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.ReturnLoan(command);

        // Assert
        result.Should().NotBeNull();
        result.ReturnedDate.Should().Be(returnDate);

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnLoan_ShouldThrowEntityNotFoundException_WhenLoanDoesNotExist()
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
        var action = async () => await _loanService.ReturnLoan(command);

        // Assert
        var thrown = await action.Should().ThrowAsync<EntityNotFoundException>();
        thrown.Which.EntityType.Should().Be(nameof(Loan));
        thrown.Which.EntityId.Should().Be(999);

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UndoReturn Tests

    [Fact]
    public async Task UndoReturn_ShouldPutLoanBackOnLoan_WhenGameIsFree()
    {
        var game = new Game("Catan") { Id = 1 };
        var loan = game.LoanToPlayer(3, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        loan.Id = 8;
        loan.MarkAsReturned(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _loanService.UndoReturn(8);

        result.ReturnedDate.Should().BeNull();
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UndoReturn_ShouldRefuse_WhenGameWasLoanedAgainSinceTheReturn()
    {
        var game = new Game("Catan") { Id = 1 };
        var loan = game.LoanToPlayer(3, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        loan.Id = 8;
        loan.MarkAsReturned(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        game.LoanToPlayer(2, new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)).Id = 9;
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = () => _loanService.UndoReturn(8);

        await act.Should().ThrowAsync<DomainException>().WithMessage(BoardGameTracker.Common.Constants.Errors.GameAlreadyOnLoan);
        loan.ReturnedDate.Should().NotBeNull();
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UndoReturn_ShouldRefuse_WhenLoanWasNotReturned()
    {
        var game = new Game("Catan") { Id = 1 };
        var loan = game.LoanToPlayer(3, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        loan.Id = 8;
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = () => _loanService.UndoReturn(8);

        await act.Should().ThrowAsync<DomainException>().WithMessage(BoardGameTracker.Common.Constants.Errors.LoanNotReturned);
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UndoReturn_ShouldThrowEntityNotFoundException_WhenLoanDoesNotExist()
    {
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Loan?)null);

        var act = () => _loanService.UndoReturn(999);

        var thrown = await act.Should().ThrowAsync<EntityNotFoundException>();
        thrown.Which.EntityType.Should().Be(nameof(Loan));
        thrown.Which.EntityId.Should().Be(999);
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UndoReturn_ShouldThrowEntityNotFoundException_WhenTheGameIsGone()
    {
        var loan = new Loan(4, 1, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)) { Id = 8 };
        loan.MarkAsReturned(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None))
            .ReturnsAsync((Game?)null);

        var act = () => _loanService.UndoReturn(8);

        var thrown = await act.Should().ThrowAsync<EntityNotFoundException>();
        thrown.Which.EntityType.Should().Be(nameof(Game));
        thrown.Which.EntityId.Should().Be(4);
        loan.ReturnedDate.Should().Be(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenTheGameIsGone()
    {
        var loanDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var loan = new Loan(4, 1, loanDate) { Id = 8 };
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None))
            .ReturnsAsync((Game?)null);

        var act = () => _loanService.Update(new UpdateLoanCommand { Id = 8, GameId = 77, PlayerId = 1, LoanDate = loanDate.AddDays(1) });

        var thrown = await act.Should().ThrowAsync<EntityNotFoundException>();
        thrown.Which.EntityType.Should().Be(nameof(Game));
        thrown.Which.EntityId.Should().Be(4);
        loan.LoanDate.Should().Be(loanDate);
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldKeepTheReturnedDate_WhenTheCommandHasNone()
    {
        var loanDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var returnedDate = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        var loan = new Loan(4, 1, loanDate) { Id = 8 };
        loan.MarkAsReturned(returnedDate);
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None))
            .ReturnsAsync(new Game("Catan") { Id = 4 });
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _loanService.Update(new UpdateLoanCommand { Id = 8, GameId = 4, PlayerId = 1, LoanDate = loanDate });

        result.ReturnedDate.Should().Be(returnedDate);
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldUpdateLoanDates_WhenLoanExists()
    {
        // Arrange
        var loanId = 1;
        var originalLoanDate = DateTime.UtcNow.AddDays(-10);
        var newLoanDate = DateTime.UtcNow.AddDays(-5);
        var newDueDate = DateTime.UtcNow.AddDays(10);
        var loan = new Loan(4, 1, originalLoanDate) { Id = loanId };

        var command = new UpdateLoanCommand
        {
            Id = loanId,
            GameId = 4,
            PlayerId = 1,
            LoanDate = newLoanDate,
            DueDate = newDueDate
        };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(loanId))
            .ReturnsAsync(loan);


        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None))
            .ReturnsAsync(new Game("Catan") { Id = 4 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.Update(command);

        // Assert
        result.Should().NotBeNull();
        result.LoanDate.Should().Be(newLoanDate);
        result.DueDate.Should().Be(newDueDate);

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldRefuseDates_ThatOverlapAnotherLoanOfTheSameGame()
    {
        var game = new Game("Catan") { Id = 1 };
        var earlier = game.LoanToPlayer(2, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        earlier.Id = 7;
        earlier.MarkAsReturned(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        var loan = game.LoanToPlayer(3, new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        loan.Id = 8;
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = () => _loanService.Update(new UpdateLoanCommand { Id = 8, GameId = 1, PlayerId = 3, LoanDate = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc) });

        await act.Should().ThrowAsync<DomainException>().WithMessage(BoardGameTracker.Common.Constants.Errors.GameAlreadyOnLoan);
        loan.LoanDate.Should().Be(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldRefuseDates_AfterTheDueDateOfAnUnreturnedLoan()
    {
        var game = new Game("Catan") { Id = 1 };
        var loan = game.LoanToPlayer(3, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        loan.Id = 8;
        loan.MarkAsReturned(new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc));
        var overdue = game.LoanToPlayer(2, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        overdue.Id = 7;
        overdue.SetDueDate(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = () => _loanService.Update(new UpdateLoanCommand
        {
            Id = 8,
            GameId = 1,
            PlayerId = 3,
            LoanDate = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc),
            ReturnedDate = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc)
        });

        await act.Should().ThrowAsync<DomainException>().WithMessage(BoardGameTracker.Common.Constants.Errors.GameAlreadyOnLoan);
        loan.LoanDate.Should().Be(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldTreatAnUnreturnedLoanAsOpenEnded_WhenALaterLoanExists()
    {
        var game = new Game("Catan") { Id = 1 };
        var later = game.LoanToPlayer(2, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
        later.Id = 7;
        var loan = new Loan(1, 3, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)) { Id = 8 };
        _loanRepositoryMock.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None))
            .ReturnsAsync(game);

        var act = () => _loanService.Update(new UpdateLoanCommand
        {
            Id = 8,
            GameId = 1,
            PlayerId = 3,
            LoanDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        await act.Should().ThrowAsync<DomainException>().WithMessage(BoardGameTracker.Common.Constants.Errors.GameAlreadyOnLoan);
        loan.LoanDate.Should().Be(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(8), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 1)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenLoanDoesNotExist()
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
        var action = async () => await _loanService.Update(command);

        // Assert
        var thrown = await action.Should().ThrowAsync<EntityNotFoundException>();
        thrown.Which.EntityType.Should().Be(nameof(Loan));
        thrown.Which.EntityId.Should().Be(999);

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldUpdateWithNullDueDate()
    {
        // Arrange
        var loanId = 1;
        var originalLoanDate = DateTime.UtcNow.AddDays(-10);
        var loan = new Loan(4, 1, originalLoanDate) { Id = loanId };
        loan.SetDueDate(DateTime.UtcNow.AddDays(5));

        var command = new UpdateLoanCommand
        {
            Id = loanId,
            GameId = 4,
            PlayerId = 1,
            LoanDate = originalLoanDate,
            DueDate = null
        };

        _loanRepositoryMock
            .Setup(x => x.GetByIdAsync(loanId))
            .ReturnsAsync(loan);


        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None))
            .ReturnsAsync(new Game("Catan") { Id = 4 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _loanService.Update(command);

        // Assert
        result.Should().NotBeNull();
        result.DueDate.Should().BeNull();

        _loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldChangeTheReturnedDate_WhenTheLoanWasReturned()
    {
        var loanDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var loan = new Loan(4, 1, loanDate) { Id = 1 };
        loan.MarkAsReturned(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        var newReturnedDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        _loanRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None))
            .ReturnsAsync(new Game("Catan") { Id = 4 });
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _loanService.Update(new UpdateLoanCommand { Id = 1, GameId = 4, PlayerId = 1, LoanDate = loanDate, ReturnedDate = newReturnedDate });

        result.ReturnedDate.Should().Be(newReturnedDate);
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldIgnoreTheReturnedDate_WhenTheLoanIsStillActive()
    {
        var loanDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var loan = new Loan(4, 1, loanDate) { Id = 1 };

        _loanRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(loan);
        _gameRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None))
            .ReturnsAsync(new Game("Catan") { Id = 4 });
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _loanService.Update(new UpdateLoanCommand { Id = 1, GameId = 4, PlayerId = 1, LoanDate = loanDate, ReturnedDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc) });

        result.ReturnedDate.Should().BeNull();
        _loanRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<GameWithLoansSpec>(s => MatchesGame(s, 4)), CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
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

    #endregion

    #region CountActiveLoans Tests

    [Fact]
    public async Task CountActiveLoans_ShouldReturnCount_FromRepository()
    {
        var expectedCount = 5;
        var now = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = new CancellationTokenSource().Token;
        var upcoming = new Loan(1, 1, now.AddDays(1));
        var started = new Loan(1, 1, now);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        _loanRepositoryMock
            .Setup(x => x.CountAsync(It.Is<ActiveLoansSpec>(s => !s.IsSatisfiedBy(upcoming) && s.IsSatisfiedBy(started)), token))
            .ReturnsAsync(expectedCount);

        var result = await _loanService.CountActiveLoans(token);

        result.Should().Be(expectedCount);

        _loanRepositoryMock.Verify(x => x.CountAsync(It.Is<ActiveLoansSpec>(s => !s.IsSatisfiedBy(upcoming) && s.IsSatisfiedBy(started)), token), Times.Once);
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
