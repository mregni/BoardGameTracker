# Backend Core/Common Review — BoardGameTracker

Scope: `BoardGameTracker.Core` + `BoardGameTracker.Common` (read-only). Every finding below was verified by reading the cited code; usage claims were verified with repo-wide greps. Baseline checks: `dotnet ef migrations has-pending-model-changes` reports **no pending model changes**; split query is enabled globally (`ServiceCollectionExtensions.cs:182`); all DB-backed tests use the EF InMemory provider, so nothing below that depends on Postgres semantics is covered by tests.

Effort key: S < 1h, M < 1 day, L > 1 day.

---

## Ranked findings

### C-01 — Any settings save wipes the stored BGG API key
- **Severity:** High · **Category:** Correctness / data loss · **Confidence:** High
- **Where:** `BoardGameTracker.Core/Settings/SettingsService.cs:50` (`BggApiKey = string.Empty` on GET), `:75-76` (unconditional `SetConfigValueAsync(BggConfig.ApiKey, model.BggApiKey ?? string.Empty)`), contrast with the change-detection key at `:88-96` which distinguishes `null` (clear) / blank (keep) / value (set).
- **Problem:** GET never returns the key, the settings form initialises `bggApiKey` from GET (`boardgametracker.client/src/routes/settings/index.tsx:77`) and submits it unchanged, so every PUT of unrelated settings (currency, date format…) writes `""` over the DB-stored BGG key. `SettingsServiceTests.cs:252` pins the unconditional write, so the test suite enforces the bug. Only env-provided keys survive.
- **Fix:** Apply the same tri-state rule as the change-detection key: `null` → clear, `IsNullOrWhiteSpace` → leave untouched, otherwise `Trim()` and store. Update the theory data in `SettingsServiceTests`.
- **Effort:** S

### C-02 — BGG import inserts duplicate category/mechanic/person rows on every import and never links them to the game
- **Severity:** High · **Category:** Data integrity / half-finished feature · **Confidence:** High
- **Where:** `BoardGameTracker.Common/Extensions/DbSetExtensions.cs:12` (`AnyAsync(x => x.Id == item.Id)`), `BoardGameTracker.Core/Games/Factories/GameFactory.cs:44-61` (builds `new GameCategory(c.Value)` etc. with `Id == 0`), `:63-80` (game built without touching `Categories`/`Mechanics`/`People`), `BoardGameTracker.Common/Entities/Game.cs:13-15` (collections exist, no add methods, no call sites anywhere).
- **Problem:** The "if not exists" check compares on `Id`, which is always `0` for freshly constructed entities, so it is always false and every import inserts a fresh `GameCategories`/`GameMechanics`/`People` row per link (roughly 10-20 rows per game, unbounded growth). None of them are ever attached to the game, so `GameDto.Categories/Mechanics/People` (`GameDtoExtensions.cs:35-37`) are always empty for imported games. Also an N+1: one `AnyAsync` per link.
- **Fix:** Look up by natural key in one query (`Where(c => names.Contains(c.Name))`), reuse the tracked instances, create only the missing ones, then `game.AddCategory(...)` etc. (add the entity methods). Add unique indexes on `GameCategory.Name`, `GameMechanic.Name`, `(Person.Name, Person.Type)` and a one-off cleanup migration for the duplicates.
- **Effort:** M

### C-03 — Score rankings are wrong on Postgres: `ORDER BY ... DESC` puts NULL scores first
- **Severity:** High · **Category:** Correctness (Postgres semantics) · **Confidence:** High
- **Where:** `BoardGameTracker.Core/Games/GameStatisticsRepository.cs:175-181` (`GetHighestScoringPlayer`), `:183-190` (`GetHighestLosingPlayer`), `:132-142` (`GetHighScorePlay`, dead); consumer `GameChartService.cs:118-137`; `Common/Models/Charts/ScoreRank.cs:56` (`Score = playerSession.Score ?? 0`); `Api/Controllers/GameController.cs:185` calls the chart for every game, scoring or not.
- **Problem:** Postgres sorts NULLs first for `DESC`. `OrderByDescending(x => x.Score).FirstOrDefaultAsync()` therefore returns a player session with a null score whenever any participant has no score, and `ScoreRank` renders it as "top score: 0". For non-scoring games (all scores null) the chart is populated with arbitrary players at 0. `GetLowestScoringPlayer`/`GetLowestWinning` happen to be right only because `ASC` puts NULLs last. The InMemory provider used by tests sorts nulls first for ascending, so no test can catch this.
- **Fix:** Add `.Where(x => x.Score != null)` to all four score-ordered queries (and to `GetHighScorePlay`/`GetLowestScorePlay` or delete them, see C-16); return `null` from `GetScoringRankedChart` when `GameHasScoringSpec` is false, mirroring `GetPlayerScoringChart` (`GameChartService.cs:75-79`).
- **Effort:** S

