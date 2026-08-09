# Backend Code Review — Full Codebase (feature/170-fixes)

Read-only review of the whole C# backend (Common / Core / Api / Host), excluding migrations,
generated EF code, and the frontend. Conducted by 7 parallel subsystem reviewers. Findings below
are deduplicated and ranked. Items marked **CONFIRMED** were re-verified against the source by hand.

> Out of scope / already handled: the `feature/170-fixes` ShopUrl/Language feature (reviewed separately),
> known won't-fix sequential DB queries in CountController/DashboardService, the flaky LogLevelExtensions test.

> **Resolved during the Specification-pattern migration** (see `SPEC_PATTERN_MIGRATION_PLAN.md`):
> - **C2** (Player edits never persist — `AsNoTracking` on the update path): FIXED in Phase 3. `PlayerService.Update` now fetches via the tracked `PlayerByIdForUpdateSpec`; two EF-InMemory round-trip tests lock it in.
> - **LOW / `LoanRepository.GetAllAsync` dropped `AsNoTracking`**: FIXED in Phase 1 — `LoansOrderedByDateSpec` restores `AsNoTracking` on the read path.

---

## CRITICAL

### C1 — Badges are evaluated before the session is saved (systemic off-by-one) · **CONFIRMED**
`BoardGameTracker.Core/Sessions/SessionService.cs:35-37`, `BadgeService.AwardBadgesAsync`
`Create()` does `CreateAsync` (EF `AddAsync` only, no save) → `AwardBadgesAsync` → `SaveChangesAsync`.
Badge evaluators query the DB via `GetByPlayerBatchAsync`, and EF queries do **not** return unsaved
`Added` entities — so the triggering session is invisible during evaluation on the create path (but
present on the update path, so create/update behave differently). Consequences:
- **FirstTry** is fully inverted (wins on the first play award nothing; a later win after a loss awards it).
- **WinningStreak** awarded one session late — can be granted on a *losing* session.
- **ConsistentSchedule** effectively unearnable on create (the current Saturday is never counted).
- **Sessions / SessionWin / DifferentGame / SoloSpecialist / WinPercentage / MonthlyGoal / LearningCurve** all award one session late.
**Fix:** save the session before awarding (`CreateAsync → SaveChangesAsync → AwardBadgesAsync → SaveChangesAsync`),
or append the in-memory session to each player's list inside `AwardBadgesAsync`.

### C2 — Player edits never persist, and the image file is deleted anyway · **CONFIRMED (2 agents)**
`BoardGameTracker.Core/Players/PlayerRepository.cs:21` + `PlayerService.cs:64-82`
`PlayerRepository.GetByIdAsync` overrides the base with `.AsNoTracking()`. `PlayerService.Update`
mutates that detached entity and calls `SaveChangesAsync()` without re-attaching → **zero rows written**.
Worse, if the image changed, the old file is deleted from disk first (line 75), so the edit is lost
*and* the image is orphaned. Mocked unit tests can't catch this.
**Fix:** use a tracked fetch on the write path (drop `AsNoTracking` or add `GetForUpdateAsync`), or call
`_playerRepository.Update(dbPlayer)` before saving. Add a round-trip integration test.

### C3 — BGG import throws for any game missing player-count data · **CONFIRMED**
`BoardGameTracker.Core/Games/Factories/GameFactory.cs:28-29,67` → `PlayerCountRange` ctor
`ThingResponse.Item.MinPlayers/MaxPlayers` are non-nullable `int`; when BGG omits them (expansions,
incomplete entries) they deserialize to `0`. The factory passes `0,0` to `UpdatePlayerCount`, which
builds `new PlayerCountRange(0,0)` → `Guard.Against.NegativeOrZero(0)` throws. In `ImportList` one bad
game rolls back the whole batch (single `SaveChangesAsync`).
**Fix:** treat `0` as unknown in the factory (`int? min = item.MinPlayers > 0 ? item.MinPlayers : null;`)
and only build the range when both are meaningful.

---

## HIGH — Security

