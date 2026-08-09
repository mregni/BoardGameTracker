using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using BoardGameTracker.Core.Datastore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

/// <summary>
/// Read-only specification-driven repository for any entity type. Uses the Ardalis EF Core
/// evaluator to translate specs onto the underlying <see cref="DbSet{T}"/>. Contains no
/// SaveChanges — reads only.
/// </summary>
public class EfReadRepository<T> : IReadRepository<T> where T : class
{
    protected readonly MainDbContext Context;
    private readonly ISpecificationEvaluator _evaluator;

    public EfReadRepository(MainDbContext context)
        : this(context, SpecificationEvaluator.Default)
    {
    }

    protected EfReadRepository(MainDbContext context, ISpecificationEvaluator evaluator)
    {
        Context = context;
        _evaluator = evaluator;
    }

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification((ISpecification<T>)specification).SingleOrDefaultAsync(cancellationToken);

    public Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).ToListAsync(cancellationToken);

    public Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).ToListAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification, evaluateCriteriaOnly: true).CountAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Context.Set<T>().CountAsync(cancellationToken);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification, evaluateCriteriaOnly: true).AnyAsync(cancellationToken);

    protected IQueryable<T> ApplySpecification(ISpecification<T> specification, bool evaluateCriteriaOnly = false)
        => _evaluator.GetQuery(Context.Set<T>().AsQueryable(), specification, evaluateCriteriaOnly);

    protected IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification)
        => _evaluator.GetQuery(Context.Set<T>().AsQueryable(), specification);
}