### C-04 — Deleting a Location or a hosting Player cascade-deletes game nights
- **Severity:** High · **Category:** Data integrity / cascade surprise · **Confidence:** High
- **Where:** `BoardGameTracker.Core/Datastore/MainDbContext.cs:132-147` (`BuildGameNights` configures neither `Host` nor `Location`), model snapshot `MainDbContextModelSnapshot.cs:1591-1601` (`HostId` and `LocationId` both `OnDelete(DeleteBehavior.Cascade)` by convention). Compare `Session.Location` which is explicitly `SetNull` (`MainDbContext.cs:221-225`).
- **Problem:** `LocationService.Delete` (`LocationService.cs:47-53`) and `PlayerService.Delete` silently remove every game night held at that location / hosted by that player, plus all RSVPs. Nothing in the services checks or warns.
- **Fix:** Configure `HasOne(x => x.Host)...OnDelete(DeleteBehavior.Restrict)` and `HasOne(x => x.Location)...OnDelete(DeleteBehavior.Restrict)` (or make `LocationId` nullable + `SetNull`), add a migration, and translate the resulting `DbUpdateException` into a `DomainException` in the two delete services.
- **Effort:** S

### C-05 — Files are deleted from disk before the DB change is committed
- **Severity:** Medium · **Category:** Data integrity / missing transaction · **Confidence:** High
- **Where:** `GameService.cs:76-79` (`DeleteImage`, `DeleteManualFilesForGame`, then `DeleteAsync` + `SaveChangesAsync`), `PlayerService.cs:107-109`, `PlayerService.cs:72-76` (old avatar deleted before save in `Update`), `ManualService.cs:139-142`.
- **Problem:** If `SaveChangesAsync` fails (FK/constraint, connection drop, the C-04 cascade being changed to Restrict…), the row survives but its cover image / manual PDF / rendered page figures are gone. `IUnitOfWork.BeginTransactionAsync` exists but is only used by `ResetService.cs:38,52`. `ManualService.UploadManuals` (`:76-98`) already does this correctly with compensation on failure.
- **Fix:** Reorder to save first, then delete files (log and swallow file errors), or collect paths and delete them in a post-commit step (a small `ISaveChangesInterceptor.SavedChangesAsync` or a "pending file deletions" list on the unit of work).
- **Effort:** S

### C-06 — Session update runs the whole sync twice, with a second copy of the logic living in the repository
- **Severity:** Medium · **Category:** Pattern violation / duplicated logic · **Confidence:** High
- **Where:** `SessionService.cs:108-128` (`UpdateFromCommand` loads via `GetByIdAsync`, syncs expansions/players/location, then calls `Update`), `SessionService.cs:54-63` → `SessionRepository.cs:122-140` (override re-queries the same tracked session with three Includes and re-runs `UpdateLocationAsync`/`SyncPlayerSessions`/`SyncExpansionsAsync` against itself), `SessionRepository.cs:158-193` (second implementation of player sync; `:185-186` only ever sets `FirstPlay = true`, never clears it, unlike the service copy at `SessionService.cs:172`).
- **Problem:** Every update issues an extra 3-include query and executes two divergent sync implementations; the repository one is domain logic in the data layer and is effectively dead because the service always passes the already-synced tracked instance (so all its diffs are empty). Anyone who later calls `_sessionRepository.Update` with a detached entity gets the asymmetric FirstPlay behaviour.
- **Fix:** Delete the `SessionRepository.Update` override and its three helpers; have `SessionService.Update` just award badges and save. Keep one sync implementation (the service one).
- **Effort:** S

