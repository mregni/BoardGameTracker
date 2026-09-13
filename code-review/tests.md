# BoardGameTracker test-suite review (read-only, 2026-09-09)

Repo root: `C:\Users\mikha\Documents\Repositories\BoardGameTracker` — all paths below are relative to it unless absolute.
Branch: `feature/236-change-detection` (working tree has uncommitted change-detection work).
Raw artifacts (scratchpad `C:\Users\mikha\AppData\Local\Temp\claude\C--Users-mikha-Documents-Repositories-BoardGameTracker\cbf35dad-7ebd-468e-8fd2-32ba297844bc\scratchpad`): `backend-test.log`, `cov\backend.trx`, `cov\*\coverage.cobertura.xml`, `backend-coverage.csv` (per-class), `parse_cov.py`, `frontend-test.log`, `fe-results.json`, `fecov\coverage-summary.json`, `fecov\lcov.info`.

---

## (a) Suite health

| | Backend (`BoardGameTracker.Tests`) | Frontend (`boardgametracker.client`) |
|---|---|---|
| Runner | xunit.v3 3.2.2 via VSTest (`Microsoft.NET.Test.Sdk` 17.14.1 + `xunit.runner.visualstudio` 3.1.5), Moq 4.20, FluentAssertions 8.10, EF InMemory 10, coverlet 8 | Vitest 4.0.18, jsdom 27, Testing Library, `@vitest/coverage-v8`, `vitest-sonar-reporter` |
| Result | **1699 / 1699 passed**, 0 skipped (961 `[Fact]` + 146 `[Theory]` = 1107 methods, 660 `[InlineData]`) | **1018 / 1018 passed**, 82 files, 508 suites |
| Wall time | **6.85 s** (TRX start→finish; sum of per-test durations 21.5 s, so ~3x parallel speed-up) — VSTest prints "Duration: 1 s", which is a known mis-report for xunit.v3 | **51.8 s** (tests 46.6 s; cumulative `environment` 376 s and `import` 258 s — jsdom boot per file dominates) |
| Tests > 2 s | **none** | **none** (no single test > 1 s) |
| Slowest | 1.34 s `Auth.TokenServiceTests.GetRefreshTokenAsync_ShouldReturnToken_WhenExists`; 1.29 s `Datastore.EfRepositoryTests.GetByIdAsync_ShouldReturnNull_WhenItDoesNotExist`; 1.19 s `Auth.OidcServiceTests.UnlinkExternalLoginAsync_ShouldRemoveLogin_WhenLoginBelongsToUser`; 0.89 s `OidcProviderServiceTests.CreateAsync…`; 0.84 s `PlayerUpdatePersistenceTests…` — all are first-use of an InMemory `MainDbContext` (model build), not real slowness | files: `routes/chat/index.test.tsx` 3.1 s, `routes/chat/-components/ChatComposer.test.tsx` 3.0 s, `components/BgtForm/BgtDatePicker.test.tsx` 2.2 s, `ManualsDialog.test.tsx` 2.0 s |
| Flaky in this run | none | none |
| Noise | none | 24 stderr blocks: 20x Radix "Missing `Description` or `aria-describedby` for DialogContent", 4x "DialogContent requires a DialogTitle" (real a11y defects in modals, e.g. `routes/locations/-modals/NewLocationModal.tsx`, `EditLocationModal.tsx`) |

**Known flaky test — root cause.** The test named in the brief (`GetEnvironmentLogLevel_ShouldReturnWarning_WhenEnvironmentVariableIsUnknownValue` with `" INFO "`) no longer exists; `BoardGameTracker.Tests/Extensions/LogLevelExtensionsTests.cs:25-42` now asserts `" INFO "` → `Information`, which matches production trimming at `BoardGameTracker.Common/Extensions/LogLevelExtensions.cs:10`, and the class sits in `[Collection("EnvironmentVariables")]` (`:9`). That flake is fixed. **A live race remains:** `BoardGameTracker.Tests/Auth/TokenServiceTests.cs:186-204` sets `JWT_SECRET=null` and `BoardGameTracker.Tests/Services/SettingsServiceTests.cs:52-63` (`WithEnvVar`) mutates arbitrary env vars, and neither class is in the collection, while `BoardGameTracker.Tests/Services/EnvironmentProviderTests.cs:192` (in the collection) also writes `JWT_SECRET`. xunit.v3 runs test classes in parallel by default and there is no `xunit.runner.json`, so these can interleave and fail either class intermittently.

---

## (b) Coverage tables

### Backend

Raw cobertura: 16.4 % lines (6 907 / 42 210) because 34 EF migration `.Designer.cs` files (≈33 700 lines) are instrumented locally. CI excludes them via `ExcludeByFile` (`.github/workflows/ci.yml:127`), so the meaningful number is:

| Assembly | Lines (excl. Migrations + generated regex) | Branch |
|---|---|---|
| **Total** | **78.1 %** (6 624 / 8 480) | 75.9 % |
| BoardGameTracker.Api | 79.8 % (599 / 751) | 78.0 % |
| BoardGameTracker.Core | 75.7 % (4 314 / 5 701) | 75.7 % |
| BoardGameTracker.Common | 84.4 % (1 711 / 2 028) | 75.9 % |

