# Specification Pattern Migration Plan — BoardGameTracker Backend

**Status:** COMPLETED (2026-07-21) — implemented in the working tree on `feature/170-fixes`; all phases (0–6) done, full test suite green. Retained as the design record. See [ARCHITECTURE.md](ARCHITECTURE.md) for the resulting data-access responsibilities.
**Target library:** `Ardalis.Specification` 9.3.1 + `Ardalis.Specification.EntityFrameworkCore` 9.3.1
**Scope:** the repository/data-access layer under `BoardGameTracker.Core` (query paths). Auth data access (`TokenService`, `OidcService`, `OidcProviderService`, `RefreshTokenCleanupService`, `DbSeeder` — all of which use `MainDbContext` directly) is explicitly **out of scope**.

---

## Table of contents

1. [The Specification pattern and why Ardalis.Specification](#1-the-specification-pattern-and-why-ardalisspecification)
2. [Current-state inventory](#2-current-state-inventory)
3. [Target architecture](#3-target-architecture)
4. [Before/after examples](#4-beforeafter-examples)
5. [Migration plan (phases)](#5-migration-plan-phases)
6. [Per-repository migration checklist](#6-per-repository-migration-checklist)
7. [Testing strategy](#7-testing-strategy)
8. [Risks and gotchas specific to this codebase](#8-risks-and-gotchas-specific-to-this-codebase)
9. [Definition of done](#9-definition-of-done)

---

## 1. The Specification pattern and why Ardalis.Specification

### 1.1 The pattern in one paragraph

A *specification* is a named, reusable, self-contained object that encapsulates the **shape of a query**: filtering (`Where`), ordering (`OrderBy`/`ThenBy`), eager loading (`Include`/`ThenInclude`), paging (`Skip`/`Take`), tracking behavior (`AsNoTracking`), split-query behavior (`AsSplitQuery`), and optionally a **projection** (`Select`). Repositories collapse to a small generic surface (`ListAsync(spec)`, `FirstOrDefaultAsync(spec)`, `CountAsync(spec)`, `AnyAsync(spec)`) and the *business meaning* of each query moves into a class with a name like `GamesWithNoRecentSessionsSpec`. Benefits for this codebase specifically:

- Query intent gets a name and one home (today the "shelf of shame" predicate is duplicated verbatim in three methods — see §2.3, `GameRepository.cs:112`, `:120`, `:128`).
- Specs are **unit-testable in memory** without a DbContext, using the in-memory evaluator (`spec.Evaluate(items)`), which fits the existing xUnit + Moq + FluentAssertions setup.
- Read/write tracking policy becomes explicit per-query instead of implicit per-repository-method — directly relevant to the confirmed Player-update tracking bug (`BACKEND_REVIEW.md:27-34`).

### 1.2 Ardalis.Specification API surface used by this plan

| Concept | Type / API | Notes |
|---|---|---|
| Contract | `ISpecification<T>` | What evaluators/repositories consume. |
| Base class | `Specification<T>` | Subclass; build the query in the constructor via the `Query` builder property. |
| Query builder | `Query.Where(...).Include(...).ThenInclude(...).OrderBy(...).ThenBy(...).Skip(n).Take(n).AsNoTracking().AsSplitQuery().AsTracking().TagWith("...")` | Fluent, chainable. Anything not called is simply not applied. |
| Projection spec | `Specification<T, TResult>` implementing `ISpecification<T, TResult>` with `Query.Select(x => new TResult {...})` | Replaces hand-written `.Select(...)` projections. `Query.SelectMany(...)` also exists for collection-flattening projections. |
| Single-result marker | `ISingleResultSpecification<T>` / `SingleResultSpecification<T>` | Semantic marker for by-id/by-unique-key specs. |
| EF evaluator | `SpecificationEvaluator.Default.GetQuery(IQueryable<T>, spec)` | Translates a spec onto an `IQueryable`. This is what a custom repository uses internally. |
| IQueryable extension | `queryable.WithSpecification(spec)` (in `Ardalis.Specification.EntityFrameworkCore`) | Lets an *existing* repository apply a spec to `_context.Games` without adopting `RepositoryBase`. This is the key incremental-migration tool. |
| Prebuilt repos | `RepositoryBase<T>` / `ReadRepositoryBase<T>` (in the EF Core package) | Generic repository implementations with `GetByIdAsync`, `ListAsync(spec)`, `FirstOrDefaultAsync(spec)`, `SingleOrDefaultAsync(singleSpec)`, `CountAsync(spec)`, `AnyAsync(spec)`, `AsAsyncEnumerable(spec)`. **Warning:** `RepositoryBase<T>.AddAsync/UpdateAsync/DeleteAsync call `SaveChangesAsync` immediately** — see §3.3 for why we will NOT use them as-is. |
| In-memory evaluator | `InMemorySpecificationEvaluator.Default.Evaluate(spec, items)` or the `spec.Evaluate(items)` extension | Applies Where/OrderBy/Skip/Take/Select to an `IEnumerable<T>` — the backbone of spec unit tests. Ignores `Include`/`AsNoTracking` (they are EF-only concerns). |
| Validation | `spec.IsSatisfiedBy(entity)` | Checks an entity against the spec's criteria in memory. |

### 1.3 Why Ardalis.Specification (and when a hand-rolled version would be better)

**Choose Ardalis.Specification because:**

1. **The repo is already in the Ardalis ecosystem.** `BoardGameTracker.Common.csproj:14` references `Ardalis.GuardClauses 5.0.0`, and `SessionRepository.cs:1` already uses it (`Guard.Against.Null` at `SessionRepository.cs:156`). Same maintainer, same conventions, long-lived and actively maintained (9.3.1 released 2025-08-24).
2. **Version compatibility is clean** (verified against nuget.org): `Ardalis.Specification.EntityFrameworkCore 9.3.1` targets `net8.0` (requires `Microsoft.EntityFrameworkCore >= 8.0.19`) and `net9.0` (requires `>= 9.0.8`). This project is `net8.0` (`BoardGameTracker.Core.csproj:4`) with **EF Core 9.0.16** (`BoardGameTracker.Core.csproj:13`) — the net8.0 asset is satisfied by 9.0.16. No conflicts.
3. **Free in-memory evaluator** — spec logic becomes unit-testable without `Microsoft.EntityFrameworkCore.InMemory` or a real database, matching the existing pure-Moq test style (`BoardGameTracker.Tests/Services/PlayerServiceTests.cs:23-60`).
4. **`WithSpecification(...)`** allows a low-risk incremental rollout: existing repository classes keep their public interfaces while their bodies shrink to spec applications; services never break mid-migration.

**When a hand-rolled implementation would be preferable (not the case here, but for the record):** a hand-rolled `ISpecification<T>` (a class exposing `Expression<Func<T,bool>> Criteria`, a list of include expressions, an order-by expression, plus an `ApplySpecification` extension method — ~120 lines total) is the better call when (a) you refuse third-party dependencies in the domain layer, (b) you only need 2–3 query capabilities (e.g., just `Where` + `Include`), or (c) you need exotic query operators the library's builder doesn't model (`GroupBy`, `ExecuteUpdate`, window functions) *as the common case*. Here, GroupBy queries exist but are a minority (~15 of ~70 methods) and are handled by the hybrid strategy in §3.5 — so the library wins.

---

## 2. Current-state inventory

### 2.1 Datastore infrastructure

| Item | File | Notes |
|---|---|---|
| DbContext | `BoardGameTracker.Core/Datastore/MainDbContext.cs:13` | `IdentityDbContext<ApplicationUser>`; 18 `DbSet`s (`:15-32`). **No `IEntityTypeConfiguration<T>` classes exist** — all model config is private static methods inside `OnModelCreating` (`:38-53`): `BuildIds` (reflection-based key config, `:55-70`), `ConfigureValueObjects` (owned types `BuyingPrice`/`SoldPrice`/`Rating`/`Weight` with `HasPrecision(18,2)`, and `PlayerCount`/`PlayTime`, `:72-123`), `BuildGame`/`BuildGameSessions`/`BuildPlayer`/`BuildBadges`/`BuildLoans`/`BuildGameNights`/`BuildAuthEntities`, plus data seeding `SeedDatabase` (`:300-361`, seeds `Language` and 39 `Badge` rows). **None of this changes in the migration.** |
| Generic CRUD base | `BoardGameTracker.Core/Datastore/CrudHelper.cs:7` | `abstract class CrudHelper<T> : ICrudHelper<T> where T : HasId`. Methods: `GetByIdAsync` (tracked `FirstOrDefaultAsync`, `:16`), `GetAllAsync` (`AsNoTracking`, `:21`), `CreateAsync` (**Add only, no save**, `:26`), `CreateRangeAsync` (`:32`, note: not on the interface), `Update` (`:37`), `DeleteAsync` (`FindAsync` + `Remove`, no save, `:43`). All `virtual` — repositories override freely. |
| CRUD interface | `BoardGameTracker.Core/Datastore/Interfaces/ICrudHelper.cs:3` | 5 methods; the per-aggregate repo interfaces extend it. |
| Unit of Work | `BoardGameTracker.Core/Datastore/UnitOfWork.cs:6`, `Interfaces/IUnitOfWork.cs:5` | Thin wrapper: `SaveChangesAsync` + `BeginTransactionAsync`. Services call `CreateAsync(...)` then later `_unitOfWork.SaveChangesAsync()` (e.g. `PlayerService.cs:52-53`, `SessionService.cs:35-37`, `GameService.cs:94-95`). |
| DbSet extension | `BoardGameTracker.Common/Extensions/DbSetExtensions.cs:8` | `AddRangeIfNotExists<T>` (per-item `AnyAsync` then `AddAsync`; N+1 but out of scope). Used by `GameRepository.cs:22,27,32`. |
| Entity base | `BoardGameTracker.Common/Entities/Helpers/HasId.cs:3` | `abstract class HasId { public int Id { get; set; } }`. **Exception:** `PlayerSession` has a composite key (`MainDbContext.cs:215-216`) and is *not* usable with an id-keyed repository. |
| DI registration | `BoardGameTracker.Core/Extensions/ServiceCollectionExtensions.cs:73-84` (repositories), `:86` (`IUnitOfWork`), `:118-136` (`AddDbContext` with `UseNpgsql(...)`). Called from `BoardGameTracker.Host/Program.cs:49` (`AddCoreService()`). | **Important:** the Npgsql provider is configured with `UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` at `:135` — **split query is the global default**; explicit `AsSplitQuery()` calls in repos are belt-and-braces. |
| Config seeder | `BoardGameTracker.Core/Configuration/ConfigRepository.cs:66-84` | `SeedConfigAsync` — saves internally. Untouched. |
| Auth seeder | `BoardGameTracker.Core/Auth/DbSeeder.cs` | Out of scope. |

### 2.2 Method classification legend

- **CRUD** — covered by the generic repository, delete the override/method.
- **SPEC** — single fetch or list; converts to a `Specification<T>`.
- **SPEC-P** — projection; converts to a `Specification<T, TResult>` with `Select`.
- **SPEC+AGG** — a spec provides the filter, the generic repo's `CountAsync(spec)`/`AnyAsync(spec)` provides the aggregate.
- **KEEP** — stays a hand-written repository method (GroupBy, Sum/Average, `ExecuteUpdateAsync`, multi-step domain logic). The hybrid strategy (§3.5).

### 2.3 GameRepository — `BoardGameTracker.Core/Games/GameRepository.cs` (interface `Games/Interfaces/IGameRepository.cs`)

Extends `CrudHelper<Game>`.

| Method | Line | Classification | Current tracking / includes |
|---|---|---|---|
| `AddGameCategoriesIfNotExists` | :20 | KEEP (write helper via `AddRangeIfNotExists`) | n/a |
| `AddGameMechanicsIfNotExists` | :25 | KEEP | n/a |
| `AddPeopleIfNotExists` | :30 | KEEP | n/a |
| `GetGameByBggId` | :35 | SPEC (single, unique index on `BggId`) | **Tracked**, no includes |
| `GetGamesOverviewList` | :41 | SPEC | `AsNoTracking` + explicit `AsSplitQuery` + `Include(Expansions)` + `Include(Categories)`, `OrderBy(Title)` |
| `GetByIdAsync` (override) | :52 | SPEC (single) | **Tracked** (write path!), 5 includes: Accessories, Categories, Expansions, Mechanics, People |
| `GetExpansions` | :63 | SPEC (over `Expansion`, not `Game`) | Tracked (attached to sessions on write path — `SessionService.cs:70-74`), `Where(ids.Contains)` |
| `GetTotalExpansionCount` | :70 | SPEC+AGG (over `Expansion`) | untracked count |
| `CountAsync` | :75 | SPEC+AGG | count |
| `DeleteExpansion` | :80 | KEEP (multi-step: fetch + domain method `RemoveExpansion`) | Tracked + `Include(Expansions)` |
| `GetRecentlyAddedGames` | :98 | SPEC | `AsNoTracking`, `Where(AdditionDate != null)`, `OrderByDescending`, `Take(count)` |
| `GetGamesWithNoRecentSessions` | :108 | SPEC | `AsNoTracking`; **cross-DbSet subquery `_context.Sessions.Any(...)` — must be rewritten to the `g.Sessions` navigation** (equivalent SQL; nav configured at `MainDbContext.cs:168-171`) |
| `CountGamesWithNoRecentSessions` | :117 | SPEC+AGG | same predicate as above, duplicated — one spec serves both |
| `GetShameGames` | :124 | SPEC-P → `ShameGame` model | same predicate a third time + `Select` projection; the correlated `_context.Sessions...FirstOrDefault()` at `:136-140` also rewrites to `g.Sessions` |
| `GetByIdsAsync` | :146 | SPEC | Tracked, `Where(ids.Contains)` |

### 2.4 GameSessionRepository — `BoardGameTracker.Core/Games/GameSessionRepository.cs` (interface `Games/Interfaces/IGameSessionRepository.cs`)

Standalone (no `CrudHelper`). Injects `IDateTimeProvider` (`:14-18`).

| Method | Line | Classification | Notes |
|---|---|---|---|
| `GetSessions(gameId, skip, take?)` | :20 | SPEC (paged) | `AsNoTracking`, `Include(Location)`, `Include(PlayerSessions).ThenInclude(Player)`, `OrderByDescending(Start)`, `Skip`/conditional `Take` |
| `GetSessions(gameId, dayCount)` | :39 | SPEC | **`_dateTimeProvider.UtcNow` embedded in the expression tree at `:44`** — the spec must take a precomputed `DateTime cutoff` ctor parameter (see §8.6) |
| `GetSessionsByGameId` | :49 | SPEC | conditional `Take` |
| `GetSessionsByPlayerId` | :66 | SPEC | conditional `Take`, `Any` subquery on `PlayerSessions` |
| `GetPlayCount` | :83 | SPEC+AGG | `CountAsync(SessionsByGameSpec)` |
| `GetTotalPlayedTime` | :91 | KEEP | `SumAsync` over computed duration |
| `GetLastPlayedDateTime` | :101 | SPEC-P | `Select((DateTime?)x.Start)` + `FirstOrDefault` |
| `GetShortestPlay` | :111 | SPEC-P | order by computed duration, project `x.Id` (currently fetches the whole entity and returns `result?.Id` — projection spec is an improvement) |
| `GetLongestPlay` | :122 | SPEC-P | mirror of above |

### 2.5 GameStatisticsRepository — `BoardGameTracker.Core/Games/GameStatisticsRepository.cs` (interface `Games/Interfaces/IGameStatisticsRepository.cs`)

Standalone. This is the **aggregate/chart-heavy** repository — most of it stays hand-written (KEEP), because the Ardalis builder has no `GroupBy`, `Sum`, `Average`, `Max`, or `SelectMany`-then-aggregate support.

| Method | Line | Classification | Notes |
|---|---|---|---|
| `GetPricePerPlay` | :19 | KEEP | anon projection + post-math |
| `GetHighestScore` | :39 | KEEP | `AnyAsync` guard + `SelectMany.MaxAsync` |
| `GetMostWins(gameId)` / `GetMostWins()` | :56 / :61 | KEEP | shared `GetMostWinsInternal` (`:66`) — GroupBy + second lookup |
| `GetAverageScore` | :93 | KEEP | `AverageAsync` |
| `GetExpansionCount` | :110 | KEEP (or SPEC+AGG over `Expansion`) | trivial count with null-if-zero semantics |
| `GetAveragePlayTime` | :119 | KEEP | materializes then averages in memory |
| `GetMeanPayedAsync` | :134 | KEEP | count guard + `AverageAsync` on owned type member |
| `GetTotalPayedAsync` | :148 | KEEP | `SumAsync` on owned type member |
| `GetGamesGroupedByState` | :156 | KEEP | `GroupBy(State)` chart query |
| `GetHighScorePlay` / `GetLowestScorePlay` | :164 / :176 | KEEP | `SelectMany(PlayerSessions)` + order + project |
| `GetPlayByDayChart` | :188 | KEEP | `GroupBy(DayOfWeek)` chart |
| `GetPlayerCountChart` | :197 | KEEP | `Select(count).GroupBy` chart |
| `GetHighestScoringPlayer` / `GetHighestLosingPlayer` / `GetLowestWinning` / `GetLowestScoringPlayer` | :207–:239 | KEEP | `SelectMany` + order over `PlayerSession` — *could* become specs over an `IReadRepository<PlayerSession>` later, but low value; defer |
| `GetMostPlayedGames` | :241 | KEEP (hybrid — see §4.3) | `GroupBy(GameId)` + projection to tuple |

Private helpers `SessionsWithPlayerSessions` (`:261`) and `GameSessionsWithPlayerSessions` (`:268`) remain.

### 2.6 PlayerRepository — `BoardGameTracker.Core/Players/PlayerRepository.cs` (interface `Players/Interfaces/IPlayerRepository.cs`)

Extends `CrudHelper<Player>`.

| Method | Line | Classification | Notes |
|---|---|---|---|
| `GetByIdAsync` (override) | :18 | SPEC (single) | **`AsNoTracking` + `Include(Badges)` — this override is the root cause of confirmed bug C2** (`BACKEND_REVIEW.md:27-34`): `PlayerService.Update` (`PlayerService.cs:64-83`) mutates the detached entity and saves nothing. The migration must produce TWO specs: a no-tracking read spec and a tracked for-update spec (§8.2). |
| `GetAllAsync` (override) | :26 | SPEC | `AsNoTracking`, `OrderBy(Name)` |
| `GetBestGame` | :33 | KEEP | `GroupBy(Session.Game)` over `PlayerSessions` |
| `GetMostPlayedGames` | :44 | KEEP (hybrid — see §4.3) | `GroupBy` + rich projection to `MostPlayedGame` model |
| `GetPlayLengthInMinutes` | :67 | KEEP | `SumAsync` |
| `GetDistinctGameCount` | :75 | KEEP | `Select.Distinct.Count` |
| `CountAsync` | :85 | SPEC+AGG | plain count |
| `GetTotalPlayCount` | :92 | SPEC+AGG | count of sessions containing the player — reuses a `SessionsByPlayerSpec` |
| `GetWinCount` | :99 | SPEC+AGG | count with player+game+won predicate |
| `GetTotalWinCount` | :108 | SPEC+AGG over `PlayerSession` | needs `IReadRepository<PlayerSession>` (composite key — read-only repo, §3.4) |
| `GetTopPlayers` | :116 | KEEP | `GroupBy(PlayerId)` + tuple projection |

### 2.7 SessionRepository — `BoardGameTracker.Core/Sessions/SessionRepository.cs` (interface `Sessions/Interfaces/ISessionRepository.cs`)

Extends `CrudHelper<Session>`.

| Method | Line | Classification | Notes |
|---|---|---|---|
| `CountAsync` | :18 | SPEC+AGG | |
| `CountByPlayer` | :23 | SPEC+AGG | reuses `SessionsByPlayerSpec` |
| `CountByPlayerAndGame` | :30 | SPEC+AGG | |
| `GetByPlayer(playerId, won?)` | :38 | SPEC | tracked + `Include(PlayerSessions)`; conditional extra `Where` — spec ctor takes `bool? won` |
| `GetByPlayerAndGame` | :52 | SPEC | tracked, no includes |
| `GetTotalPlayTime` | :60 | KEEP | `AnyAsync` guard + `SumAsync` |
| `GetMeanPlayTime` | :71 | KEEP | guard + `AverageAsync` |
| `GetByPlayerBatchAsync` | :82 | KEEP | anon projection + in-memory dictionary regroup (used by badge evaluation, `BadgeService.cs:37-38`) |
| `GetByIdAsync` (override) | :111 | SPEC (single) | **Tracked** + `Include(PlayerSessions)` + `Include(Expansions)` — write path for `SessionService.UpdateFromCommand` (`SessionService.cs:94`) |
| `GetRecentSessions` | :119 | SPEC | `AsNoTracking` + `Include(Game)` + `Include(PlayerSessions).ThenInclude(Player)` + `Take(count)` |
| `GetSessionsByDayOfWeek` | :131 | KEEP | `GroupBy` chart |
| `DeleteByPlayerIdAsync` | :139 | KEEP (uses a spec internally) | fetch list + `RemoveRange`, deferred save (called from `PlayerService.Delete` `PlayerService.cs:105` **before** the single `SaveChangesAsync` at `:109` — do not convert to `ExecuteDeleteAsync`, that would break the transactional delete) |
| `Update` (override) | :148 | KEEP | 100-line domain sync routine (`UpdateLocationAsync` `:168`, `SyncPlayerSessions` `:184`, `SyncExpansionsAsync` `:221`) — not a query at all |

### 2.8 Small repositories

| Repository | File | Methods | Classification |
|---|---|---|---|
| **LoanRepository** | `BoardGameTracker.Core/Loans/LoanRepository.cs` | `GetAllAsync` override (`:17`, ordered by `LoanDate` desc, **tracked** — flagged in `BACKEND_REVIEW.md:167` as dropping `AsNoTracking`); `CountActiveLoans` (`:24`) | SPEC; SPEC+AGG |
| **LocationRepository** | `BoardGameTracker.Core/Locations/LocationRepository.cs` | `GetAllAsync` override (`:17`, `Include(Sessions)`, ordered, tracked); `CountAsync` (`:25`) | SPEC; SPEC+AGG |
| **LanguageRepository** | `BoardGameTracker.Core/Languages/LanguageRepository.cs:7` | none — pure `CrudHelper<Language>` passthrough | CRUD only; becomes a direct `IRepository<Language>` consumer |
| **BadgeRepository** | `BoardGameTracker.Core/Badges/BadgeRepository.cs` | `GetPlayerBadgesAsync` (`:16`) SPEC; `GetPlayerBadgesBatchAsync` (`:23`) KEEP (projection + regroup); `AwardBatchToPlayer` (`:49`) KEEP (multi-fetch + domain mutation + deferred save — see §8.1) | mixed |
| **GameNightRepository** | `BoardGameTracker.Core/GameNights/GameNightRepository.cs` | `GetByIdAsync` override (`:20`, tracked, 4 includes + ThenInclude via helper `:69-77`) SPEC; `GetAllAsync` override (`:26`, same includes + `AsNoTracking` + ordered) SPEC; `GetRsvpByIdAsync` (`:34`, `Set<GameNightRsvp>()`) SPEC; `UpdateRsvpAsync` (`:41`) CRUD; `GetFutureGameNightsCountAsync` (`:47`, **`_dateTimeProvider.UtcNow` in expression at `:51`** — see §8.6) SPEC+AGG; `GetRsvpByPlayerAndGameAsync` (`:55`) SPEC; `GetGameNightByLinkId` (`:63`) SPEC | mixed |
| **DashboardRepository** | `BoardGameTracker.Core/Dashboard/DashboardRepository.cs:5` | **empty class**, empty interface (`Dashboard/Interfaces/IDashboardRepository.cs`) — registered at `ServiceCollectionExtensions.cs:80` but never used (`DashboardService.cs` injects game/player/session repos instead) | DELETE in cleanup phase |

### 2.9 Repositories that stay entirely hand-written

| Repository | File | Why |
|---|---|---|
| **CompareRepository** | `BoardGameTracker.Core/Compares/CompareRepository.cs` (9 methods, `:18-224`) | Every method is a two-player statistical aggregate: paired `CountAsync` calls (`GetDirectWins` `:18`), `GroupBy` + projection (`GetMostWonGame` `:35`, `GetPreferredGame` `:86`), `SumAsync` (`:77`), complex anonymous projections with client-side post-processing (`GetClosestGame` `:173`). None map to the spec builder. **Optional refinement:** the "sessions with both players" predicate repeated in all 9 methods can be extracted once into a `SessionsWithBothPlayersSpec` and applied inside the repo via `_context.Sessions.WithSpecification(spec)` — worthwhile de-duplication, not required. `CompareService` (`Compares/CompareService.cs`) is unaffected either way. |
| **ConfigRepository** | `BoardGameTracker.Core/Configuration/ConfigRepository.cs` | Key-value store semantics, not entity-query semantics: `SetConfigValueAsync` uses **`ExecuteUpdateAsync`** (`:40-42`) and **saves internally** (`:48`, `:82`); `GetConfigValueAsync` merges environment variables (`:22`). Leave 100% untouched. It does not extend `CrudHelper`. |
| **Auth data access** | `Auth/TokenService.cs` (saves at `:69,85,99,118`), `Auth/OidcService.cs`, `Auth/OidcProviderService.cs` | Direct `MainDbContext` usage, self-saving, Identity-adjacent. Out of scope. |

### 2.10 Service-side save-ordering facts (constrain the design)

- `SessionService.Create` (`Sessions/SessionService.cs:32-41`): `CreateAsync(session)` (no save) → `AwardBadgesAsync(session)` (queries + in-memory mutations, `BadgeService.cs:25-58`, awarding via `BadgeRepository.AwardBatchToPlayer` which mutates a tracked graph without saving, `BadgeRepository.cs:49-67`) → **one** `SaveChangesAsync` (`:37`). `BACKEND_REVIEW.md:20-25` documents that badges are therefore evaluated *before* the session is persisted ("one session late" bugs). **The migration must preserve this ordering exactly** — fixing the badge bug is a separate task; a repository that auto-saves on `Add` would silently change badge behavior (see §3.3, §8.1).
- `PlayerService.Delete` (`Players/PlayerService.cs:96-111`): `DeleteByPlayerIdAsync` + `DeleteAsync` + single `SaveChangesAsync` — one atomic save.
- `BggImportService` (`Games/BggImportService.cs:67,146`): batch import with a single save — one bad row rolls back the batch (documented at `BACKEND_REVIEW.md:40-41`); auto-save-per-add would change that semantics too.

---

## 3. Target architecture

### 3.1 Folder and naming conventions

- Spec classes live **next to the aggregate they query**, one class per query:
  `BoardGameTracker.Core/{Aggregate}/Specifications/{Name}Spec.cs`
  e.g. `BoardGameTracker.Core/Games/Specifications/GameByIdWithDetailsSpec.cs`, namespace `BoardGameTracker.Core.Games.Specifications`.
- Naming: `{Entity}{Criteria}[With{Includes}][For{Purpose}]Spec` in PascalCase (Microsoft naming):
  - `GameByIdWithDetailsSpec`, `GameByBggIdSpec`, `GamesOverviewSpec`, `RecentlyAddedGamesSpec`, `GamesWithNoRecentSessionsSpec`, `ShameGamesSpec`, `GamesByIdsSpec`
  - `PlayerByIdWithBadgesSpec` (read) vs `PlayerByIdForUpdateSpec` (tracked write)
  - `SessionsByGamePagedSpec`, `SessionsByPlayerSpec`, `RecentSessionsSpec`, `SessionByIdWithDetailsSpec`
  - `ActiveLoansSpec`, `LoansOrderedByDateSpec`, `LocationsWithSessionsSpec`
  - `GameNightByIdWithDetailsSpec`, `GameNightsOverviewSpec`, `FutureGameNightsSpec`, `RsvpByIdSpec`, `RsvpByPlayerAndGameNightSpec`, `GameNightByLinkIdSpec`
  - `BadgesByPlayerSpec`
- Projection specs return the existing model types in `BoardGameTracker.Common/Models` (e.g. `ShameGame`, `MostPlayedGame`) — no new DTOs needed.
- All parameters (ids, counts, cutoff dates) are constructor arguments. **Never** resolve `IDateTimeProvider` inside a spec — pass the computed `DateTime` in (§8.6).

### 3.2 The new generic repository

Create in `BoardGameTracker.Core/Datastore`:

```csharp
// Datastore/Interfaces/IReadRepository.cs
using Ardalis.Specification;

namespace BoardGameTracker.Core.Datastore.Interfaces;

public interface IReadRepository<T> where T : class
{
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default);
    Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default);
    Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
}
```

```csharp
// Datastore/Interfaces/IRepository.cs
using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Core.Datastore.Interfaces;

public interface IRepository<T> : IReadRepository<T> where T : HasId
{
    Task<T?> GetByIdAsync(int id);          // plain tracked fetch, same as CrudHelper today
    Task<List<T>> GetAllAsync();            // AsNoTracking, same as CrudHelper today
    Task<T> CreateAsync(T entity);          // Add WITHOUT save — preserves UnitOfWork flow
    Task CreateRangeAsync(List<T> entities);
    Task<T> Update(T entity);
    Task<bool> DeleteAsync(int id);         // Find + Remove WITHOUT save
}
```

```csharp
// Datastore/EfRepository.cs
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Datastore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

public class EfRepository<T> : IRepository<T> where T : HasId
{
    private readonly MainDbContext _context;
    private readonly ISpecificationEvaluator _evaluator;

    public EfRepository(MainDbContext context)
        : this(context, SpecificationEvaluator.Default)
    {
    }

    protected EfRepository(MainDbContext context, ISpecificationEvaluator evaluator)
    {
        _context = context;
        _evaluator = evaluator;
    }

    // --- spec-based reads ---
    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);

    public Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).ToListAsync(cancellationToken);

    public Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification).ToListAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification, evaluateCriteriaOnly: true).CountAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => _context.Set<T>().CountAsync(cancellationToken);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ApplySpecification(specification, evaluateCriteriaOnly: true).AnyAsync(cancellationToken);

    // --- CRUD, IDENTICAL semantics to CrudHelper (no auto-save) ---
    public virtual Task<T?> GetByIdAsync(int id)
        => _context.Set<T>().FirstOrDefaultAsync(x => x.Id == id);

    public virtual Task<List<T>> GetAllAsync()
        => _context.Set<T>().AsNoTracking().ToListAsync();

    public virtual async Task<T> CreateAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        return entity;
    }

    public async Task CreateRangeAsync(List<T> entities)
    {
        await _context.Set<T>().AddRangeAsync(entities);
    }

    public virtual Task<T> Update(T entity)
    {
        _context.Set<T>().Update(entity);
        return Task.FromResult(entity);
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _context.Set<T>().Remove(entity);
        return true;
    }

    protected IQueryable<T> ApplySpecification(ISpecification<T> specification, bool evaluateCriteriaOnly = false)
        => _evaluator.GetQuery(_context.Set<T>().AsQueryable(), specification, evaluateCriteriaOnly);

    protected IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification)
        => _evaluator.GetQuery(_context.Set<T>().AsQueryable(), specification);
}
```

Plus a read-only variant for composite-key entities:

```csharp
// Datastore/EfReadRepository.cs — for entities WITHOUT an int Id (PlayerSession)
public class EfReadRepository<T> : IReadRepository<T> where T : class
{
    // same spec-based read methods as above; no CRUD block
}
```

DI (add to `ServiceCollectionExtensions.AddCoreService`, near line 73):

```csharp
serviceCollection.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
serviceCollection.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
```

**How it replaces `CrudHelper<T>`:** the CRUD block above is a line-for-line behavioral copy of `CrudHelper.cs:16-53`. During migration, each per-aggregate repository changes its base class from `CrudHelper<T>` to `EfRepository<T>`, and `ICrudHelper<T>` members on the per-aggregate interfaces are re-pointed to `IRepository<T>` (`public interface IGameRepository : IRepository<Game>`). When the last repository has moved, delete `CrudHelper.cs` and `ICrudHelper.cs`.

### 3.3 UnitOfWork decision: **keep it, do not adopt Ardalis `RepositoryBase<T>` directly**

Ardalis's shipped `RepositoryBase<T>` calls `SaveChangesAsync` inside `AddAsync`/`UpdateAsync`/`DeleteAsync`. This codebase deliberately defers saves to a single `IUnitOfWork.SaveChangesAsync()` per use case, and **behavior depends on it**:

- **Badge evaluation save order** (`SessionService.cs:35-37`): the session is added (unsaved) → badge evaluators run against the pre-save state → one save commits session + badge awards atomically. Adopting an auto-saving repository would (a) commit the session before badge evaluation — accidentally *changing* the documented "one session late" badge behavior (`BACKEND_REVIEW.md:20-25`) in an uncontrolled way, and (b) split one transaction into several, so a failed badge award would leave a session saved without its awards. If the team wants to fix the badge-lateness bug, do it deliberately (`CreateAsync → SaveChangesAsync → AwardBadgesAsync → SaveChangesAsync` per the review's suggested fix), **as its own PR, not as a migration side effect**.
- **Atomic player delete** (`PlayerService.cs:105-109`) and **batch BGG import** (`BggImportService.cs:146`) similarly rely on one deferred save.

Decision: `IUnitOfWork` / `UnitOfWork` (`Datastore/UnitOfWork.cs`) stay exactly as-is; `EfRepository<T>` (§3.2) never saves. We use the Ardalis **evaluator** (the valuable part) without the Ardalis **repository base** (the part that conflicts with the app's transaction model). Do not add `Ardalis.Specification`'s `IRepositoryBase<T>` to DI at all, to prevent accidental use.

### 3.4 What happens to the per-aggregate repository interfaces

Two-stage end state:

- **Stage A (during migration, per aggregate):** the per-aggregate interface keeps its exact signatures; the implementation body becomes spec applications (`return ListAsync(new GamesOverviewSpec());`). Services and their tests are untouched. This is the "old and new side by side" mechanism — at any commit, every service still compiles against the same interface.
- **Stage B (end state, per aggregate):** trivial pass-through members are deleted from the per-aggregate interface; services call `IRepository<T>` + specs directly (e.g. `GameService` takes `IRepository<Game>` and calls `SingleOrDefaultAsync(new GameByIdWithDetailsSpec(id))`). The per-aggregate interface **survives only if it still owns KEEP methods**:
  - `IGameRepository` → survives, reduced to `AddGameCategoriesIfNotExists`/`AddGameMechanicsIfNotExists`/`AddPeopleIfNotExists`/`DeleteExpansion`.
  - `IGameStatisticsRepository`, `ICompareRepository`, `IConfigRepository` → survive intact (aggregates/charts/key-value).
  - `ISessionRepository` → survives, reduced to `GetTotalPlayTime`, `GetMeanPlayTime`, `GetByPlayerBatchAsync`, `GetSessionsByDayOfWeek`, `DeleteByPlayerIdAsync`, `Update` override.
  - `IPlayerRepository` → survives, reduced to `GetBestGame`, `GetMostPlayedGames`, `GetPlayLengthInMinutes`, `GetDistinctGameCount`, `GetTopPlayers`.
  - `IBadgeRepository` → survives, reduced to `GetPlayerBadgesBatchAsync`, `AwardBatchToPlayer`.
  - `ILoanRepository`, `ILocationRepository`, `ILanguageRepository`, `IGameNightRepository` (RSVP helpers may keep it), `IDashboardRepository` → dissolve/delete.

Stage B is optional per aggregate and can trail Stage A indefinitely; the plan's phases (§5) mandate Stage A everywhere and Stage B where the interface fully dissolves.

### 3.5 Hybrid strategy for un-mappable queries

| Category | Examples | Handling |
|---|---|---|
| `GroupBy` chart/statistics queries | `GetGamesGroupedByState` (`GameStatisticsRepository.cs:156`), `GetPlayByDayChart` (`:188`), `GetSessionsByDayOfWeek` (`SessionRepository.cs:131`), `GetTopPlayers` (`PlayerRepository.cs:116`), `GetMostPlayedGames` (both), `GetBestGame` | KEEP as hand-written repo methods. Where a spec-able filter precedes the GroupBy, apply the spec inside the repo via `_context.Sessions.WithSpecification(spec)` to de-duplicate predicates (see §4.3). |
| `Sum`/`Average`/`Max` aggregates | `GetTotalPlayTime`, `GetMeanPlayTime`, `GetTotalPayedAsync`, `GetHighestScore`, `GetPlayLengthInMinutes` | KEEP. The spec builder has no aggregate terminal operators by design. |
| `ExecuteUpdateAsync` | `ConfigRepository.SetConfigValueAsync` (`ConfigRepository.cs:40-42`) | KEEP, repository untouched. |
| Multi-step domain operations | `SessionRepository.Update` (`:148`), `DeleteByPlayerIdAsync` (`:139`), `BadgeRepository.AwardBatchToPlayer` (`:49`), `GameRepository.DeleteExpansion` (`:80`) | KEEP. These are commands, not queries. `DeleteByPlayerIdAsync` may use `ListAsync(new SessionsByPlayerSpec(playerId))` internally to fetch, then `RemoveRange` — but the RemoveRange + deferred-save shape stays. |
| Batch regroup queries | `GetByPlayerBatchAsync` (`SessionRepository.cs:82`), `GetPlayerBadgesBatchAsync` (`BadgeRepository.cs:23`) | KEEP (anonymous projection + dictionary building). |

### 3.6 Projection specs replace hand-written Selects

Every hand-written `.Select(...)` that projects to a named model (not a `GroupBy` result) becomes a `Specification<TEntity, TModel>`:

- `GetShameGames` → `ShameGamesSpec : Specification<Game, ShameGame>` (§4.2)
- `GetLastPlayedDateTime` → `LastPlayedDateSpec : Specification<Session, DateTime?>`
- `GetShortestPlay`/`GetLongestPlay` → `ShortestPlayIdSpec` / `LongestPlayIdSpec : Specification<Session, int?>` with `Query.Select(x => (int?)x.Id)` — an *improvement*: today the whole entity is materialized just to read `.Id` (`GameSessionRepository.cs:111-131`).

`GroupBy` projections (`MostPlayedGame`, top players, most wins) cannot be projection specs — they stay in repos per §3.5.

---

## 4. Before/after examples

### 4.1 Include-heavy single fetch — `GameRepository.GetByIdAsync`

**Before** (`BoardGameTracker.Core/Games/GameRepository.cs:52-61`):

```csharp
public override Task<Game?> GetByIdAsync(int id)
{
    return _context.Games
        .Include(x => x.Accessories)
        .Include(x => x.Categories)
        .Include(x => x.Expansions)
        .Include(x => x.Mechanics)
        .Include(x => x.People)
        .SingleOrDefaultAsync(x => x.Id == id);
}
```

**After — spec** (`BoardGameTracker.Core/Games/Specifications/GameByIdWithDetailsSpec.cs`):

```csharp
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Specifications;

/// <summary>
/// Loads a game with its full detail graph. Tracked on purpose:
/// GameService.UpdateGame and UpdateGameExpansions mutate the result and rely on change tracking.
/// </summary>
public sealed class GameByIdWithDetailsSpec : SingleResultSpecification<Game>
{
    public GameByIdWithDetailsSpec(int gameId)
    {
        Query
            .Where(x => x.Id == gameId)
            .Include(x => x.Accessories)
            .Include(x => x.Categories)
            .Include(x => x.Expansions)
            .Include(x => x.Mechanics)
            .Include(x => x.People);
        // No AsNoTracking: this spec serves write paths (GameService.cs:109, :141, :165).
        // No AsSplitQuery needed: split query is the global default (ServiceCollectionExtensions.cs:135).
    }
}
```

**After — repository (Stage A: interface unchanged):**

```csharp
public class GameRepository : EfRepository<Game>, IGameRepository
{
    public override Task<Game?> GetByIdAsync(int id)
    {
        return SingleOrDefaultAsync(new GameByIdWithDetailsSpec(id));
    }
    // ...
}
```

**After — calling service (Stage B: service uses the generic repo directly):**

```csharp
// GameService.cs — constructor takes IRepository<Game> instead of IGameRepository for query members
public Task<Game?> GetGameById(int id)
{
    _logger.LogDebug("Fetching game {GameId}", id);
    return _gameRepository.SingleOrDefaultAsync(new GameByIdWithDetailsSpec(id));
}
```

### 4.2 Filtered list + projection — shame games (three duplicated predicates become one spec)

**Before** (`GameRepository.cs:108-144` — the same predicate appears at `:112`, `:120`, and `:128`, and the projection at `:129-141` runs a correlated subquery through `_context.Sessions`):

```csharp
public Task<List<ShameGame>> GetShameGames(DateTime cutoffDate)
{
    return _context.Games
        .AsNoTracking()
        .Where(g => g.State == GameState.Owned && !_context.Sessions.Any(s => s.GameId == g.Id && s.Start >= cutoffDate))
        .Select(g => new ShameGame
        {
            Id = g.Id,
            Title = g.Title,
            Image = g.Image,
            AdditionDate = g.AdditionDate,
            Price = g.BuyingPrice != null ? g.BuyingPrice.Amount : null,
            LastSessionDate = _context.Sessions
                .Where(s => s.GameId == g.Id)
                .OrderByDescending(s => s.Start)
                .Select(s => (DateTime?)s.Start)
                .FirstOrDefault()
        })
        .OrderBy(g => g.Title)
        .ToListAsync();
}
```

**After — filter spec** (shared by the list, the count, and the projection). Note the mandatory rewrite of the cross-DbSet subquery `_context.Sessions.Any(...)` to the `g.Sessions` navigation (configured at `MainDbContext.cs:168-171`; produces the same SQL `NOT EXISTS`):

```csharp
// Games/Specifications/GamesWithNoRecentSessionsSpec.cs
public sealed class GamesWithNoRecentSessionsSpec : Specification<Game>
{
    public GamesWithNoRecentSessionsSpec(DateTime cutoffDate)
    {
        Query
            .Where(g => g.State == GameState.Owned && !g.Sessions.Any(s => s.Start >= cutoffDate))
            .OrderBy(g => g.Title)
            .AsNoTracking();
    }
}
```

```csharp
// Games/Specifications/ShameGamesSpec.cs — projection spec
public sealed class ShameGamesSpec : Specification<Game, ShameGame>
{
    public ShameGamesSpec(DateTime cutoffDate)
    {
        Query
            .Where(g => g.State == GameState.Owned && !g.Sessions.Any(s => s.Start >= cutoffDate))
            .OrderBy(g => g.Title)
            .AsNoTracking();

        Query.Select(g => new ShameGame
        {
            Id = g.Id,
            Title = g.Title,
            Image = g.Image,
            AdditionDate = g.AdditionDate,
            Price = g.BuyingPrice != null ? g.BuyingPrice.Amount : null,
            LastSessionDate = g.Sessions
                .OrderByDescending(s => s.Start)
                .Select(s => (DateTime?)s.Start)
                .FirstOrDefault()
        });
    }
}
```

**After — calling service** (`ShameService`, `Games/ShameService.cs:29-49`, Stage B):

```csharp
public async Task<int> CountShelfOfShameGames()
{
    var enabled = await _configRepository.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled);
    if (!enabled)
    {
        return 0;
    }

    var months = await _configRepository.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths);
    var cutoffDate = _dateTimeProvider.UtcNow.AddMonths(-months);
    return await _gameRepository.CountAsync(new GamesWithNoRecentSessionsSpec(cutoffDate));
}