### C-07 — `GameRepository.GetByIdAsync` loads the full six-collection graph for every by-id read
- **Severity:** Medium · **Category:** EF over-fetching · **Confidence:** High
- **Where:** `GameRepository.cs:60-63` (override → `GameByIdWithDetailsSpec`, `Specifications/GameByIdWithDetailsSpec.cs:10-17`: Accessories, Categories, Expansions, Mechanics, People, Loans, tracked). Callers that need one or two scalars: `GameService.Delete :71`, `UpdateGame :123`, `SearchExpansionsForGame :159` (needs `BggId`), `UpdateGameExpansions :185` (needs `Expansions`), `LoanService.LoanGameToPlayer :42` (needs `Loans`), `CloseWinBadgeEvaluator.cs:41` and `CloseLossBadgeEvaluator.cs:39` (need `HasScoring`, executed once per badge candidate per player on every session create/update).
- **Problem:** With split query enabled that is 7 round-trips and a fully tracked graph per call. The badge evaluators alone can run it several times per session save; `GameHasScoringSpec` (`Specifications/GameHasScoringSpec.cs`) already exists for exactly that need.
- **Fix:** Remove the override (base `FindAsync` semantics) and give each caller a purpose-built spec: `GameWithExpansionsSpec`, `GameWithLoansSpec`, `GameHasScoringSpec`, a projection for `BggId`.
- **Effort:** M

### C-08 — Session create/update existence checks load heavy graphs and are N+1
- **Severity:** Medium · **Category:** EF N+1 / over-fetching · **Confidence:** High
- **Where:** `SessionService.cs:69-73` (`_gameService.GetGameById` → `GameByIdWithDetailsForReadSpec`, six Includes, only null-checked), `SessionService.cs:207-217` (`EnsurePlayersExistAsync` loops `_playerService.Get(id)` → `PlayerRepository.cs:29-32` → `PlayerByIdWithBadgesSpec` with `Include(Badges)` per player).
- **Problem:** A 5-player session issues 1 + 5 multi-query reads purely to check existence; the results are discarded.
- **Fix:** `_gameRepository.AnyAsync(new GameByIdSpec(id))` and one `CountAsync(new PlayersByIdsSpec(ids)) == ids.Distinct().Count()`; throw `EntityNotFoundException` with the missing ids.
- **Effort:** S

### C-09 — Badge awarding tracks every session a player ever played and reloads all badge holders per award
- **Severity:** Medium · **Category:** EF over-fetching / N+1 · **Confidence:** High
- **Where:** `SessionRepository.cs:66-93` (`GetByPlayerBatchAsync`: tracked, `Include(PlayerSessions)`, `Include(Game)`, `Include(Expansions)`), `BadgeService.cs:37-38` (called on every session create/update), `BadgeRepository.cs:48-66` (`AwardBatchToPlayer`: `Include(x => x.Players)` loads every player holding the badge, plus a second query for the player, per awarded badge).
- **Problem:** No evaluator reads `Game` or `Expansions` (they use `GameId`, `Start`, `End`, `PlayerSessions`), yet both are included and every session is attached to the change tracker, so `SaveChangesAsync` runs `DetectChanges` over the player's full history. `AwardBatchToPlayer` is called inside `ProcessBadgeGroup` per badge and scales with the number of badge holders.
- **Fix:** Drop the two unused Includes and add `AsNoTracking()` (evaluators only read); in `AwardBatchToPlayer`, attach stubs or add the join row directly (`Context.Set<Dictionary<string, object>>("BadgePlayer")`) instead of loading `Badge.Players`.
- **Effort:** S

### C-10 — Settings update persists 12 keys before validating, each as an autonomous write
- **Severity:** Medium · **Category:** Data integrity / pattern violation · **Confidence:** High
- **Where:** `SettingsService.cs:61-76` (12 × `SetConfigValueAsync`) then `:78-84` (`ChangeDetectionBaseUrl` validation throws `ValidationException`); `ConfigRepository.cs:35-50` (`ExecuteUpdateAsync` + `_context.SaveChangesAsync()` directly, bypassing `IUnitOfWork`; also `:78-84`).
- **Problem:** A bad base URL yields HTTP 400 but currency/date format/BGG key (see C-01) have already been committed; the request is neither atomic nor idempotent. `ConfigRepository` is the only repository that saves, contrary to the stated convention, and its `SaveChangesAsync` would also flush any unrelated tracked changes in the scope. The update-or-insert also races (two first-time writers both insert; no unique index on `Config.Key`, already known).
- **Fix:** Validate everything first; wrap the writes in `IUnitOfWork.BeginTransactionAsync` (or accumulate `Config` entities and save once); move the save out of `ConfigRepository`.
- **Effort:** S