246 of 338 production classes are ≥ 95 %. The gap is concentrated, not diffuse:

**Top 20 least-covered risky backend classes** (ranked by uncovered lines × risk; repositories, auth, controllers, AI, infra):

| # | Class | Line % | Uncovered / total | Risk | Why it matters |
|---|---|---|---|---|---|
| 1 | `Core.Auth.OidcService` (`BoardGameTracker.Core/Auth/OidcService.cs`) | 14.5 % | 235 / 275 (branch 2.9 %) | Auth | Whole authorize/callback/token-exchange/user-provisioning/admin-group path (`:60-413`) untested; `OidcServiceTests` only covers unlink/provider lookup |
| 2 | `Core.Games.GameStatisticsRepository` | 0 % | 172 / 172 | Data | 5 `GroupBy` aggregates (`:51,128,161,171,214`) incl. `GroupBy(x => x.Start.DayOfWeek)` — translation only verifiable on Postgres |
| 3 | `Core.Sessions.SessionRepository` | 0 % | 152 / 152 | Data | Play-time aggregates, `GroupBy(DayOfWeek)` (`:109`) |
| 4 | `Core.Compares.CompareRepository` | 0 % | 142 / 142 | Data | Head-to-head aggregates, 3 `GroupBy` (`:36,47,73`) |
| 5 | `Core.Extensions.ServiceCollectionExtensions` | 0 % | 112 / 112 | Infra | DI wiring incl. Npgsql/pgvector data source (`:161-182`); a mis-registration is only caught at boot |
| 6 | `Core.Players.PlayerRepository` | 0 % | 85 / 85 | Data | 3 `GroupBy` on navigation (`:43,54,115`) |
| 7 | `Core.Rag.PdfPageRenderer` | 0 % | 74 / 74 | AI | Native PDF rendering, path logic (`:67`) |
| 8 | `Core.Games.GameRepository` | 0 % | 70 / 70 | Data | Recently-added / expansion counts feeding the dashboard |
| 9 | `Core.Badges.BadgeRepository` | 0 % | 41 / 41 | Data | Badge award persistence |
| 10 | `Common.Entities.Session` | 61.9 % | 40 / 105 (branch 45.5 %) | Domain | `End < Start` throws at `:26` and `:60` **never executed**; `GetWinner/GetHighestScore/GetLowestScore/GetAverageScore/GetPlayers/HasFirstTimePlayers/AddImage` (`:107-162`) untested; excluded from CI/Sonar coverage so invisible |
| 11 | `Api.Controllers.Admin.OidcProvidersController` | 0 % | 36 / 36 | Auth/API | No test file; `[Authorize(Roles=Admin)]` |
| 12 | `Core.Auth.DbSeeder` | 0 % | 36 / 36 | Auth | Seeds roles/admin user at boot |
| 13 | `Api.Controllers.OidcController` | 0 % | 35 / 35 | Auth/API | Login/callback endpoints; no test file |
| 14 | `Common.Extensions.WebHostBuilderExtensions` | 0 % | 34 / 34 | Infra | Host configuration |
| 15 | `Api.Controllers.Admin.UsersController` | 0 % | 29 / 29 | Auth/API | Admin user management; no test file |
| 16 | `Core.ChangeDetection.ChangeDetectionClient` | 80.5 % | 29 / 149 | Ext API (this branch) | Uncovered: blank watchId (`:48-49`), empty batch (`:83-84`), NotConfigured/Misconfigured batch branches (`:102-125`), invalid API key `FormatException` (`:170-173`), `Unreachable` catch (`:226-233`) |
| 17 | `Core.Maintenance.MaintenanceRepository` | 0 % | 27 / 27 | Data | 22 `ExecuteDeleteAsync` calls whose FK ordering (`:19-41`) is only checked by a real DB |
| 18 | `Core.Rag.PdfTextExtractor` | 0 % | 26 / 26 | AI | Text extraction feeding embeddings |
| 19 | `Api.Infrastructure.UtcNullableDateTimeConverter` + `UtcDateTimeConverter` (`DateTimeConverter.cs`) | 0 % | 23 + 16 | API contract | JSON DateTime Kind normalisation; pure and trivially testable |
| 20 | `Core.Configuration.ConfigRepository` | 69.0 % | 22 / 71 | Data | `SetConfigValueAsync` upsert (`:35-50`, uses `ExecuteUpdateAsync`) and `ConvertToString` (`:103-111`) untested — InMemory cannot run `ExecuteUpdate` |

Runners-up: `Core.Email.MailKitSmtpSender` 0 % (15), `Core.Settings.SettingsService` 89.8 % (change-detection URL/API-key validation `:78-96` uncovered — this branch), `Core.Games.LazyBoardGameGeekClient` 60 % (10/25), `Core.Datastore.UnitOfWork` 0 % (10), `Core.Badges.BadgeEvaluators.MonthlyGoalBadgeEvaluator` 0 % (the only evaluator without a test file: 14 test files for 16 evaluators), `Common.Extensions.AuthDtoExtensions` 46.7 %, `Api.Controllers.GameController` 89.6 % (new `GetGamePrice`/`GetWantedPrices` `:120-140` uncovered — this branch), `Core.Datastore.Utc*DateTimeValueConverter` branch 25 % (the `DateTimeKind.Local → ToUniversalTime()` arm untested).

