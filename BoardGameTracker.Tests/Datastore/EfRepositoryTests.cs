using System;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Datastore;

public class EfRepositoryTests
{
    private static MainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MainDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotPersist_UntilSaveChanges()
    {
        await using var context = CreateContext();
        var repository = new EfRepository<Loan>(context);
        var loan = new Loan(1, 1, DateTime.UtcNow);

        await repository.CreateAsync(loan);

        // The entity is tracked as Added but must NOT be committed until IUnitOfWork saves.
        context.ChangeTracker.Entries<Loan>().Count(e => e.State == EntityState.Added).Should().Be(1);
        (await context.Loans.CountAsync()).Should().Be(0);

        await context.SaveChangesAsync();
        (await context.Loans.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
    {
        await using var context = CreateContext();
        var repository = new EfRepository<Loan>(context);

        var result = await repository.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_ShouldApplyWhereAndOrder_FromSpecification()
    {
        await using var context = CreateContext();
        var repository = new EfRepository<Loan>(context);

        var older = new Loan(1, 1, DateTime.UtcNow.AddDays(-10));
        var newer = new Loan(2, 1, DateTime.UtcNow.AddDays(-2));
        var returned = new Loan(3, 1, DateTime.UtcNow.AddDays(-5));
        returned.MarkAsReturned(DateTime.UtcNow.AddDays(-1));
        await context.Loans.AddRangeAsync(older, newer, returned);
        await context.SaveChangesAsync();

        var result = await repository.ListAsync(new ActiveLoansOrderedSpec());

        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.ReturnedDate == null);
        result.Select(x => x.LoanDate).Should().BeInDescendingOrder();
    }

    private sealed class ActiveLoansOrderedSpec : Specification<Loan>
    {
        public ActiveLoansOrderedSpec()
        {
            Query
                .Where(x => x.ReturnedDate == null)
                .OrderByDescending(x => x.LoanDate)
                .AsNoTracking();
        }
    }
}