### C-11 — Manual indexing embeds an entire rulebook in one request and deletes old chunks outside any transaction
- **Severity:** Medium · **Category:** Half-finished feature / robustness · **Confidence:** High
- **Where:** `Rag/ManualIndexingService.cs:97-100` (`embedder.GenerateAsync(chunks.Select(c => c.Content).ToList())`), `:102` (`ExecuteDeleteAsync` fires immediately), `:125-127` (new chunks saved later), `:131-143` (any exception, including `OperationCanceledException` at shutdown, is recorded as `Failed` with `ex.Message`).
- **Problem:** A 200-page PDF at 1000-char chunks is several hundred inputs in a single Ollama `/api/embed` call, which is where Ollama timeouts/OOM appear; there is no batching or size cap. If embedding or `SaveChangesAsync` fails after the delete, the manual has zero chunks (RAG silently answers "not found") until the next successful run. Shutdown mid-index leaves a misleading "A task was canceled" failure message.
- **Fix:** Batch embeddings (e.g. 32-64 chunks per call) and stream them into the entity list; run delete + insert + `MarkIndexed` inside one transaction; rethrow `OperationCanceledException` so the manual stays `Indexing` and is re-queued by `ManualsToIndexSpec`.
- **Effort:** M

### C-12 — Vector search will return fewer than TopK hits once several games have manuals; RAG runs ~21 config queries per question
- **Severity:** Medium · **Category:** Half-finished feature / performance · **Confidence:** Medium
- **Where:** `Rag/Specifications/NearestManualChunksSpec.cs:12-25` (`Where(GameId == …)` + `OrderBy(CosineDistance)` + `Take(k)` on an HNSW index), `Rag/RagSettingsProvider.cs:16-36` (7 sequential `GetConfigValueAsync` per call) called from `RagService.cs:51` and again inside `AiClientFactory.cs:33,47` (`CreateEmbeddingGeneratorAsync`, `CreateChatClientAsync`), `RagService.cs:86-96` (one `GetByIdAsync` per distinct manual for titles).
- **Problem:** pgvector's HNSW scan returns `hnsw.ef_search` (default 40) candidates before the `GameId` filter is applied; with many games the filter discards most candidates and the query returns fewer than `k` (or zero) chunks even though matches exist. Separately, each question costs 21 config lookups plus N manual lookups.
- **Fix:** Enable iterative scans (`SET hnsw.iterative_scan = relaxed_order`, pgvector ≥ 0.8) via a connection/command interceptor, or raise `ef_search` for that query, or partition per game (partial indexes are not practical here; a per-game `GameId`-prefixed IVFFlat is an alternative). Load AI settings once with `GetConfigsByPrefixAsync("ai_")` and pass the `RagSettings` into the factory; include `Manual` in the chunk spec projection.
- **Effort:** M

### C-13 — BGG list import: duplicate ids in one request fail the entire batch after all image downloads; per-item DB round-trips
- **Severity:** Medium · **Category:** Correctness / N+1 · **Confidence:** High
- **Where:** `Games/BggImportService.cs:128-138` (one `GetGameByBggId` per requested game), `:145-173` (per-game factory call with image download, all `CreateAsync` then one `SaveChangesAsync` at `:175`), `Game.BggId` unique index (`MainDbContext.cs:152-154`).
- **Problem:** Two entries with the same `BggId` in the request both pass the DB check, both get created, and the single `SaveChangesAsync` throws a unique violation, discarding every game in the batch after their images were downloaded and after N `AnyAsync` calls per link (C-02). The per-game `catch` cannot help because the failure happens at save time.
- **Fix:** `DistinctBy(x => x.BggId)` on input, one `Where(bggIds.Contains(BggId))` query for existing ids, and save per game (or per chunk) so one bad row does not discard the rest.
- **Effort:** S

