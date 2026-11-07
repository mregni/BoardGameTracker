using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoardGameTracker.Tests.Datastore;

public class EfReadRepositoryTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly EfReadRepository<Player> _repository;

    public EfReadRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);
        _repository = new EfReadRepository<Player>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SeedAsync(params string[] names)
    {
        _context.Players.AddRange(names.Select(name => new Player(name)));
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldApplyTheFilterAndOrder()
    {
        await SeedAsync("Bob", "Anna", "Adam");

        var result = await _repository.FirstOrDefaultAsync(new NamesStartingWith("A"));

        result!.Name.Should().Be("Adam");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldReturnNull_WhenNothingMatches()
    {
        await SeedAsync("Bob");

        var result = await _repository.FirstOrDefaultAsync(new NamesStartingWith("A"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldApplyTheProjection()
    {
        await SeedAsync("Bob", "Anna");

        var result = await _repository.FirstOrDefaultAsync(new NameLengths("Ann"));

        result.Should().Be(4);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ShouldReturnTheOnlyMatch()
    {
        await SeedAsync("Bob", "Anna");

        var result = await _repository.SingleOrDefaultAsync(new PlayerNamed("Anna"));

        result!.Name.Should().Be("Anna");
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ShouldThrow_WhenSeveralRowsMatch()
    {
        await SeedAsync("Anna", "Anna");

        var act = async () => await _repository.SingleOrDefaultAsync(new PlayerNamed("Anna"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListAsync_ShouldApplyTheProjection()
    {
        await SeedAsync("Bob", "Anna", "Annabel");

        var result = await _repository.ListAsync(new NameLengths("Ann"));

        result.Should().BeEquivalentTo(new[] { 4, 7 });
    }

    [Fact]
    public async Task CountAsync_ShouldIgnorePaging_AndOnlyCountMatches()
    {
        await SeedAsync("Bob", "Anna", "Adam", "Abel");

        var result = await _repository.CountAsync(new NamesStartingWith("A", take: 1));

        result.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_ShouldCountEveryRow_WithoutASpecification()
    {
        await SeedAsync("Bob", "Anna");

        var result = await _repository.CountAsync();

        result.Should().Be(2);
    }

    [Fact]
    public async Task AnyAsync_ShouldReflectWhetherARowMatches()
    {
        await SeedAsync("Bob");

        (await _repository.AnyAsync(new NamesStartingWith("B"))).Should().BeTrue();
        (await _repository.AnyAsync(new NamesStartingWith("A"))).Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_ShouldForwardTheCancellationToken()
    {
        await SeedAsync("Anna");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = async () => await _repository.ListAsync(new NamesStartingWith("A"), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class NamesStartingWith : Specification<Player>
    {
        public NamesStartingWith(string prefix, int? take = null)
        {
            Query.Where(x => x.Name.StartsWith(prefix)).OrderBy(x => x.Name);

            if (take.HasValue)
            {
                Query.Take(take.Value);
            }
        }
    }

    private sealed class PlayerNamed : SingleResultSpecification<Player>
    {
        public PlayerNamed(string name)
        {
            Query.Where(x => x.Name == name);
        }
    }

    private sealed class NameLengths : Specification<Player, int>
    {
        public NameLengths(string prefix)
        {
            Query.Where(x => x.Name.StartsWith(prefix));
            Query.Select(x => x.Name.Length);
        }
    }
}