public async Task<List<ShameGame>> GetShameGames()
{
    var months = await _configRepository.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths);
    var cutoffDate = _dateTimeProvider.UtcNow.AddMonths(-months);
    return await _gameRepository.ListAsync(new ShameGamesSpec(cutoffDate));
}
```

This deletes `GetGamesWithNoRecentSessions`, `CountGamesWithNoRecentSessions`, and `GetShameGames` from `IGameRepository` — three methods, one predicate, two specs. (Duplicating the `Where` between the filter spec and the projection spec is acceptable; if desired, extract the expression into a static `internal static Expression<Func<Game, bool>> NoRecentSessions(DateTime cutoff)` helper both specs share.)

A simpler filtered-list example for reference — `GetRecentlyAddedGames` (`GameRepository.cs:98-106`):

```csharp
public sealed class RecentlyAddedGamesSpec : Specification<Game>
{
    public RecentlyAddedGamesSpec(int count)
    {
        Query
            .Where(x => x.AdditionDate != null)
            .OrderByDescending(x => x.AdditionDate)
            .Take(count)
            .AsNoTracking();
    }
}
// DashboardService.cs:49 becomes:
// var recentlyAddedGames = await _gameRepository.ListAsync(new RecentlyAddedGamesSpec(4));
```

### 4.3 Projection with GroupBy — `PlayerRepository.GetMostPlayedGames` (the honest limit of the pattern)

**Before** (`Players/PlayerRepository.cs:44-64`): `GroupBy(x => x.Session.Game)` over `PlayerSessions` with a rich projection into `MostPlayedGame`.

**Why this cannot be a pure projection spec:** `Specification<T, TResult>.Select` is an element-wise map; the Ardalis builder deliberately has **no `GroupBy` operator** (grouping changes the queryable's element type, which the evaluator pipeline does not model). Do not try to force it.

**After — hybrid:** the method stays on the repository, but the *filter* becomes a reusable spec applied via `WithSpecification`, and the GroupBy stays hand-written:

```csharp
// Players/Specifications/PlayerSessionsByPlayerSpec.cs
public sealed class PlayerSessionsByPlayerSpec : Specification<PlayerSession>
{
    public PlayerSessionsByPlayerSpec(int playerId)
    {
        Query
            .Where(x => x.PlayerId == playerId)
            .AsNoTracking();
    }
}
```

```csharp
// PlayerRepository.cs (KEEP method, spec-assisted)
public async Task<List<MostPlayedGame>> GetMostPlayedGames(int playerId, int count)
{
    return await _dbContext.PlayerSessions
        .WithSpecification(new PlayerSessionsByPlayerSpec(playerId))
        .GroupBy(x => x.Session.Game)
        .OrderByDescending(x => x.Count())
        .Take(count)
        .Select(x => new MostPlayedGame
        {
            Id = x.Key.Id,
            Title = x.Key.Title,
            Image = x.Key.Image ?? string.Empty,
            TotalSessions = x.Count(),
            TotalWins = x.Count(ps => ps.Won),
            WinningPercentage = x.Count() > 0
                ? (double)x.Count(ps => ps.Won) / x.Count() * 100
                : 0
        })
        .ToListAsync();
}
```

The calling `PlayerStatisticsService` does not change. The same treatment applies to `GameStatisticsRepository.GetMostPlayedGames` (`:241`), `GetTopPlayers` (`PlayerRepository.cs:116`), and `GetBestGame` (`:33`). The payoff is smaller here — the spec only carries the `Where` — so treat spec-assistance of KEEP methods as optional polish, mandatory only where the same predicate is reused elsewhere (e.g. `PlayerSessionsByPlayerSpec` is also the filter for `GetTotalWinCount`, `PlayerRepository.cs:108-114`, via `IReadRepository<PlayerSession>.CountAsync(new WonPlayerSessionsByPlayerSpec(id))`).

### 4.4 Aggregate — `LoanRepository.CountActiveLoans`

**Before** (`Loans/LoanRepository.cs:24-29`):

```csharp
public Task<int> CountActiveLoans()
{
    return _context.Loans
        .Where(x => x.ReturnedDate == null)
        .CountAsync();
}
```

**After — spec:**

```csharp
// Loans/Specifications/ActiveLoansSpec.cs
public sealed class ActiveLoansSpec : Specification<Loan>
{
    public ActiveLoansSpec()
    {
        Query.Where(x => x.ReturnedDate == null);
    }
}
```

**After — calling service** (`LoanService.cs:95-98`, Stage B — `ILoanRepository` dissolves into `IRepository<Loan>`):

```csharp
public Task<int> CountActiveLoans()
{
    return _loanRepository.CountAsync(new ActiveLoansSpec());
}
```

And the ordered list override (`LoanRepository.cs:17-22`) becomes `LoansOrderedByDateSpec` with `Query.OrderByDescending(x => x.LoanDate).AsNoTracking()` — adding the `AsNoTracking` that `BACKEND_REVIEW.md:167` flags as missing today (safe: `GetLoans` → `LoanService.cs:26-30` is a pure read; writes go through `GetByIdAsync`).

---

## 5. Migration plan (phases)

### 5.0 Phase 0 — Packages and infrastructure (no behavior change)

1. Add packages (versions verified compatible with net8.0 + EF Core 9.0.16 on 2026-07-04):
   - `BoardGameTracker.Core.csproj`: `<PackageReference Include="Ardalis.Specification" Version="9.3.1" />` and `<PackageReference Include="Ardalis.Specification.EntityFrameworkCore" Version="9.3.1" />`
   - `BoardGameTracker.Tests.csproj`: `<PackageReference Include="Ardalis.Specification" Version="9.3.1" />` (base package only — the in-memory evaluator lives there).
   - Do NOT add either package to `BoardGameTracker.Common` — entities stay persistence-ignorant; specs live in Core.
2. Create `IReadRepository<T>`, `IRepository<T>`, `EfRepository<T>`, `EfReadRepository<T>` per §3.2.
3. Register open generics in `ServiceCollectionExtensions.AddCoreService` (§3.2). `ICrudHelper<T>`/`CrudHelper<T>` remain untouched and in use.
4. Add a smoke unit test: `EfRepositoryTests` proving `CreateAsync` does **not** save (mock-free, using `Microsoft.EntityFrameworkCore.InMemory` which is already referenced by `BoardGameTracker.Tests.csproj:18`).
5. Build + full test run. Zero production call sites changed.

### 5.1 Phase 1 — Pilot: the Loan aggregate

**Why Loans:** smallest real repository (2 custom methods, `LoanRepository.cs:17-29`), one calling service (`LoanService`), one controller, existing test suite (`Tests/Services/LoanServiceTests.cs`, `Tests/Controllers/LoansControllerTests.cs`), and it exercises every migration mechanic once: an override-to-spec, an aggregate-to-spec, CRUD passthrough, deferred saves via `IUnitOfWork` (`LoanService.cs:51,67,83,92`), and Stage B interface dissolution.

1. Create `Loans/Specifications/LoansOrderedByDateSpec.cs` and `ActiveLoansSpec.cs` + spec unit tests (in-memory evaluator).
2. `LoanRepository : EfRepository<Loan>, ILoanRepository`; body shrinks to spec applications (Stage A). Run tests.
3. Stage B: change `LoanService` to depend on `IRepository<Loan>`; delete `ILoanRepository` + `LoanRepository` + their DI line (`ServiceCollectionExtensions.cs:79`); update `LoanServiceTests` to mock `IRepository<Loan>` (§7.2).
4. Manual verification: loans list ordering, active-loan count on dashboard, create/return/delete loan round trip.

**Exit criterion:** all tests green, `git diff` shows no change to any non-Loan service, and the team signs off on the spec/test ergonomics before proceeding.

### 5.2 Phase 2 — Low-risk small aggregates

Order: **Locations → Languages → Badges (Stage A only) → GameNights.**

- Locations: `LocationsWithSessionsSpec` (keeps tracked semantics — the include is consumed for counts in DTO mapping; verify with `LocationController` usage), `CountAsync` → generic. Dissolve `ILocationRepository` (Stage B).
- Languages: no specs needed; replace `ILanguageRepository` with `IRepository<Language>` in `LanguageService`, delete repo + interface (Stage B).
- Badges: `BadgesByPlayerSpec`; `GetPlayerBadgesBatchAsync` and `AwardBatchToPlayer` are KEEP → `BadgeRepository : EfRepository<Badge>, IBadgeRepository` survives (Stage A permanent). **Do not touch the call order in `BadgeService.AwardBadgesAsync` (`BadgeService.cs:25-58`).**
- GameNights: `GameNightByIdWithDetailsSpec` (tracked — RSVP/update flows mutate the graph, `GameNightService.cs:65-127`), `GameNightsOverviewSpec` (no-tracking + ordered), `GameNightByLinkIdSpec`, `FutureGameNightsSpec(DateTime now)` (§8.6), `RsvpByIdSpec`/`RsvpByPlayerAndGameNightSpec` over `IReadRepository<GameNightRsvp>` — note `GameNightRsvp` is reached via `_context.Set<GameNightRsvp>()` today (`GameNightRepository.cs:36`); confirm whether it derives from `HasId` — if yes use `IRepository<GameNightRsvp>`, if no use `EfReadRepository` + keep the update helper on the surviving repo.

### 5.3 Phase 3 — Players (includes the deliberate C2 bug fix)

- Specs: `PlayersOrderedByNameSpec`, `PlayerByIdWithBadgesSpec` (**`AsNoTracking`, read path**), `PlayerByIdForUpdateSpec` (**tracked, no includes**), `PlayerSessionsByPlayerSpec`, `WonPlayerSessionsByPlayerSpec`.
- `PlayerService.Update` (`PlayerService.cs:64-83`) switches its fetch to `PlayerByIdForUpdateSpec` — **this intentionally fixes confirmed bug C2** (`BACKEND_REVIEW.md:27-34`). Flag it in the PR description as a behavior change (player edits will start persisting). `PlayerService.Get`/`Delete` keep the no-tracking read spec (`Delete` re-deletes by id via `DeleteAsync(player.Id)`, `PlayerService.cs:108`, so a detached read is fine there).
- KEEP methods (`GetBestGame`, `GetMostPlayedGames`, `GetPlayLengthInMinutes`, `GetDistinctGameCount`, `GetTopPlayers`) stay on a slimmed `PlayerRepository : EfRepository<Player>, IPlayerRepository`.
- Add an EF-InMemory round-trip test: update a player, save, re-fetch, assert the name persisted (the review notes mocked tests cannot catch C2).

### 5.4 Phase 4 — Games and Sessions (the big one; split into 3 PRs)

- **PR 4a — Game queries:** `GameByIdWithDetailsSpec`, `GameByBggIdSpec`, `GamesOverviewSpec`, `RecentlyAddedGamesSpec`, `GamesWithNoRecentSessionsSpec`, `ShameGamesSpec`, `GamesByIdsSpec`, `ExpansionsByIdsSpec` (+ `IRepository<Expansion>` consumer or keep on repo). `GameRepository : EfRepository<Game>` keeps the four KEEP write helpers. `ShameService` moves to spec calls (§4.2).
- **PR 4b — GameSessionRepository:** all 6 SPEC/SPEC-P methods (§2.4) become specs (`SessionsByGamePagedSpec`, `SessionsByGameSinceSpec(cutoff)`, `SessionsByGameSpec(count?)`, `SessionsByPlayerRecentFirstSpec(count?)`, `LastPlayedDateSpec`, `ShortestPlayIdSpec`, `LongestPlayIdSpec`); the 3 KEEP methods stay. The `IDateTimeProvider` dependency moves out of the repository into callers (`GameChartService` et al.) — the repo may no longer need it at all.
- **PR 4c — SessionRepository:** `SessionByIdWithDetailsSpec` (tracked), `RecentSessionsSpec`, `SessionsByPlayerSpec(won?)`, `SessionsByPlayerAndGameSpec`; count methods → `CountAsync(spec)`. `Update`, `DeleteByPlayerIdAsync`, `GetTotalPlayTime`, `GetMeanPlayTime`, `GetByPlayerBatchAsync`, `GetSessionsByDayOfWeek` are KEEP. **Re-run the full badge evaluator test suite (`Tests/Evaluators/*`, `Tests/Services/BadgeServiceTests.cs`, `Tests/Services/SessionServiceTests.cs`) after this PR** — session fetch shapes feed badge evaluation.

### 5.5 Phase 5 — Hybrid holdouts polish (optional, low priority)

- `CompareRepository`: extract `SessionsWithBothPlayersSpec(p1, p2)` and apply via `WithSpecification` in all 9 methods (pure de-duplication; no interface change; `CompareServiceTests` untouched).
- `GameStatisticsRepository`: optionally spec-assist filters as in §4.3. No interface changes.

### 5.6 Phase 6 — Cleanup

1. Delete `CrudHelper.cs` and `ICrudHelper.cs` (nothing may reference them — enforce with a solution-wide search).
2. Delete `DashboardRepository`/`IDashboardRepository` (dead code, §2.8) and their DI registration (`ServiceCollectionExtensions.cs:80`).
3. Sweep for now-redundant explicit `AsSplitQuery()` (only `GameRepository.cs:45` had one; global default covers it — keeping it in the spec is also fine, just be consistent).
4. Update `CODE_REVIEW.md`/`BACKEND_REVIEW.md` notes (C2 fixed in Phase 3; LoanRepository tracking note fixed in Phase 1).
5. Add an architecture note to the repo docs: "queries = specs; aggregates/GroupBy/commands = repo methods; saves = IUnitOfWork only."

**Rollout safety rule for every phase:** one aggregate per PR; each PR leaves `main` fully working because Stage A never changes public interfaces, and Stage B changes exactly one service + its tests in the same PR.

---

## 6. Per-repository migration checklist

| Repo | Method (file:line) | Target spec / handling | Notes & gotchas |
|---|---|---|---|
| LoanRepository | `GetAllAsync` :17 | `LoansOrderedByDateSpec` | Add `AsNoTracking` (fixes `BACKEND_REVIEW.md:167`) |
| LoanRepository | `CountActiveLoans` :24 | `ActiveLoansSpec` + `CountAsync(spec)` | Pilot |
| LocationRepository | `GetAllAsync` :17 | `LocationsWithSessionsSpec` | Keep include; check whether DTO mapping needs `Sessions` materialized |
| LocationRepository | `CountAsync` :25 | generic `CountAsync()` | |
| LanguageRepository | — | dissolve into `IRepository<Language>` | |
| BadgeRepository | `GetPlayerBadgesAsync` :16 | `BadgesByPlayerSpec` | |
| BadgeRepository | `GetPlayerBadgesBatchAsync` :23 | KEEP | |
| BadgeRepository | `AwardBatchToPlayer` :49 | KEEP | No save inside; preserves §3.3 ordering |
| GameNightRepository | `GetByIdAsync` :20 | `GameNightByIdWithDetailsSpec` | **Tracked** (RSVP mutation path) |
| GameNightRepository | `GetAllAsync` :26 | `GameNightsOverviewSpec` | NoTracking + `OrderByDescending(StartDate)` |
| GameNightRepository | `GetRsvpByIdAsync` :34 | `RsvpByIdSpec` | Entity accessed via `Set<GameNightRsvp>()`; verify `HasId` |
| GameNightRepository | `UpdateRsvpAsync` :41 | generic `Update` | |
| GameNightRepository | `GetFutureGameNightsCountAsync` :47 | `FutureGameNightsSpec(DateTime now)` + `CountAsync` | Pass `now` in; remove `IDateTimeProvider` from expression (§8.6) |
| GameNightRepository | `GetRsvpByPlayerAndGameAsync` :55 | `RsvpByPlayerAndGameNightSpec` | |
| GameNightRepository | `GetGameNightByLinkId` :63 | `GameNightByLinkIdSpec` | Tracked (RSVP flow) |
| PlayerRepository | `GetByIdAsync` :18 | `PlayerByIdWithBadgesSpec` (NoTracking) **and** `PlayerByIdForUpdateSpec` (tracked) | **Fixes bug C2** — flag as behavior change |
| PlayerRepository | `GetAllAsync` :26 | `PlayersOrderedByNameSpec` | |
| PlayerRepository | `GetBestGame` :33 | KEEP | GroupBy |
| PlayerRepository | `GetMostPlayedGames` :44 | KEEP (spec-assist filter, §4.3) | GroupBy |
| PlayerRepository | `GetPlayLengthInMinutes` :67 | KEEP | Sum |
| PlayerRepository | `GetDistinctGameCount` :75 | KEEP | Distinct+Count |
| PlayerRepository | `CountAsync` :85 | generic `CountAsync()` | |
| PlayerRepository | `GetTotalPlayCount` :92 | `SessionsByPlayerSpec` + `CountAsync` on `IRepository<Session>` | |
| PlayerRepository | `GetWinCount` :99 | `WonSessionsByPlayerAndGameSpec` + `CountAsync` | |
| PlayerRepository | `GetTotalWinCount` :108 | `WonPlayerSessionsByPlayerSpec` + `IReadRepository<PlayerSession>.CountAsync` | Composite-key entity — read-only repo (§3.2) |
| PlayerRepository | `GetTopPlayers` :116 | KEEP | GroupBy |
| SessionRepository | `CountAsync` :18 | generic `CountAsync()` | |
| SessionRepository | `CountByPlayer` :23 | `SessionsByPlayerSpec` + `CountAsync` | |
| SessionRepository | `CountByPlayerAndGame` :30 | `SessionsByPlayerAndGameSpec` + `CountAsync` | |
| SessionRepository | `GetByPlayer` :38 | `SessionsByPlayerSpec(won?)` | Tracked today — used by badge evaluators; keep tracked |
| SessionRepository | `GetByPlayerAndGame` :52 | `SessionsByPlayerAndGameSpec` | |
| SessionRepository | `GetTotalPlayTime` :60 / `GetMeanPlayTime` :71 | KEEP | Sum/Average with Any-guard |
| SessionRepository | `GetByPlayerBatchAsync` :82 | KEEP | Badge batch path |
| SessionRepository | `GetByIdAsync` :111 | `SessionByIdWithDetailsSpec` | **Tracked** — `SessionService.UpdateFromCommand` write path |
| SessionRepository | `GetRecentSessions` :119 | `RecentSessionsSpec(count)` | NoTracking, 2 includes + ThenInclude |
| SessionRepository | `GetSessionsByDayOfWeek` :131 | KEEP | GroupBy chart |
| SessionRepository | `DeleteByPlayerIdAsync` :139 | KEEP (fetch via spec internally OK) | Must remain deferred-save (`PlayerService.Delete` atomicity) |
| SessionRepository | `Update` :148 | KEEP | Domain sync, not a query |
| GameRepository | `AddGameCategoriesIfNotExists` :20 / `AddGameMechanicsIfNotExists` :25 / `AddPeopleIfNotExists` :30 | KEEP | `AddRangeIfNotExists` write helpers |
| GameRepository | `GetGameByBggId` :35 | `GameByBggIdSpec` | Tracked; unique index |
| GameRepository | `GetGamesOverviewList` :41 | `GamesOverviewSpec` | NoTracking; drop redundant `AsSplitQuery` or keep in spec |
| GameRepository | `GetByIdAsync` :52 | `GameByIdWithDetailsSpec` | **Tracked** — see §4.1 |
| GameRepository | `GetExpansions` :63 | `ExpansionsByIdsSpec` (over `Expansion`) | Tracked — expansions get attached to sessions |
| GameRepository | `GetTotalExpansionCount` :70 | `IRepository<Expansion>.CountAsync()` | |
| GameRepository | `CountAsync` :75 | generic `CountAsync()` | |
| GameRepository | `DeleteExpansion` :80 | KEEP | Multi-step |
| GameRepository | `GetRecentlyAddedGames` :98 | `RecentlyAddedGamesSpec(count)` | §4.2 |
| GameRepository | `GetGamesWithNoRecentSessions` :108 / `CountGamesWithNoRecentSessions` :117 | `GamesWithNoRecentSessionsSpec(cutoff)` (list + count) | **Rewrite `_context.Sessions.Any` → `g.Sessions.Any`** |
| GameRepository | `GetShameGames` :124 | `ShameGamesSpec(cutoff)` (projection) | Same rewrite; §4.2 |
| GameRepository | `GetByIdsAsync` :146 | `GamesByIdsSpec(ids)` | |
| GameSessionRepository | `GetSessions(gameId,skip,take?)` :20 | `SessionsByGamePagedSpec` | Conditional `Take` → only call `.Take` when `take.HasValue` inside spec ctor |
| GameSessionRepository | `GetSessions(gameId,dayCount)` :39 | `SessionsByGameSinceSpec(cutoff)` | Compute cutoff in caller (§8.6) |
| GameSessionRepository | `GetSessionsByGameId` :49 | `SessionsByGameSpec(count?)` | |
| GameSessionRepository | `GetSessionsByPlayerId` :66 | `SessionsByPlayerRecentFirstSpec(count?)` | |
| GameSessionRepository | `GetPlayCount` :83 | `CountAsync(SessionsByGameSpec)` | |
| GameSessionRepository | `GetTotalPlayedTime` :91 | KEEP | Sum |
| GameSessionRepository | `GetLastPlayedDateTime` :101 | `LastPlayedDateSpec` (projection) | |
| GameSessionRepository | `GetShortestPlay` :111 / `GetLongestPlay` :122 | `ShortestPlayIdSpec` / `LongestPlayIdSpec` (projection to `int?`) | Perf win: stops materializing full entities |
| GameStatisticsRepository | all (§2.5) | KEEP (optional spec-assist) | |
| CompareRepository | all (§2.9) | KEEP (optional `SessionsWithBothPlayersSpec` de-dup) | |
| ConfigRepository | all | UNTOUCHED | `ExecuteUpdateAsync` + internal saves |
| DashboardRepository | — | DELETE | Dead code |

---

## 7. Testing strategy

### 7.1 New: spec unit tests (the big win)

Location: `BoardGameTracker.Tests/Specifications/{Aggregate}/{SpecName}Tests.cs`. Uses only `Ardalis.Specification` (in-memory evaluator) — no DbContext, no mocks:

```csharp
public class ActiveLoansSpecTests
{
    [Fact]
    public void Evaluate_ShouldReturnOnlyLoansWithoutReturnDate()
    {
        // Arrange
        var loans = new List<Loan> { /* one returned, one active */ };
        var spec = new ActiveLoansSpec();

        // Act
        var result = spec.Evaluate(loans).ToList();

        // Assert
        result.Should().ContainSingle().Which.ReturnedDate.Should().BeNull();
    }
}
```

What to assert per spec: filtering (in/out cases), ordering (element order), paging (`Skip`/`Take` boundaries), projection output (for `Specification<T,TResult>`, `spec.Evaluate(items)` applies the selector — assert model fields), and single-entity checks via `spec.IsSatisfiedBy(entity)`. **Limitations:** the in-memory evaluator ignores `Include` and `AsNoTracking`; assert those declaratively instead when they are load-bearing: `spec.IncludeExpressions.Should().HaveCount(5);` / `spec.AsNoTracking.Should().BeTrue();` — this is exactly how to pin the tracked-vs-untracked contract of `PlayerByIdForUpdateSpec` vs `PlayerByIdWithBadgesSpec` (regression guard for bug C2). Note: constructing entities may require using their public ctors/`Update*` methods (DDD-lite private setters) — same as existing evaluator tests (`Tests/Evaluators/*`).

### 7.2 Changed: service tests (Moq)

The existing pattern (mock per-aggregate repo interface + `VerifyNoOtherCalls`, e.g. `Tests/Services/PlayerServiceTests.cs:23-60`) evolves per stage:

- **Stage A (repo interface unchanged):** service tests unchanged. Zero churn — this is why Stage A is the default rollout mode.
- **Stage B (service takes `IRepository<T>`):** setups match on spec type:

```csharp
_gameRepositoryMock
    .Setup(x => x.SingleOrDefaultAsync(It.IsAny<GameByIdWithDetailsSpec>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(game);
// ...
_gameRepositoryMock.Verify(
    x => x.SingleOrDefaultAsync(
        It.Is<GameByIdWithDetailsSpec>(s => s.IsSatisfiedBy(game)),
        It.IsAny<CancellationToken>()),
    Times.Once);
```

  Matching on the concrete spec type (not `It.IsAny<ISpecification<Game>>()`) keeps the tests meaningful; `IsSatisfiedBy` additionally verifies the id parameter reached the spec. `VerifyNoOtherCalls` keeps working unchanged. Note the generic method pitfall: `SingleOrDefaultAsync` takes `ISingleResultSpecification<T>` — set up against the parameter type Moq sees.
- **New repository-level tests:** `EfRepositoryTests` (Phase 0) with EF InMemory (`BoardGameTracker.Tests.csproj:18`) proving: `CreateAsync` doesn't save, `DeleteAsync` returns false for missing ids, spec evaluation applies Where+OrderBy against a real queryable.
- **New integration regression test (Phase 3):** player update round trip on EF InMemory to lock in the C2 fix.
- Per existing convention: test files named `{Class}Tests.cs` in a matching directory, Arrange/Act/Assert with FluentAssertions + `Moq.Verify`, and a `VerifyNoOtherCalls()` helper per class.

### 7.3 Regression gates

- Full `dotnet test` after every PR. Known pre-existing flaky test (documented): `LogLevelExtensionsTests.GetEnvironmentLogLevel_ShouldReturnWarning_WhenEnvironmentVariableIsUnknownValue` — a failure there is not migration-related.
- After Phase 4c specifically: the whole `Tests/Evaluators/*` + `BadgeServiceTests` + `SessionServiceTests` suites (badge behavior depends on session query shapes and save ordering).
- Manual smoke per phase: dashboard page (exercises `DashboardService.GetStatistics`, `Dashboard/DashboardService.cs:33-69`, which touches 12 repo methods across 4 repos), game detail page (stats + charts), session create/edit (badges), shelf of shame, compare page, game nights.

---

## 8. Risks and gotchas specific to this codebase

### 8.1 Save-order / auto-save (highest risk)

Ardalis `RepositoryBase<T>` auto-saves; this app's badge awarding (`SessionService.cs:35-37` + `BadgeService.AwardBadgesAsync` + `BadgeRepository.AwardBatchToPlayer`) and batch import (`BggImportService.cs:146`) require deferred saves through `IUnitOfWork`. **Mitigation:** custom `EfRepository<T>` (§3.2) that never saves; do not register or use `RepositoryBase<T>`/`IRepositoryBase<T>` anywhere. Any accidental auto-save would change badge award timing (documented behavior at `BACKEND_REVIEW.md:20-25`) and break batch-import atomicity.

### 8.2 AsNoTracking read vs tracked write (bug C2 territory)

Current overrides are inconsistent by design accident: `GameRepository.GetByIdAsync` (`:52`) is **tracked** (correct — `GameService.UpdateGame` `GameService.cs:106-134` mutates it), `SessionRepository.GetByIdAsync` (`:111`) is **tracked** (correct — update path), but `PlayerRepository.GetByIdAsync` (`:18`) is **`AsNoTracking`** and its write path silently persists nothing (confirmed C2, `BACKEND_REVIEW.md:27-34`). Rules for the implementer:

1. Every spec must state its tracking decision in a doc comment ("tracked because X mutates the result").
2. A naive "add `AsNoTracking()` to every read spec" sweep **will break** `GameService.UpdateGame`, `UpdateGameExpansions`, `SessionService.UpdateFromCommand`, `GameNightService` RSVP flows, and `LoanService.ReturnLoan`/`Update` (`LoanService.cs:57-86` mutates the entity from plain `GetByIdAsync` and saves). Copy the tracking behavior from the tables in §2/§6 verbatim, except the deliberate Player fix.
3. The Player fix (Phase 3) is the only intentional tracking change; it gets its own test and PR callout.

### 8.3 Split query

`UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` is the **global default** (`ServiceCollectionExtensions.cs:135`). Multi-include specs (`GameByIdWithDetailsSpec` — 5 includes, `GameNightByIdWithDetailsSpec` — 4 + ThenInclude) therefore split automatically; do not remove that provider option, and do not assume `AsSplitQuery()` must be added to each spec. If a spec ever needs single-query semantics (e.g. for consistency under concurrent writes), use `Query.AsSingleQuery()` explicitly — no current call site needs it.

### 8.4 Model configuration stays in the DbContext

All owned-type/precision config (`HasPrecision(18,2)` for `BuyingPrice`/`SoldPrice`/`Rating`/`Weight`, `MainDbContext.cs:72-123`), key conventions (`BuildIds`), enum-to-string conversions (`:240-246`, `:152-154`), relationship config, and seed data (`:300-361`) remain in `MainDbContext.OnModelCreating`. There are no `IEntityTypeConfiguration<T>` classes today; the migration must not introduce any or move anything — specs are pure query objects and interact with owned types transparently (e.g. `g.BuyingPrice != null ? g.BuyingPrice.Amount : null` in `ShameGamesSpec` translates exactly as it does today). No EF migration (`Datastore/Migrations/Postgres/*`) is generated by this work — if `dotnet ef migrations add` produces a non-empty diff at any point, something moved that shouldn't have.

### 8.5 Cross-DbSet subqueries must be rewritten to navigations

Specs are built over a single root `IQueryable<T>`; they cannot reference `_context.OtherSet`. Affected: `GameRepository.cs:112`, `:120`, `:128-140` (`_context.Sessions` → `g.Sessions`). The `Game.Sessions` navigation exists (`MainDbContext.cs:168-171`) and produces equivalent `NOT EXISTS`/correlated-subquery SQL. Verify with the SQL logging already enabled in development (`EnableSensitiveDataLogging`, `ServiceCollectionExtensions.cs:134`).

### 8.6 No services inside expression trees

`GameSessionRepository.cs:44` and `GameNightRepository.cs:51` call `_dateTimeProvider.UtcNow` *inside* the LINQ expression. Specs take the computed `DateTime` as a ctor parameter instead. Side effect to note: today the provider call is evaluated once per query anyway (EF client-evaluates the member access when building SQL), so passing a precomputed value is behavior-preserving — and it makes the specs deterministic in tests.

### 8.7 Miscellaneous

- **`PlayerSession` has a composite key** (`MainDbContext.cs:215-216`) and does not fit `IRepository<T> where T : HasId` — use `IReadRepository<PlayerSession>` (§3.2) for `GetTotalWinCount`-style queries.
- **Conditional query building** (`take.HasValue` at `GameSessionRepository.cs:31-34`, `won.HasValue` at `SessionRepository.cs:44-47`): do the `if` inside the spec constructor (`if (take.HasValue) { Query.Take(take.Value); }`) — per project style, braces even on one-liners.
- **`DbSetExtensions.AddRangeIfNotExists`** (`Common/Extensions/DbSetExtensions.cs:8`) is a write helper, not a query — untouched.
- **`Specification` vs `ISingleResultSpecification`:** use `SingleResultSpecification<T>` for by-id/by-unique-key specs so the repository exposes `SingleOrDefaultAsync` semantics matching today's `SingleOrDefaultAsync` calls (`GameRepository.cs:38`, `:60`; `GameNightRepository.cs:23`); `SessionRepository.GetByIdAsync` uses `FirstOrDefaultAsync` today (`:116`) — keep `FirstOrDefault` semantics there to avoid new exception paths on (impossible but) duplicate ids.
- **Nullable annotations:** Core has `<Nullable>enable</Nullable>` — spec ctor params and `TResult` projections must be annotated accordingly (e.g. `Specification<Session, DateTime?>`).
- **Do not touch** `boardgametracker.client` — this migration is backend-only; the current branch (`feature/170-fixes`) has extensive uncommitted frontend changes. Do the migration on a fresh branch off `master`.

---

## 9. Definition of done

Phase 0
- [ ] `Ardalis.Specification` 9.3.1 + `Ardalis.Specification.EntityFrameworkCore` 9.3.1 in Core; `Ardalis.Specification` 9.3.1 in Tests; solution restores and builds.
- [ ] `IReadRepository<T>`, `IRepository<T>`, `EfRepository<T>`, `EfReadRepository<T>` exist in `BoardGameTracker.Core/Datastore` with **no SaveChanges anywhere in them**.
- [ ] Open-generic DI registrations added; `EfRepositoryTests` proves add-without-save.

Per migrated aggregate (repeat for Loans, Locations, Languages, Badges, GameNights, Players, Games, GameSessions, Sessions)
- [ ] Every SPEC/SPEC-P/SPEC+AGG method from §6 has a spec class under `{Aggregate}/Specifications`. Specs carry NO comments — the class name + builder calls are self-documenting; the tracking decision is expressed by the presence/absence of `AsNoTracking()`, not a comment.
- [ ] Every spec has an in-memory unit test covering filter, order, paging, and (if projection) output mapping; include/tracking asserted via `IncludeExpressions`/`AsNoTracking` where load-bearing.
- [ ] Repository inherits `EfRepository<T>`; no `CrudHelper` reference remains for this aggregate.
- [ ] Stage B aggregates (Loans, Locations, Languages): per-aggregate repo interface + class deleted, DI line removed, service mocks `IRepository<T>` with spec-typed setups, `VerifyNoOtherCalls` intact.
- [ ] KEEP methods unchanged in behavior (diff reviewed line-by-line against §6 notes).
- [ ] Full `dotnet test` green (modulo the documented flaky `LogLevelExtensionsTests` case).
- [ ] Generated SQL spot-checked in dev logs for: game detail fetch, shame games, dashboard, recent sessions.

Cross-cutting
- [ ] Badge flow order preserved: `CreateAsync → AwardBadgesAsync → SaveChangesAsync` still the sequence in `SessionService.Create`/`Update`; badge evaluator + session service test suites green after Phase 4c.
- [ ] Player update round-trip integration test exists and passes (C2 fixed); PR explicitly flags the behavior change.
- [ ] `IUnitOfWork`/`UnitOfWork` unchanged; a solution-wide search for `SaveChanges` finds it only in `UnitOfWork`, `ConfigRepository`, Auth services, and seeders — never in `EfRepository` or specs.
- [ ] `MainDbContext.OnModelCreating` byte-identical; `dotnet ef migrations add VerifyNoModelDrift` produces an empty migration (then delete it).
- [ ] `CrudHelper.cs`, `ICrudHelper.cs`, `DashboardRepository.cs`, `IDashboardRepository.cs` deleted; no dangling DI registrations.
- [ ] `GameStatisticsRepository`, `CompareRepository`, `ConfigRepository` public interfaces byte-identical (hybrid holdouts).
- [ ] No `Ardalis.Specification` reference added to `BoardGameTracker.Common`.
- [ ] Docs updated: architecture note on spec/repo/UoW responsibilities; `BACKEND_REVIEW.md` items C2 and the LoanRepository tracking note marked resolved.