### C-14 — A session can reference expansions of a different game
- **Severity:** Medium · **Category:** Data integrity / entity invariant · **Confidence:** High
- **Where:** `SessionService.cs:82-87` and `:145-149` (`_gameService.GetGameExpansions(ids)`), `GameRepository.cs:65-70` → `Specifications/ExpansionsByIdsSpec.cs:10` (filters on `Id` only).
- **Problem:** Nothing checks `expansion.GameId == session.GameId`, so a client (or a stale UI after `UpdateGameExpansions` removed an expansion) attaches foreign expansions; statistics per game then include expansions the game does not own.
- **Fix:** Add `gameId` to the spec (`Where(x => x.GameId == gameId && ids.Contains(x.Id))`) and throw `EntityNotFoundException` when the count differs.
- **Effort:** S

### C-15 — Loan creation: unvalidated player id, no-op guards, and overdue loans count as inactive
- **Severity:** Medium · **Category:** Validation / entity invariant · **Confidence:** High
- **Where:** `Loans/LoanService.cs:39-56` (no player existence check → FK violation → 500), `Common/Entities/Loan.cs:18-19` (`Guard.Against.Null` on `int` is a no-op; should be `NegativeOrZero`), `Loan.cs:42-51` (`IsActiveOn` uses `ReturnedDate ?? DueDate` as the end).
- **Problem (new detail on the known double-loan item):** because `IsActiveOn` treats a passed `DueDate` as the end of the loan, `Game.LoanToPlayer` (`Game.cs:52-62`) happily creates a second loan while the first is overdue and unreturned, and `IsLoaned`/`IsCurrentlyOnLoan` (`Loan.cs:36-40`) disagree with it (they ignore `DueDate`). Three definitions of "active" exist (`ActiveLoansSpec` is the third).
- **Fix:** One definition: active = `ReturnedDate == null` (optionally `LoanDate <= now`); use it in the entity, the spec and the DTO. Validate `PlayerId` via `IReadRepository<Player>.AnyAsync`.
- **Effort:** S

### C-16 — Dead / orphaned code cluster (verified by grep: no callers outside definitions and tests)
- **Severity:** Low · **Category:** Dead code · **Confidence:** High
- **Where:**
  - Repository methods: `IPlayerRepository.GetBestGame` (`PlayerRepository.cs:38-47`, also groups by the whole `Game` entity), `GetWinCount` (`:100-103`); `IGameStatisticsRepository.GetHighScorePlay`/`GetLowestScorePlay` (`GameStatisticsRepository.cs:132-154`); `ISessionRepository.CountByPlayer`, `CountByPlayerAndGame`, `GetByPlayer`, `GetByPlayerAndGame` (`SessionRepository.cs:24-42`); `IBadgeRepository.GetPlayerBadgesAsync` (`BadgeRepository.cs:17-20`); `IGameRepository.GetGamesWithNoRecentSessions` (`GameRepository.cs:105-108`); `EfRepository.Update` (`EfRepository.cs:34-38`, only the Session override is called and that is C-06).
  - `IBadgeProgressionService`/`BadgeProgressionService` (registered at `ServiceCollectionExtensions.cs:130`, never injected) and `BadgeLevelProgressionPolicy.GetNextLevel/GetPreviousLevel/IsMaxLevel/CompareLevels` (`:24-76`).
  - `BadgeEvaluatorConstants.ConsistentWeeksRequired = 4` and `MonthlyGoalLookbackMonths` (`BadgeEvaluatorConstants.cs:16,21`) are unused while `ConsistentScheduleBadgeEvaluator.cs:20` hard-codes `10` weeks and `MonthlyGoalBadgeEvaluator.cs:20` hard-codes `-1`; the constant and the behaviour disagree.
  - Value objects `PlayerName`, `GameScore`, `SessionTimeRange`; models `PlayerComparison`, enum `ResultState`.
  - Entity helpers never called from Core/Api: `Session.GetWinner/GetPlayers/HasFirstTimePlayers/GetHighestScore/GetLowestScore/GetAverageScore` (`Session.cs:117-162`), `Location.GetPlayCount/GetGamesPlayedAtLocation` (`Location.cs:29-36`), `*.GetGameCount()`.
  - `Image` entity + `Session.ExtraImages`/`AddImage` (`Session.cs:35,107-111`): `new Image(` has zero call sites. The mapping also carries a wart: `Image.GamePlayId` (`Image.cs:16`) is a plain column while EF created a shadow FK `PlayId` for `Play` (snapshot `MainDbContextModelSnapshot.cs:914-928`), so the constructor's `gamePlayId` would never link an image to a session.
  - `LoanDto.IsActive` (`LoanDto.cs:11`) is never set by `LoanDtoExtensions.ToDto` (`:8-19`) and never read by the client.