### H1 — OIDC admin-group match is a substring `Contains` → privilege escalation
`BoardGameTracker.Core/Auth/OidcService.cs:292,346`
Groups claim is joined to a comma-string then `Contains(AdminGroupValue, OrdinalIgnoreCase)`. With
`AdminGroupValue = "admin"`, a user in `badminton-club` or `administrative-assistants` is provisioned Admin.
**Fix:** split the claim into discrete values and compare each with `string.Equals(..., OrdinalIgnoreCase)`.

### H2 — Default `admin/admin`, relaxed password policy, no lockout
`BoardGameTracker.Core/Auth/DbSeeder.cs:42-43`, `Host/Program.cs:79-84`, `AuthService.cs:50`
Fresh installs seed `admin`/`admin`; policy relaxed to length 4 / no character classes; login uses
`lockoutOnFailure: false`. An exposed instance is one guess from full admin.
**Fix:** generate a random initial password (log once) or force change on first login; enable lockout.

### H3 — `AllowAnyOrigin` CORS + auth-disabled admin principal → drive-by cross-origin admin
`BoardGameTracker.Host/Program.cs:146-154` + `AuthDisabledMiddleware.cs:16-30`
CORS is `AllowAnyOrigin/AnyMethod/AnyHeader`. With `AUTH_ENABLED=false`, every request gets an Admin
principal. Any web page the user visits can XHR to `http://<lan-ip>:<port>/api/...`, perform admin
mutations, and read responses (ACAO `*`).
**Fix:** when auth is disabled, restrict CORS to same-origin/configured origins (the SPA is same-origin anyway).

### H4 — Anonymous RSVP endpoint is an IDOR · **CONFIRMED**
`BoardGameTracker.Api/Controllers/GameNightController.cs:61-73` + `GameNightService.cs:108-129`
`UpdateRsvp` is `[AllowAnonymous]` and resolves by `(PlayerId, GameNightId)` with no ownership/link
check. Anyone can change any player's RSVP by guessing sequential ints. The public path should be gated
by the unguessable `LinkId` (as `GetByLink` is).
**Fix:** require `LinkId` on the anonymous RSVP path and verify the player belongs to that game night.

---

## HIGH — Correctness

### H5 — Loaning an already-loaned game is not prevented
`BoardGameTracker.Core/Loans/LoanService.cs:38-55` + `Game.cs:49-54`
No check for an existing open loan (`ReturnedDate == null`). A game can be "on loan" to two players at once.
**Fix:** reject (409) if the game already has an unreturned loan.

### H6 — Session create/update lacks validation (500s, zero players, multiple winners)
`BoardGameTracker.Core/Sessions/SessionService.cs:65,100` + `Session.cs`
Negative `Minutes` → `end < start` → raw `ArgumentException` surfaced as **500** (not 400); `Minutes = 0`
silently accepted; zero-participant sessions accepted; any number of `Won == true` players allowed, and
`GetWinner()` silently returns the first. When `HasScoring`, the `Won` flag is taken from the client and
never reconciled with actual scores (a lowest scorer can be recorded as winner; no win-direction on Game).
**Fix:** validate `Minutes > 0`, non-empty players, and winner rules as domain exceptions (→ 400).

### H7 — Image files are never deleted (silent unbounded disk growth)
`BoardGameTracker.Core/Images/ImageService.cs:86-98` → `Disk/DiskProvider.cs:26-41`
The stored path is web-relative with a leading slash (`/images/profile/foo.jpg`) but files are written to
`{cwd}/images/profile/...`. `DeleteImage` forwards the stored string to `File.Delete("/images/...")`,
which targets the drive root, fails, and is swallowed by the generic catch. Every replaced/deleted cover
and profile image is orphaned forever. The existing test only asserts pass-through, masking it.
**Fix:** map the stored web path back to the physical path (strip leading `/`, combine `PathHelper.Full*ImagePath`
with `Path.GetFileName`) before deleting; or store the physical path/filename separately.

### H8 — `ImportList` doesn't skip games already in the DB → duplicates / batch failure
`BoardGameTracker.Core/Games/BggImportService.cs:122-148`
`ImportGameFromBgg` guards re-import via `GetGameByBggId`, but `ImportList` never does. Re-importing a
collection creates duplicate `Game` rows (or fails on a unique index), and one bad item rolls back the batch.
**Fix:** `GetGameByBggId(importGame.BggId)` + `continue` when found; consider per-item save/error isolation.