Namespace view (excl. migrations): `Core.Compares` 25 %, `Core.Maintenance` 43 %, `Core.Players` 57 %, `Core.Sessions` 59 %, `Core.Auth` 66 %, `Api.Infrastructure` 69 %, `Core.Games` 73 %; everything else ≥ 78 %, and `Core.Manuals`, `GameNights`, `Loans`, `Dashboard`, `Locations` are 100 %.

### Frontend

Overall (v8, `include: src/**/*.{ts,tsx}`, 356 files): **lines 29.95 %, statements 29.9 %, functions 23.2 %, branches 34.2 %**. **234 of 356 files are at 0 %**, 83 at 100 %. The pattern is "leaf components are well tested, everything that touches data/routing is not":

| Area | Line % | Covered / total | Files |
|---|---|---|---|
| routes/players | 0 % | 0 / 319 | 24 |
| routes/game-nights | 0 % | 0 / 278 | 16 |
| routes/sessions | 0 % | 0 / 221 | 15 |
| routes/_bare (login, reset-password, rsvp, auth-callback) | 0 % | 0 / 161 | 10 |
| routes/compare | 0 % | 0 / 129 | 10 |
| routes/loans | 0 % | 0 / 124 | 7 |
| routes/shames | 0 % | 0 / 24 | 4 |
| routes/-components (Sidebar, BottomNav…) | 3.1 % | 3 / 96 | 10 |
| routes/settings | 10.9 % | 33 / 303 | 21 |
| **services** (16 service modules + 15 query modules) | **14.5 %** | 49 / 339 | 31 |
| routes/-hooks | 17.9 % | 12 / 67 | 6 |
| routes/games | 18.2 % | 146 / 803 | 50 |
| routes/locations | 33.7 % | 30 / 89 | 6 |
| hooks | 43.7 % | 45 / 103 | 10 |
| utils | 55.3 % | 183 / 331 | 13 |
| components/BgtForm | 84.8 % | 217 / 256 | 20 |
| routes/chat | 89.5 % | 213 / 238 | 10 |
| components/BgtTable, BgtCard, BgtButton, BgtDialog, … | 100 % | | |

**Top 20 least-covered risky frontend files** (by uncovered lines, weighted to infra/auth/data):

| # | File | Line % | Uncovered / total | Why it matters |
|---|---|---|---|---|
| 1 | `src/utils/axiosInstance.ts` | 5.3 % | 106 / 112 | JWT attach interceptor (`:104`), 401 → refresh queue + rotation + redirect to `/login` (`:143-233`), 30 s timeout classification (`:49,79`). The single most critical client file; effectively untested |
| 2 | `src/routes/games/table.tsx` | 0 % | 74 / 74 | Modified on this branch (price columns) |
| 3 | `src/services/gameService.ts` | 0 % | 50 / 50 | All game API calls |
| 4 | `src/routes/settings/-components/AccountSettings.tsx` | 0 % | 49 / 49 | Profile/password/user-link flows |
| 5 | `src/routes/game-nights/index.tsx` | 0 % | 48 / 48 | Route + loader |
| 6 | `src/routes/game-nights/-components/MultiSelectField.tsx` | 0 % | 46 / 46 | Form control with own state |
| 7 | `src/routes/sessions/-hooks/useSessionFormState.ts` | 0 % | 45 / 45 | Session form logic (players/scores/winner) |
| 8 | `src/routes/games/index.tsx` | 0 % | 44 / 44 | Games list route + loader |
| 9 | `src/routes/games/import/list_.$username.tsx` | 0 % | 44 / 44 | BGG import route incl. `beforeLoad` redirect guard (`:26-29`) |
| 10 | `src/services/authService.ts` | 0 % | 43 / 43 | login/logout/refresh/status calls |
| 11 | `src/routes/games/-hooks/useGameData.ts` | 0 % | 36 / 36 | Modified on this branch (price query) |
| 12 | `src/routes/players/$playerId_.sessions.tsx` | 0 % | 36 / 36 | Route + loader |
| 13 | `src/routes/loans/-modals/NewLoanModal.tsx` | 0 % | 34 / 34 | Loan creation validation |
| 14 | `src/routes/games/$gameId_.sessions.tsx` | 0 % | 33 / 33 | Route + loader |
| 15 | `src/routes/players/-modals/CreatePlayerModal.tsx` | 0 % | 32 / 32 | |
| 16 | `src/routes/settings/-hooks/useAccountData.ts` | 0 % | 32 / 32 | Account mutations + invalidation |
| 17 | `src/routes/compare/-utils/compareUtils.tsx` | 0 % | 31 / 31 | Pure comparison maths — cheapest win in the list |
| 18 | `src/routes/settings/-modals/CreateUserModal.tsx` | 0 % | 30 / 30 | Admin user creation |
| 19 | `src/hooks/useAuth.ts` | 0 % | 26 / 26 | Zustand persisted auth store (login/logout/setTokens) |
| 20 | `src/routes/_bare/login.tsx` / `auth-callback.tsx` / `reset-password.tsx` | 0 % | 25 / 21 / 27 | Auth entry routes |