- **Fix:** Delete the lot (and the corresponding tests), or map `IsActive`; decide whether session images are a feature and if so fix the FK naming.
- **Effort:** S-M

### C-17 — Game update leaks replaced cover images; expansion endpoints hide missing games
- **Severity:** Low · **Category:** Orphaned files / inconsistent null handling · **Confidence:** High
- **Where:** `GameService.cs:133` (`UpdateImage(command.Image)` with no `DeleteImage` of the old file; `PlayerService.cs:72-76` does it), `GameService.cs:185-188` (`UpdateGameExpansions` returns `[]` with 200 for a missing game), `GameRepository.cs:88-91` + `GameService.cs:235-241` (`DeleteExpansion` silently no-ops for missing game or expansion, controller returns 204).
- **Fix:** Mirror the player path (delete old file after save when the path changed); throw `EntityNotFoundException` in both expansion paths.
- **Effort:** S

### C-18 — `UpdateGameExpansions` fans out one BGG request per expansion in parallel
- **Severity:** Low · **Category:** HttpClient / rate limiting · **Confidence:** High
- **Where:** `GameService.cs:200-207` (`Task.WhenAll` of `ThingRequest([id])`), while `BggImportService.FetchThingsFromBgg` (`:196-235`) already batches 20 ids per request and maps 429 to `BggRateLimitException`.
- **Fix:** One `ThingRequest(newExpansionsIds, types: ["boardgameexpansion"])`, reuse the 401/429 handling from the import service (extract a shared helper).
- **Effort:** S

### C-19 — Money and percentages use mixed types/scales across the data layer
- **Severity:** Low · **Category:** Duplicated logic / decimal-double mixing · **Confidence:** High
- **Where:** `GameStatisticsRepository.cs:26,113,121` cast `Price.Amount` to `double` (`GetPricePerPlay`, `GetMeanPayedAsync`, `GetTotalPayedAsync`) and `DashboardStatisticsDto.TotalCollectionValue/AvgGamePrice` are `double?`, while `ShameService.cs:57-64`/`ShameStatisticsDto` keep `decimal`. Win percentage is computed four times on two scales (known 0-1 vs 0-100): `PlayerRepository.cs:64-66`, `WinPercentageBadgeEvaluator.cs:25`, `TopPlayerDto.cs:22`, `CompareService.cs:33-34`. `GetMeanPayedAsync` (`:102-114`) also issues a `Count` query that `AverageAsync` on a nullable projection makes unnecessary.
- **Fix:** Keep `decimal` end-to-end for money (`SumAsync(x => (decimal?)x.BuyingPrice!.Amount)`); one `Percentage.Of(wins, total)` helper in Common.
- **Effort:** S

### C-20 — Time zone handling is inconsistent
- **Severity:** Low · **Category:** DateTime/timezone · **Confidence:** Medium
- **Where:** `GameNights/GameNightService.cs:176` formats `StartDate` (UTC) into the RSVP notification email even though `IDateTimeProvider.ConvertToLocalTime` exists (`Common/DateTimeProvider.cs:32-40`) and is injected into the service; `SessionRepository.cs:110` / `GameStatisticsRepository.cs:161` group on `Start.DayOfWeek` in SQL, so the weekday follows the Postgres session time zone rather than the configured `TZ`; `ConsistentScheduleBadgeEvaluator.cs:12,27` compares UTC dates; `ManualService.cs:84`, `ManualIndexingService.cs:126`, `BaseGame.cs:57,160` use `DateTime.UtcNow` directly while every other service goes through `IDateTimeProvider` (tests cannot control them).
- **Fix:** Convert with `IDateTimeProvider` before formatting/day-bucketing (for SQL grouping: `AT TIME ZONE` via `EF.Functions` or group client-side after conversion); inject `IDateTimeProvider` in the two manual services.
- **Effort:** S