### H9 — Cartesian explosion on game detail
`BoardGameTracker.Core/Games/GameRepository.cs:52-61`
`GetByIdAsync` `Include`s five collections in one query → row count is their product (thousands of rows,
duplicating the large `Description`/image columns). `GetGamesOverviewList` already uses `AsSplitQuery`; this was missed.
**Fix:** add `.AsSplitQuery()`.

### H10 — `SessionRepository.GetByPlayer(playerId, won)` filters on the wrong player
`BoardGameTracker.Core/Sessions/SessionRepository.cs:44-47`
`Where(x => x.PlayerSessions.Any(y => y.Won == won))` checks whether *anyone* won, not the requested player.
Currently dead code (no production caller), but a landmine.
**Fix:** `Any(y => y.PlayerId == playerId && y.Won == won.Value)`, or delete the method.

### H11 — DurationBadge only counts sessions the player won
`BoardGameTracker.Core/Badges/BadgeEvaluators/DurationBadgeEvaluator.cs:13`
Badge is "play for N hours" but the query filters to `.Won`. A player with 20 hours and no wins never earns it.
**Fix:** remove the `Won` filter.

---

## MEDIUM

| # | Area | File | Issue |
|---|------|------|-------|
| M1 | Auth | OidcService.cs:75,239 + OidcController.cs:40 | OIDC `state` is client-supplied, never generated/validated → login CSRF; empty state degrades PKCE cache key to a global collision |
| M2 | Auth | AuthService.cs:73-94 | No refresh-token reuse detection; a rotated stolen token keeps a live chain (`ReplacedByToken` unused) |
| M3 | Auth | Program.cs:130-139 | Rate limiter is one **global** 10/min bucket, not per-client — trivial login DoS for all users |
| M4 | Auth | OidcService.cs:148 | OIDC roles assigned only at first provision, never re-synced on later logins |
| M5 | Data | SessionRepository.cs:139-146 + PlayerService.cs:105 | Deleting a player deletes whole shared `Session` rows → erases other players' history (confirm intent) |
| M6 | Data | ConfigRepository.cs:35-50 | Check-then-insert with no unique index on `Config.Key` → duplicate keys; `ToDictionaryAsync` then throws |
| M7 | Games | GameService.cs:148,184 | BGG expansion endpoints call the client with no try/catch → raw `BoardGameGeekHttpException` / 500 |
| M8 | Data | DbSetExtensions.cs:8-17 | `AddRangeIfNotExists` is N+1 (one `AnyAsync` per item); ~35 queries per BGG import |
| M9 | Data | GameStatisticsRepository.cs:156-205, SessionRepository.cs:131-137 | Count charts materialize full tables client-side; project `GroupBy().Select(Count)` instead |
| M10 | Data | GameNightRepository.cs:69-77, SessionRepository.cs:86-96,111-117 | Multi-collection includes without `AsSplitQuery` (the session one runs on every create/update) |
| M11 | Badges | CloseLossBadgeEvaluator.cs:56-65 | Awards for being close to the *lowest* scorer, not the winner — badge fires despite losing by 50 |
| M12 | Badges | BadgeLevelProgressionPolicy.cs:30,42 + BadgeProgressionService.cs:30-45 | `BadgeLevel.Green == 0 == default` breaks prev/next-level logic (bug codified in tests); latent (no prod caller) |
| M13 | Badges | MonthlyGoalBadgeEvaluator.cs:20-21 | Window anchored to `UtcNow`, not the session date; bulk-imported past sessions never qualify; `>=20` vs "more than 20" |
| M14 | Infra | Program.cs:195-199 + UpdateService.cs:74 | DockerHub Refit client has no timeout (default 100s) and no `CancellationToken` threaded through |
| M15 | Infra | UpdateController.cs:20-26 | `POST /api/update/check` unthrottled, any authed user, live outbound call + up to 4 DB writes per call |
| M16 | Games | GameService.cs:197-201 + Expansion.cs:24-28 | Expansion ctor throws on BGG id `0` / blank name → aborts the whole expansion update |
| M17 | Stats | CompareService.cs:33-34 vs PlayerRepository.cs:59-61 | Win% is a 0–1 fraction in compare but 0–100 in most-played — same-named DTO fields, factor-of-100 mismatch |

