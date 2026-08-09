# Architecture — Data Access Layer

BoardGameTracker's repository/data-access layer follows the **Specification pattern**
(via [`Ardalis.Specification`](https://github.com/ardalis/Specification)) on top of EF Core.
Three responsibilities are kept strictly separate:

| Concern | Lives in | Notes |
|---|---|---|
| **Queries** — filter / order / include / page / project | **Specifications**: `BoardGameTracker.Core/{Aggregate}/Specifications/{Name}Spec.cs` | One class per query, named for its intent. Tracking is expressed by the presence/absence of `AsNoTracking()`. Specs carry **no comments** — the class name plus the `Query` builder are self-documenting. |
| **Aggregates, `GroupBy`, commands** | **Hand-written repository methods** | `GroupBy` / `Sum` / `Average` / `Max` charts and statistics, `ExecuteUpdateAsync`, and multi-step domain mutations have no spec-builder equivalent and stay as repository methods. |
| **Persistence** (`SaveChanges`) | **`IUnitOfWork` only** | Repositories never save. Services stage changes via `CreateAsync` / `Update` / `DeleteAsync`, then call `IUnitOfWork.SaveChangesAsync()` **once** per use case. |

## Generic repositories

- `IReadRepository<T>` / `EfReadRepository<T>` — spec-driven reads (`ListAsync`,
  `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `CountAsync`, `AnyAsync`). Works for any
  entity, including composite-key ones such as `PlayerSession`.
- `IRepository<T> : IReadRepository<T>` / `EfRepository<T>` — adds staged CRUD
  (`CreateAsync`, `CreateRangeAsync`, `Update`, `DeleteAsync`, `GetByIdAsync`, `GetAllAsync`)
  for entities with an int `Id` (`HasId`). **None of these methods call `SaveChanges`.**

Both are registered as open generics in `ServiceCollectionExtensions.AddCoreService`.

## Why not Ardalis `RepositoryBase<T>`

Ardalis's shipped `RepositoryBase<T>` calls `SaveChanges` inside `AddAsync` / `UpdateAsync` /
`DeleteAsync`. This app deliberately defers saves to a single `IUnitOfWork.SaveChangesAsync()`
per use case — badge awarding and batch BGG import both rely on that one atomic save. We use
the Ardalis **evaluator** (the valuable part) through our own `EfRepository<T>`, and never
register `RepositoryBase<T>` / `IRepositoryBase<T>`.

## Per-aggregate repositories

A per-aggregate repository (e.g. `IGameRepository`) survives only where it still owns
hand-written `GroupBy` / aggregate / command methods; its query methods delegate to specs.
`GameStatisticsRepository`, `CompareRepository`, and `ConfigRepository` are intentionally
hand-written (charts, two-player aggregates, and key-value config with `ExecuteUpdateAsync`).

See [`SPEC_PATTERN_MIGRATION_PLAN.md`](SPEC_PATTERN_MIGRATION_PLAN.md) for the full design
record and the per-repository mapping.