### C-21 — Minor correctness nits
- **Severity:** Low · **Category:** Correctness · **Confidence:** High
- `GameFactory.cs:33-36,68`: missing BGG play times become `PlayTime(0, 0)` instead of `null` (player count at `:28-29` correctly maps to `null`), so imported games show "0-0 min".
- `GameChartService.cs:112`: `chartData.TryAdd(session.Start, …)` silently drops a second session with the same `Start` (two same-day quick plays logged with the same time).
- `CompareRepository.cs:177`: `WinnerId = x.PlayerOneWon ? playerOne : playerTwo` reports player two as winner of a session nobody won.
- `PlayerRepository.cs:43,54`: `GroupBy(ps => ps.Session.Game)` groups by the entire `Game` row (every column including `Description` in `GROUP BY`); group by `GameId` and join the title/image after.
- `SessionRepository.cs:44-64`: `AnyAsync` guard is unnecessary for `Sum` and can be replaced for `Average` by projecting `(double?)`.
- **Effort:** S each

### C-22 — Change detection: status of the known items
- **Severity:** Low · **Category:** Verification · **Confidence:** High
- Unbounded fan-out: fixed by the process-wide `SemaphoreSlim(4)` (`ChangeDetectionClient.cs:19-20,196`). Negative cache: fixed (`FailureCacheDuration` 1 min, `:16,185`). Silent failure: fixed via `ChangeDetectionStatus` surfaced on `GamePriceDto`. Parser thousands separators: **partially** fixed — mixed separators are rejected (`ChangeDetectionSnapshotParser.cs:50-53`) but a lone comma is always a decimal separator (`:54-62`), so a snapshot reading `Price: 1,234` parses as `1.234`.
- **Fix:** Treat a single comma followed by exactly three digits and no other separator as ambiguous (return null) or make the format configurable per watch.
- **Effort:** S

### C-23 — Missing indexes for the most common filters
- **Severity:** Low · **Category:** EF/indexes · **Confidence:** High
- **Where:** `Sessions` has indexes on `GameId` and `LocationId` only (snapshot `:1176-1179`); `Start` is used by `GamesWithNoRecentSessionsSpec.cs:12`/`ShameGamesSpec.cs:13` (correlated `Any(s => s.Start >= cutoff)` per game), `LastPlayedDateSpec`, `RecentSessionsSpec`, `SessionsByGameSinceSpec`, `SessionsByGameSpec` ordering. `GameNightRsvps` has no unique `(GameNightId, PlayerId)` although `RsvpByPlayerAndGameNightSpec` uses `SingleOrDefault`. (`Config.Key` unique is already known.)
- **Fix:** `HasIndex(s => new { s.GameId, s.Start })`; `HasIndex(r => new { r.GameNightId, r.PlayerId }).IsUnique()`; one migration.
- **Effort:** S

### C-24 — Config reads disagree on empty/invalid values
- **Severity:** Low · **Category:** Inconsistent null/empty handling · **Confidence:** High
- **Where:** `SettingsService.ResolveValue` (`:172-189`) silently returns `default` for missing/unparseable values, while `ConfigRepository.GetConfigValueAsync<T>` (`:20-33,86-100`) throws `ConfigMissingException` for an empty or unparseable `int`/`bool`/enum (`TypeConverter.cs:15-31`). `ShameService.cs:38,46` therefore returns HTTP 500 if `shelf_of_shame_months` is blank, while the settings page shows `0`.
- **Fix:** One accessor with an explicit default parameter (`GetConfigValueAsync<T>(key, T fallback)`) used by both.
- **Effort:** S

---

## Quick wins (≤ 30 min each)