## LOW (selected)

- **Games** `GameFactory.cs:70-71` — unrated BGG games store Rating/Weight `0` (via `?? 0`) instead of null → shows "0" not "unknown".
- **Games** `GameService.cs:131` — `UpdateGame` calls `UpdateAdditionDate(command.AdditionDate)` unconditionally; an update omitting the date **wipes** the original (create guards with `HasValue`).
- **Games** `ImageService.cs:100-104` — `CreateFileNameFromUrl` uses `Path.GetExtension(url)`; BGG URLs with query strings yield `.jpg?v=2` or no extension.
- **Games** `GameController.cs:97-102` — `ImportBgg` (GET) lacks the `UserOrAdmin` role restriction every other import endpoint has; `username` unvalidated.
- **Games** `BggImportService.cs:140` — `(decimal)importGame.Price` on a raw `double` → `OverflowException` on NaN/Infinity.
- **Sessions** `SessionService.cs:134-168` — duplicate `PlayerId`s in a command silently overwrite each other (no 400).
- **Loans** `Loan.cs:36-40` — `IsCurrentlyOnLoan` is time-dependent (`UtcNow < ReturnedDate`), disagreeing with `CountActiveLoans` (`ReturnedDate == null`); future return dates read as still-on-loan.
- **Loans** `Loan.cs:24-34,52-67` — no upper bound on return/due dates; `UpdateDates` can un-return a loan and throws raw `ArgumentException` (→ 500).
- **Loans** `Game.cs:49-54` — `LoanToPlayer` ignores its `dueDate` param (only the redundant `SetDueDate` call saves it).
- **GameNights** `GameNightService.cs:44-69` — past `StartDate` accepted (invisible to future-count); bad `HostId/LocationId` fail as FK 500 not 400; unknown game IDs silently dropped.
- **Auth** `RefreshToken.cs:25-26` — refresh tokens stored plaintext (store SHA-256 hash instead).
- **Auth** `OidcController.cs:38-45` — anonymous OIDC endpoints unthrottled; unbounded `IMemoryCache` writes (no `SizeLimit`).
- **Auth** `Program.cs:246-247` — Swagger UI/JSON served unauthenticated in production.
- **Auth** `Program.cs:277-279` — `UseExceptionHandler` only added outside Development → raw 500s + dev/prod divergence; also positioned after rate-limiter/auth.
- **Auth** `Program.cs:187` — sync-over-async `GetBggApiKeyAsync().GetAwaiter().GetResult()` in a DI factory.
- **Auth** `AuthService.cs:227` — modulo bias in temp-password gen (use `RandomNumberGenerator.GetItems`).
- **Data** `LocationRepository.cs:17-23` — `GetAllAsync` loads/tracks all Sessions just to list locations.
- **Data** `LoanRepository.cs:17-22` — `GetAllAsync` override drops `AsNoTracking`.
- **Data** `MainDbContext.cs:55-70` — `BuildIds` scans the wrong assembly (Core, not Common) → no-op.
- **Data** — no optimistic concurrency anywhere (Postgres `xmin` is free via `UseXminAsConcurrencyToken()`).
- **Badges** `ConsistentScheduleBadgeEvaluator.cs` — unused `ConsistentWeeksRequired = 4` constant (evaluator hardcodes 10); UTC `DayOfWeek` may misclassify local-evening games.
- **Badges** `WinningStreakBadgeEvaluator.cs:13` — `OrderByDescending(Start)` unstable on equal timestamps; add `.ThenByDescending(Id)`.
- **Badges** — badges are never revoked when a session is edited/deleted (confirm if by design).
- **Infra** `ImageService.cs:40-43` — `DownloadImage` buffers untrusted remote bytes with no size cap before `Image.Load`.
- **Infra** `UploadFileTypeExtension.cs:8-15` — `ConvertToPath` returns `""` for `Game`/unknown enum (latent path trap).
- **Stats** `CompareService.cs:24-34` — head-to-head `WinPercentage` uses each player's global win rate, easy to misread as vs-opponent.