Also 0 %: `src/services/queries/invalidations.ts` (`QueryInvalidator`, the cache-consistency contract) and `src/hooks/useQueryInvalidator.ts`; `src/utils/sentry.ts`. Partial: `src/routes/games/-components/GamesFilters.tsx` 45.8 %, `src/services/queries/queryFactory.ts` 63.6 %, `src/components/BgtForm/BgtSelect.tsx` 72.7 %, `BgtSimpleSelect.tsx` 64.7 %.

Files touched on this branch: `GameStaticSection.tsx` 100 %, `BggSettings.tsx` 100 %, `CreateGame.ts` 71 % (new `CreateGame.test.ts`), but `table.tsx` 0 %, `useGameData.ts` 0 %, `TrackedPriceIcon.tsx` 23 %, `$gameId.tsx` 0 %.

---

## (c) Test-quality findings (ranked)

| # | Finding | Where | Problem | Fix | Effort |
|---|---|---|---|---|---|
| 1 | **Repository layer has zero tests; InMemory cannot test it anyway** | `Core/Games/GameStatisticsRepository.cs`, `Core/Sessions/SessionRepository.cs`, `Core/Compares/CompareRepository.cs`, `Core/Players/PlayerRepository.cs`, `Core/Games/GameRepository.cs`, `Core/Badges/BadgeRepository.cs`, `Core/Maintenance/MaintenanceRepository.cs`, `Core/Rag/ManualChunkRepository.cs` — all 0 % | Per ARCHITECTURE the "aggregates / GroupBy / commands" logic lives in repos. 15 `GroupBy` sites, 22 `ExecuteDeleteAsync` (`MaintenanceRepository.cs:19-41` — FK order), 1 `ExecuteUpdateAsync` (`ConfigRepository.cs:42`) and the pgvector `CosineDistance` ordering (`Core/Rag/Specifications/NearestManualChunksSpec.cs:22-25`) are all either untested or untestable on InMemory (ExecuteUpdate/Delete throw, GroupBy is evaluated client-side, `DateTimeKind` is not enforced, `vector` type does not exist). Services are tested against mocks of these repos, so a wrong SQL translation ships silently. | Integration test project on Testcontainers Postgres with the `pgvector/pgvector:pg17` image (see §d). Start with the 4 aggregate repos + `MaintenanceRepository.ResetAsync` + `ConfigRepository.SetConfigValueAsync` upsert. | L (2-3 days incl. infra) |
| 2 | **Env-var race between parallel test classes** | `Tests/Auth/TokenServiceTests.cs:186-204`, `Tests/Services/SettingsServiceTests.cs:52-63` (not in collection) vs `Tests/Services/EnvironmentProviderTests.cs:192`, `Tests/Configuration/ConfigRepositoryTests.cs`, `Tests/Extensions/LogLevelExtensionsTests.cs` (in `[Collection("EnvironmentVariables")]`) | Process-wide state mutated from classes xunit.v3 runs concurrently → intermittent failures (the historical LogLevel flake was this class of bug). Root cause is production reading `Environment.GetEnvironmentVariable` directly (`Core/Auth/TokenService.cs:26`, `Common/Extensions/LogLevelExtensions.cs:9`, `Core/Common/DateTimeProvider.cs:12`) instead of `IEnvironmentProvider`. | Short term: add the two classes to the collection. Proper: route `JWT_SECRET`/`LOGLEVEL`/`TZ` reads through `IEnvironmentProvider` so tests inject values. | S / M |
| 3 | **Domain logic in entities is untested and hidden from coverage** | `Common/Entities/Session.cs:26,60` (End<Start throw, never executed), `:107-162` (winner/score/duration queries); only `Tests/Entities/GameTests.cs` exists. CI/Sonar exclude `**/Entities/**` (`ci.yml:108-109,127`) | The DDD-lite `Update*()` rules are the validation layer, but `sonar.coverage.exclusions` and `ExcludeByFile` treat entities as POCOs, so a 62 %-covered `Session` shows as "excluded". `SessionServiceTests` has no "end before start" case either (the 21 test names in `Tests/Services/SessionServiceTests.cs` contain no such negative). | Add `SessionTests.cs` (mirror `GameTests`), `GameNightTests`, `LoanTests` if missing; remove `**/Entities/**` from both exclusion lists (keep `Migrations`, `Host`, `Datastore`). | S |
| 4 | **Specification tests assert Include counts** | `Tests/Specifications/Games/GameSpecsTests.cs:72,90`; `GameNights/GameNightByIdWithDetailsSpecTests.cs:29`; `GameNightByLinkIdSpecTests.cs:25`; `GameNightsOverviewSpecTests.cs:36`; `RsvpSpecTests.cs:25,43`; `Players/PlayerSpecsTests.cs:37`; `Sessions/SessionQuerySpecsTests.cs:60,76`; `SessionRepositorySpecsTests.cs:31,86` (12 sites); `Rag/NearestManualChunksSpecTests.cs:31-35` asserts `Selector.Should().NotBeNull()` | `IncludeExpressions.Should().HaveCount(6)` breaks on any harmless Include change and passes when the *wrong* navigation is included. The filter/order assertions in the same files are good; only the Include-count assertions are noise. | Replace with `spec.IncludeExpressions.Select(i => i.EntityType/…)` on the specific navigations, or better, drop them and let the integration tests (finding 1) load the graph for real. | S |
| 5 | **`VerifyNoOtherCalls()` helpers embed hidden `Times.Once` assertions; 731 call sites** | `Tests/Services/BggImportServiceTests.cs:57` (`IsBggEnabled` Once), `Tests/Services/BadgeServiceTests.cs:58` (`GetAllAsync` Once), `Tests/Services/SettingsServiceTests.cs:46-49` (`VerifyGet … Once` x3); 52 files, 731 `VerifyNoOtherCalls()` | Every test in a class implicitly asserts exact call counts of a dependency it may not care about. Adding a cache check, a log, or a second read to a service fails 30+ unrelated tests. This is implementation-coupling, not behaviour. | Keep `VerifyNoOtherCalls` only for side-effecting deps (`IUnitOfWork`, `IEmailService`, `IDiskProvider`); use `MockBehavior.Loose` + result assertions for read-only deps; move `Times.Once` into the tests that actually care. | M (mechanical) |
| 6 | **84 interaction-only tests (Verify, no result assertion)** | e.g. `Tests/Auth/AuthServiceTests.cs:297,319,335,355,820,831`; concentrated in `AuthServiceTests` (10), `BggImportServiceTests` (10), `BadgeServiceTests` (9), `UpdateServiceTests` (8), `ImageServiceTests` (7); `Tests/Services/DashboardServiceTests.cs:270-275` `GetStatistics_ShouldCallAllRepositoryMethodsExactlyOnce` has **no assertion at all beyond the shared `VerifyGetStatisticsCalls()` helper** and duplicates the preceding test | For void/side-effect methods this is fine; for `ForgotPasswordAsync_ShouldDoNothing_*` and the Dashboard test it verifies the mock choreography rather than an observable outcome. | Assert on returned DTO / persisted state where one exists; delete `DashboardServiceTests.cs:270`. | S |
| 7 | **No test-data builders; large hand-rolled arranges** | `Tests/Controllers/CountControllerTests.cs` — 80-line constructor for 1 test; `Tests/Auth/AuthServiceTests.cs` — 11 mocks, 38-line ctor, 1 162 LOC; `BggImportServiceTests.cs` 1 186 LOC; `GameServiceTests.cs` 1 081 LOC; 123 raw `new Game/Session/Player(…)` across `Services/`, `Auth/`, `Controllers/`; helpers are private and per-class (`Evaluators/FirstTryBadgeEvaluatorTests.cs:149`, `Rag/ManualIndexingServiceTests.cs:271`, `Services/ManualServiceTests.cs:70-80`) | No AutoFixture/Bogus/builder anywhere (`grep` finds only the production `PublicUrlBuilder`). Entities have private setters + `Update*()`, so AutoFixture would fight the model; hand-written builders fit. | Add `Tests/TestData/{GameBuilder,SessionBuilder,PlayerBuilder,GameNightBuilder,RefreshTokenBuilder}.cs` and a `LoggerMock<T>` verifier (the `VerifyLogInformation` helper in `DiskProviderTests` is a good seed). Bogus optional for realistic names. | M |
| 8 | **Controllers are unit-tested in isolation; the ASP.NET pipeline never runs** | `Tests/Controllers/*` construct controllers directly (`GameControllerTests.cs:29-42`); no `WebApplicationFactory`, Testcontainers or Sqlite anywhere; 69 `[Authorize]`/`Roles=` sites, `[EnableRateLimiting("changedetection")]` (`Api/Controllers/GameController.cs:121,134`), `ValidateIdFilter` registration (`Host/Program.cs:212`), JWT bearer options (`Program.cs:130-150`) | `ValidateIdFilterTests`, `GlobalExceptionHandlerTests`, `AuthDisabledMiddlewareTests` cover the pieces, but nothing proves they are wired, that `[ApiController]` returns the expected 400 shape, that an Admin-only endpoint returns 403 for a User, or that the rate limiter policy exists. `OidcController`, `Admin/OidcProvidersController`, `Admin/UsersController` have no tests of any kind. | Integration project (§d): an auth matrix theory (anonymous/User/Admin × endpoint → 401/403/200), one 400-shape test, one rate-limit test, OIDC/Admin endpoint smoke tests. | M-L |
| 9 | **Auth edge cases** | `Tests/Auth/AuthServiceTests.cs`, `Tests/Auth/TokenServiceTests.cs` | Service-level coverage is actually good (login wrong-password/locked-out, refresh not-found/inactive/expired, logout scoping, register duplicates, revoke/cleanup, reset/forgot). Missing: expiry *boundary* — `Common/Entities/Auth/RefreshToken.cs:15` uses `DateTime.UtcNow` directly so "expires exactly now" is untestable; replay of a *rotated* token is only covered implicitly by "inactive"; role checks are only tested through `usePermissions` on the client and never on the server; OIDC (finding 1 of §b). | Inject `IDateTimeProvider` (already exists in `Core/Common`) into `RefreshToken.IsExpired` evaluation (or pass `now`); add explicit "rotated token replay → 401 and family revoked" test if that is the intended behaviour; OIDC service tests with a fake `HttpMessageHandler` (pattern already used in `Tests/Rag/AiClientFactoryTests.cs:221`). | M |
| 10 | **Price parsing / change detection (this branch)** | `Tests/ChangeDetection/ChangeDetectionSnapshotParserTests.cs` (7 tests, `InlineData` at `:61-77`), `ChangeDetectionClientTests.cs` (9 tests) | Parser covers comma decimal, ambiguous "1,299.99"/"1.299,99" rejection, empty/malformed. Missing: currency symbols/prefix-suffix (`€ 12,50`, `12.50 EUR`), thousands with space/apostrophe, zero/negative, price with trailing text, stock-token case variants. Client: the five uncovered branches listed in §b #16 are exactly the failure-mode branches (NotConfigured / Misconfigured / Unreachable / invalid key) — i.e. the silent-failure theme from the earlier review is untested. | Extend the theory; add one test per `ChangeDetectionStatus` outcome. | S |
| 11 | **`NotThrow`-only test** | `Tests/Services/DiskProviderTests.cs:81-90` `EnsureFolder_ShouldNotThrow_WhenDirectoryAlreadyExists` | Sole assertion is `NotThrow`; the other 11 `NotThrow` sites (`AuthServiceTests.cs:881-896`, `DiskProviderTests.cs:62-113`, `Rag/ManualIndexingServiceTests.cs:206-220`) also verify mocks/logs, so they are fine. | Assert the directory still exists and is unchanged. | XS |
| 12 | **Frontend: global i18n mock means keys are never validated; no key-consistency test** | `boardgametracker.client/src/test/setup.ts:6-33` (key-echo `t`) ; `public/locales/{base,en-US,es-ES,nl-BE,nl-NL}` (26 namespaces, 931 base keys) | Every locale is missing the same 5 keys vs `base` (`error.json`: `auth.account-locked-out`, `auth.invalid-auth-session`, `auth.invalid-redirect-uri`, `image.too-large`, `image.unsupported-format`), and a static scan finds ~16 `t("…")` literals that do not resolve to a base key (some are plural forms, e.g. `dashboard:welcome-back` with `count`). Nothing would catch a typo'd key. | Add `src/i18n.keys.test.ts`: (1) every `t("ns:key")` literal in `src/**` exists in `base` (plural-aware), (2) every locale namespace has the base key set (warn-only if Crowdin lags). | S |
| 13 | **Frontend: routing, loaders and guards have no tests** | 31 `createFileRoute` files, 20 `loader:` and 3 `beforeLoad` guards (`src/routes/chat/index.tsx:36-39`, `src/routes/games/import/list_.$username.tsx:26-29`, `src/routes/games/import/start.tsx:18-21`); no test uses `createRouter`/`RouterProvider`/`createMemoryHistory`; `src/routes/__root.test.tsx:21-64` mocks `@tanstack/react-router`, `useAuth`, `settingsService`, Sidebar and BottomNav — so `__root.tsx` reads 100 % while asserting nothing about real routing | Redirect-when-feature-disabled and prefetch-on-load are the behaviours most likely to regress on a settings change and are unverifiable today. | Route harness: build a router from the generated `routeTree` with `createMemoryHistory`, MSW for data, assert redirects/prefetch. Start with the 3 `beforeLoad` guards. | M |
| 14 | **Frontend: services and the HTTP layer are never executed; mocking at the query-module level** | 16 service modules 14.5 %; `src/utils/axiosInstance.ts` 5 %; `src/services/queries/invalidations.ts` 0 %; 13 test files `vi.mock` service/query modules (e.g. `src/routes/-hooks/useMenuInfo.test.tsx:9-35`, `src/routes/chat/index.test.tsx:37-48`, `src/routes/games/-hooks/useGameManuals.test.ts:21`); no MSW/axios mock anywhere | The 401-refresh queue, `QueryInvalidator` fan-out (the app's cache-consistency contract) and every request/response mapping are untested. Mocking `@/services/queries/*` also freezes the query-key shape into each test. | Add MSW (`msw` + `setupServer` in `setup.ts`), test `axiosInstance` refresh/redirect with `axios-mock-adapter` or MSW, unit-test `QueryInvalidator` against a real `QueryClient` (pure, 30 min). | M |
| 15 | **Frontend: over-mocking and redundant mocks** | `src/routes/-hooks/useMenuInfo.test.tsx:44-70` mocks 7 SVG icons although `vitest.config.ts:6` already loads the `svgr()` plugin; `__root.test.tsx` (9 mocks) | Redundant mocks add maintenance and hide real import errors. | Delete the SVG mocks; if icons are noisy, add one global `vi.mock` for `*.svg?react` in `setup.ts`. | XS |
| 16 | **Frontend: 19 tests whose only assertion is `toBeInTheDocument`** | e.g. `src/components/BgtTable/BgtTable.test.tsx:103` ("should render tbody element"), `src/components/BgtMenu/BgtMenuLogo.test.tsx:7`, `src/components/BgtLoadingSpinner/BgtLoadingSpinner.test.tsx:11`, `src/routes/chat/-components/SourcesOverlay.test.tsx:22`, `src/routes/locations/-modals/NewLocationModal.test.tsx:106` and `EditLocationModal.test.tsx:128` ("should not call saveLocation … empty name" asserts presence, not `not.toHaveBeenCalled`) | Render-smoke tests; the two modal tests are named as negative tests but do not assert the negative. | Add the `expect(saveLocation).not.toHaveBeenCalled()` / validation-message assertions; leave pure smoke tests but do not count them as coverage of behaviour. | XS |
| 17 | **Naming / folder-mirroring inconsistencies (backend)** | 193 of 1 107 methods (17 %) use `Method_WithX_ShouldY` instead of `Method_ShouldY_WhenX` (`Infrastructure/GlobalExceptionHandlerTests.cs`, `Services/DiskProviderTests.cs`, `Core/LoanValidationTests.cs`, `ValueObjects/*`); `Tests/Services/` is a flat bucket for 24 classes from 15 `Core/*` aggregate folders, `Tests/Evaluators` ↔ `Core/Badges/BadgeEvaluators`, `Tests/DomainServices` (4 files) ↔ `Core/{Badges,Compares,Games,Players}`, `Tests/Core` holds `DateTimeProviderTests` + two loan entity tests, `Tests/Entities` holds one file. `Tests/Specifications/<Aggregate>` does mirror correctly. | Discoverability; new contributors put tests in the wrong place. | Adopt `Tests/<Aggregate>/…` mirroring `Core/<Aggregate>` (as Specifications already do); pick one naming style and add an `.editorconfig`/analyzer note. | S (mechanical moves) |
| 18 | **Frontend a11y warnings surface as test noise** | 24 stderr blocks: Radix `DialogContent` missing `Description`/`DialogTitle` (`NewLocationModal`, `EditLocationModal`, others) | These are real screen-reader defects that tests already detect but nobody fails on. | Fix the dialogs; then make `setup.ts` fail on unexpected `console.error`/`warn`. | S |

Positives worth keeping: consistent AAA structure; `Theory`/`InlineData` used heavily (660); fake `HttpMessageHandler` pattern in `Tests/Rag/AiClientFactoryTests.cs` and `ChangeDetectionClientTests`; `Tests/Datastore/PlayerUpdatePersistenceTests.cs` guards the read-spec-vs-update-spec tracking contract; `ImageServiceTests` covers size/format/path-traversal negatives; badge evaluators have 14 focused suites; `usePermissions.test.ts` covers the auth-disabled/null cases; `test-utils.tsx` builds a fresh `QueryClient` per render.

---

## (d) Missing test kinds and infra recommendations

1. **Backend integration test project (`BoardGameTracker.IntegrationTests`) — highest value.**
   - `WebApplicationFactory<Program>`: `Host/Program.cs` uses top-level statements and calls `RunDbMigrations` + `context.Database.Migrate()` (`:417-420`) at boot; add `public partial class Program {}` at the end of the file and read the connection string from `IEnvironmentProvider` so the factory can point at a container.
   - Database: **Testcontainers.PostgreSql with image `pgvector/pgvector:pg17`**, not Sqlite — `MainDbContext.cs:204-206` registers the `vector` extension and `NearestManualChunksSpec` uses `CosineDistance`; Sqlite cannot host that, and it would also mis-model `ExecuteUpdate`/`DateTimeKind`/`GroupBy` translation the same way InMemory does. Run real migrations against the container (this also makes the 34 currently un-executed migrations part of CI).
   - One `ICollectionFixture` per collection so the container starts once (~5-10 s); keep it out of the unit project so the 7 s unit run stays fast.
   - Alternative: `Aspire.Hosting.Testing` against the existing `BoardGameTracker.AppHost` — heavier (Ollama/JS resources) and slower; Testcontainers is the pragmatic choice.
   - First targets, in order: repositories (§c #1), `[Authorize]` matrix and `[ApiController]`/`ValidateIdFilter` 400 shapes (§c #8), `MaintenanceRepository.ResetAsync` FK ordering, `ConfigRepository.SetConfigValueAsync` upsert, OIDC/Admin controllers, `ServiceCollectionExtensions` (a single "container resolves every controller" test catches DI mistakes).

2. **Test-data builders** (§c #7) — `Tests/TestData/*Builder.cs`, hand-written fluent builders over the DDD constructors; a `LoggerMock<T>` helper. Skip AutoFixture (private setters, guarded ctors).

3. **xunit.v3 configuration** — add `BoardGameTracker.Tests/xunit.runner.json` (`CopyToOutputDirectory`): `{"longRunningTestSeconds": 2, "diagnosticMessages": false, "parallelizeTestCollections": true}`. `longRunningTestSeconds` gives the ">2 s" report the brief asked for on every run. Keep parallel collections but eliminate process-global state (§c #2).

4. **Coverage gating** — today nothing fails on a coverage drop: `ci.yml` posts a ReportGenerator summary as a sticky PR comment (`:183-197`) and hands OpenCover/lcov to SonarCloud (`:103-106`); the SonarCloud quality gate is only enforced if its status check is *required* in branch protection (verify). Add `coverlet` `Threshold`/`ThresholdType=line` on new code is awkward; the simplest enforceable step is (a) Vitest `coverage.thresholds` with `autoUpdate: true` starting at current values (lines 29, functions 23, branches 34) and (b) Sonar "new code coverage ≥ 80 %" as a required check.

5. **CI fixes** (`.github/workflows/ci.yml`):
   - `:52-55` pins `dotnet-version: "8.x"` while every csproj targets `net10.0`; the build only succeeds because the `ubuntu-latest` image happens to preinstall .NET 10. Pin `10.x` (or add `global.json`).
   - `:105` `sonar.javascript.lcov.reportPaths="coverage/lcov.info"` is relative to the repo root, but the file is written to `boardgametracker.client/coverage/lcov.info` (the `sed` at `:154` runs inside that directory and ReportGenerator at `:185` uses the prefixed path). Unless SonarCloud resolves it differently, JS coverage is not being imported — check the scanner `end` log for "Could not resolve … lcov". `sonar.testExecutionReportPaths` at `:106` already uses the prefixed path.
   - Both suites do run on every PR (`ci.yml` is the only test workflow; `pr.yml` is labeler + semantic-title only), .NET results are published with `fail_on: test failures` (`:158-164`), frontend failures fail the step. Coverage exclusions drop `**/Entities/**` (§c #3) — narrow them.
   - Frontend `vitest.config.ts` has only `reporter: ['lcov']`; add `text-summary` (and `json-summary`) so the PR comment / local runs show numbers without opening lcov.

6. **Frontend infra** — MSW (`setupServer` in `setup.ts`, handlers per service), a router harness (§c #13), an i18n key test (§c #12), fail-on-console-error. Consider `happy-dom` or `pool: 'threads'` to cut the 376 s cumulative jsdom environment cost (wall 52 s today; fine, but will grow with route tests).

7. **E2E** — none exists (no Playwright/Cypress). Recommend a small Playwright smoke suite (login → create game → log session → badge appears → shelf-of-shame) against `docker-compose.yml`, run nightly or on `master` merges rather than per-PR; it is the only thing that would catch the "route loader + API + DB" seam until the integration project matures.

---

## (e) Quick wins (≤ 1 h each)

1. Add `[Collection("EnvironmentVariables")]` to `Tests/Auth/TokenServiceTests.cs` and `Tests/Services/SettingsServiceTests.cs` (kills the remaining flake source).
2. `Tests/Entities/SessionTests.cs`: `End < Start` throws in ctor and `UpdateTimes` (`Session.cs:26,60`); `GetWinner`, `GetHighest/Lowest/AverageScore` with and without `HasScoring`, `HasFirstTimePlayers`.
3. This branch: `SettingsServiceTests` for invalid change-detection URL / non-http scheme / API-key null-vs-blank tri-state (`SettingsService.cs:78-96`); `ChangeDetectionClientTests` for the five uncovered status branches; `GameControllerTests` for `GetGamePrice` 404/200 and `GetWantedPrices` (`GameController.cs:120-140`); extend `ChangeDetectionSnapshotParserTests` with currency-symbol / zero / trailing-text cases.
4. `Tests/Evaluators/MonthlyGoalBadgeEvaluatorTests.cs` (only evaluator without tests, 0 %).
5. `Api/Infrastructure/DateTimeConverter.cs` JSON converter tests (Utc / Local / Unspecified / null) and the `DateTimeKind.Local` arm of `Core/Datastore/UtcDateTimeValueConverters.cs:16,32`.
6. Delete `DashboardServiceTests.cs:270` or give it a result assertion; add the missing `not.toHaveBeenCalled` to `NewLocationModal.test.tsx:106` / `EditLocationModal.test.tsx:128`.
7. Frontend: unit-test `src/services/queries/invalidations.ts` against a real `QueryClient` (spy on `invalidateQueries`), and `src/hooks/useAuth.ts` with `authService` mocked — both 0 % and pure.
8. Frontend: remove the 7 SVG `vi.mock`s in `useMenuInfo.test.tsx:44-70`; add `text-summary` reporter + `thresholds { autoUpdate: true }` to `vitest.config.ts`.
9. `xunit.runner.json` with `longRunningTestSeconds: 2`.
10. CI: `dotnet-version: "10.x"`; fix the Sonar lcov path; drop `**/Entities/**` from the two coverage-exclusion lists.
11. `src/i18n.keys.test.ts` (base ↔ usage, base ↔ locales) — immediately flags the 5 missing `error.json` keys.

---

## (f) Overall assessment

Both suites are green, fast (6.9 s / 52 s) and free of slow tests, and the backend's service/controller/evaluator layers are genuinely well covered (78 % ex-migrations, 246/338 classes ≥ 95 %). The real risk is structural rather than volumetric: everything that talks to Postgres (all repositories, ExecuteUpdate/Delete, GroupBy aggregates, pgvector search, migrations) and everything that runs the ASP.NET pipeline ([Authorize] roles, filters, rate limiting, OIDC/Admin endpoints) has zero executed tests, and InMemory cannot close that gap — a Testcontainers-Postgres integration project is the single highest-value investment. On the frontend, leaf components are solid but 234/356 files (routes, loaders, guards, services, the axios refresh interceptor, query invalidation) are at 0 %, so the 30 % headline is an honest reflection of "presentational only" testing. Test quality is disciplined but over-coupled to Moq choreography (731 `VerifyNoOtherCalls`, hidden `Times.Once`, Include-count assertions), which will tax refactoring more than it protects behaviour. CI runs both suites and ships coverage to Sonar, but nothing gates on coverage, entities are excluded from the numbers, the dotnet SDK pin is wrong, and the Sonar lcov path looks unresolved — all cheap to fix.