1. `.Where(x => x.Score != null)` in the four score-ordered queries; gate `GetScoringRankedChart` on `GameHasScoringSpec` (C-03).
2. Tri-state handling for `BggApiKey` in `UpdateSettingsAsync`, mirroring the change-detection key (C-01).
3. `OnDelete(DeleteBehavior.Restrict)` for `GameNight.Host` and `GameNight.Location` + migration (C-04).
4. Move the `ChangeDetectionBaseUrl` validation to the top of `UpdateSettingsAsync` (C-10).
5. Delete `SessionRepository.Update` and its three helpers (C-06).
6. Replace `_gameRepository.GetByIdAsync` in `CloseWin`/`CloseLoss` evaluators with `FirstOrDefaultAsync(new GameHasScoringSpec(id))` (C-07).
7. Drop `Include(Game)`/`Include(Expansions)` and add `AsNoTracking()` in `GetByPlayerBatchAsync` (C-09).
8. `DistinctBy(x => x.BggId)` at the top of `ImportList` (C-13).
9. Add `gameId` to `ExpansionsByIdsSpec` (C-14).
10. `Guard.Against.NegativeOrZero` in the `Loan` constructor; `AnyAsync` player check in `LoanGameToPlayer` (C-15).
11. Delete the dead repository methods, `IBadgeProgressionService`, unused value objects/models/enum, and either use or delete `ConsistentWeeksRequired` (C-16).
12. Composite index `Sessions(GameId, Start)` and unique `GameNightRsvps(GameNightId, PlayerId)` (C-23).
13. Rename `GameDto.isLoaned` → `IsLoaned` (`GameDto.cs:20`) — the only non-PascalCase public member in Common.
14. `BadgeRepository.AwardBatchToPlayer` → `AwardBadgeToPlayer`.
15. `ImportGame.Price` is `double` (`ImportGame.cs:12`) and converted with `ToSafeDecimalPrice`; make it `decimal?` at the boundary and delete the helper.
16. `GameFactory`: map missing play time to `null` (C-21).

## Missing packages / patterns (only where the code shows the pain)

- **Testcontainers.PostgreSql (+ Respawn)** for repository/spec tests. Every DB test uses `UseInMemoryDatabase`, which cannot execute `ExecuteUpdate/ExecuteDelete`, ignores Postgres NULL ordering, `timestamptz` Kind rules, `GroupBy` translation and pgvector. C-03 and the cascade in C-04 are exactly the class of bug that a handful of container-backed tests over `GameStatisticsRepository`, `SessionRepository`, `ConfigRepository` and the specs would have caught.
- **FluentValidation** (or at least DataAnnotations on commands). Validation today is scattered across guard clauses in entities (surfacing as `ArgumentException`), ad-hoc checks in services (`SettingsService.cs:78-84`, `ManualService.ValidateFile`) and nothing at all for `CreateLoanCommand.PlayerId`, `CreateGameNightCommand.HostId/LocationId`, `CreateSessionCommand.Minutes/PlayerSessions` (known), `UIResourceDto.PublicUrl`. A validator per command run by the `[ApiController]` pipeline would give consistent 400s and let C-10/C-15 become declarative.
- **Post-commit file cleanup hook** (an `ISaveChangesInterceptor` implementing `SavedChangesAsync`, or a `PendingFileDeletes` list on `UnitOfWork`). Four services (C-05) repeat the "delete file, then save" ordering; one interceptor fixes all of them and keeps the services free of file-system ordering concerns.
- **Microsoft.Extensions.Http.Resilience** (`AddStandardResilienceHandler`) on the `changedetection`, `ai` and default (image download) named clients. Only the BGG library has retries; the Ollama client can hang on a large embed call (C-11) and the image download at `ImageService.cs:41-42` relies on the 100 s default timeout. Registration lives in `Host/Program.cs:173-176` (other reviewer's scope), but the pain is in Core.
- **Not recommended:** Mapperly/AutoMapper — the hand-written `ToDto` extensions are small, consistent and not a pain point; source-generated logging — the `LogDebug` calls are cheap enough at this scale.

## Overall assessment

1. The specification/UoW architecture is applied consistently and the EF model is in sync with the migrations; the real problems are Postgres semantics that the InMemory tests cannot see (NULL ordering, convention cascades) and two import/settings paths that silently corrupt data (BGG key wipe, duplicate unlinked taxonomy rows).
2. Read paths systematically over-fetch: the `GameRepository.GetByIdAsync` override and the player existence loop turn simple writes into 10+ split queries, and the badge pipeline tracks a player's entire history per save.
3. Multi-step flows (file + DB, 14 config writes, delete-then-insert of chunks) run without transactions even though `IUnitOfWork.BeginTransactionAsync` exists.
4. There is a sizeable dead layer (unused repo methods, progression service, value objects, session images) that inflates the surface reviewers and tests have to cover.
5. The AI/RAG feature is functional but not yet robust at scale (single embed request per manual, HNSW-with-filter recall, 21 config reads per question).
