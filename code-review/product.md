# BoardGameTracker — product-level review (read-only, 2026-09-09)

Scope: `feature/236-change-detection` working tree. Every claim below is grounded in a file:line reference (paths relative to the repo root; frontend paths relative to `boardgametracker.client/src`). Confidence is stated where the behaviour was inferred from code rather than observed at runtime.

---

## (a) Wrong / misleading behaviours (ranked by user impact)

### A1. Deleting a player deletes every session that player ever took part in — including the other players' results
- `BoardGameTracker.Core/Players/PlayerService.cs:96-111` — `Delete` calls `_sessionRepository.DeleteByPlayerIdAsync(id)` (line 105) before deleting the player.
- `BoardGameTracker.Core/Sessions/SessionRepository.cs:113-120` — `DeleteByPlayerIdAsync` removes **whole `Session` rows** where the player appears (`s.PlayerSessions.Any(ps => ps.PlayerId == playerId)`), not just that player's `PlayerSession` row.
- The schema already cascades `PlayerSession` on player delete (`BoardGameTracker.Core/Datastore/MainDbContext.cs:248-252`), so the extra call is what destroys shared history. A 4-player session vanishes from the other three players' stats, badges stay awarded, game play counts drop.
- The confirm dialog does say "will delete all game sessions connected to this player" (`public/locales/en-US/player.json` → `delete.description`), so it is intentional, but it is the wrong product rule for shared data.
- **Fix:** drop the `DeleteByPlayerIdAsync` call and rely on the `PlayerSession` cascade; optionally delete only sessions that end up with zero players. Offer "merge into another player" instead (see F-list).

### A2. Deleting a location or a player silently cascades all game nights they host / are held at
- `BoardGameTracker.Core/Datastore/MainDbContext.cs:132-147` (`BuildGameNights`) never configures the `Host`/`Location` relationships, so EF picks the required-FK default: `MainDbContextModelSnapshot.cs:1591-1601` shows `HostId` → `OnDelete(Cascade)` and `LocationId` → `OnDelete(Cascade)`.
- The location delete dialog only warns about *sessions* (`routes/locations/index.tsx:133-143`) — and even that warning can never show (see A14). Deleting a location used by a game night removes the game night with all RSVPs; deleting the host player does the same.
- **Fix:** `OnDelete(DeleteBehavior.Restrict)` for both + a 409/400 with a translated message, or reassign host/location in the UI before delete.

### A3. Badges are evaluated before the new session is saved → count/streak badges are off by one and "First Try" can never be earned on an actual first win
- `BoardGameTracker.Core/Sessions/SessionService.cs:35-44`: `CreateAsync` → `AwardBadgesAsync` → `SaveChangesAsync`. `EfRepository.CreateAsync` only adds to the change tracker (`BoardGameTracker.Core/Datastore/EfRepository.cs:23-27`).
- `BoardGameTracker.Core/Badges/BadgeService.cs:37-38` loads the player's sessions with a DB query (`GetByPlayerBatchAsync`, `SessionRepository.cs:66-93`); an unsaved `Added` entity is not returned by an EF query, so evaluators see the history **without** the session being logged.
- Consequences: `FirstTryBadgeEvaluator.cs:12-14` requires `count == 1` → false on the first play (count 0) and **true on the second play** if that one is won ("Win a game on first try" is awarded for winning your second game). `SessionsBadgeEvaluator.cs:13`, `SessionWinEvaluator.cs:12`, `WinningStreakBadgeEvaluator.cs:12-17`, `LearningCurveBadgeEvaluator.cs:21-25` all lag one session. `ConsistentScheduleBadgeEvaluator.cs:20-32` includes `i = 0` (today's Saturday) in `requiredSaturdays`, so it can only be awarded on an *edit*, never on create.
- The unit tests assume the opposite semantics: `BoardGameTracker.Tests/Evaluators/FirstTryBadgeEvaluatorTests.cs:38` builds `sessions = [currentSession]`. On the update path the tracked entity *is* returned (identity resolution), so create and update behave differently.
- **Fix:** save first, then award (`SessionService.Create`: `SaveChangesAsync` → `AwardBadgesAsync` → `SaveChangesAsync`), or append `session` to `playerSessions` when it is not present. Add revocation on session delete/edit (badges are never revoked: `SessionService.Delete` lines 46-52 does nothing with badges).

### A4. The "Not self owned" game state exists only in the frontend → every form that offers it produces a 400
- `models/Games/GameState.ts:1-7` has `NotOwned = "notOwned"`; `BoardGameTracker.Common/Enums/GameState.cs:3-9` does not.
- It is offered through `Object.values(GameState)` in the manual create/edit form (`routes/games/-components/GameFormPlayerFields.tsx:81-93`), the BGG add form (`routes/games/bgg.tsx:119-131`) and the table state filter/inline editor (`routes/games/table.tsx:87-93, 104-107`), with a translation (`game.json` → `state.not-owned`) and colour (`utils/ItemStateUtils.ts:15-16, 39-40`). `JsonStringEnumConverter` rejects it → "Game creation failed"/"Failed updating game" toast with no explanation.
- **Fix:** remove it from the TS enum (or add it to the backend enum + BGG `StatusExtensions`).

### A5. The "Language" setting does not change the UI language; Spanish can never load; nl-NL is not selectable
- No `i18n.changeLanguage` call exists anywhere (grep over `src`). `utils/i18n.ts:41-46` uses the browser `LanguageDetector`; `settings.uiLanguage` is only used for date-fns locales (`utils/dateUtils.ts:65`, `routes/compare/-utils/compareUtils.tsx:30`).
- `supportedLngs: ["en-US", "nl-NL", "nl-BE"]` (`utils/i18n.ts:46`) excludes `es-ES`, although `public/locales/es-ES/*.json` is fully translated (26 files, key parity with en-US).
- The settings dropdown lists only the seeded `Language` rows `en-us` and `nl-be` (`MainDbContext.cs:338-346`, rendered by `routes/settings/-components/GeneralSettings.tsx:22-34`). nl-NL and es-ES are unreachable from settings.
- **Fix:** apply `i18n.changeLanguage(settings.uiLanguage)` once settings load (and on save), add `es-ES` to `supportedLngs`, seed `nl-nl`/`es-es`.

### A6. Editing a session hides its expansions
- `routes/sessions/-hooks/useSessionFormState.ts:99-105` initialises `expansionList: []` and only fills it in the store subscription when `gameId` **changes** (lines 112-134). On `/sessions/update/:id` the game id never changes, so `SessionExpansionSelector` renders "No expansions added to this game" (`routes/sessions/-components/SessionExpansionSelector.tsx:27-32`) even though `selectedExpansionIds` holds the saved ids. The user cannot see or change which expansions were used unless they switch game and back.
- **Fix:** initialise `expansionList` from `games.find(g => g.id === initialGameId)?.expansions ?? []`.

### A7. "Add expansion" picker: backend and frontend disagree on the payload shape (appears broken — verify at runtime)
- `GET api/game/{id}/expansions` returns `ExpansionData { Title, BggId }` (`BoardGameTracker.Api/Controllers/GameController.cs:150-156`, `BoardGameTracker.Core/Games/GameService.cs:173-177`, `BoardGameTracker.Common/Models/ExpansionData.cs:3-7`) → JSON `{ title, bggId }`.
- The client types it as `ExpansionLink { id, value }` (`models/Games/ExpansionLink.ts:1-4`, `services/gameService.ts:72-76`) and hands it to `BgtCheckboxList`, which reads `item.id`/`item.value` (`components/BgtForm/BgtCheckboxList.tsx:5-8, 37-46`) via `routes/games/-modals/ExpansionSelectorModal.tsx:40-45`. Labels resolve to `undefined`, keys collide (`item-undefined`), and the posted `expansionBggIds` would be `[null]`.
- No frontend test covers the picker. `ExpansionData` was last touched in `86063d2a` (BGG nuget swap) — this looks like a model that was never updated after the backend change. **Confidence: high from code; not executed.**
- **Fix:** map `{ id: x.bggId, value: x.title }` in `useExpansionSelectorModal` (or return `{id,value}` from the API and update the test at `GameControllerTests.cs:320-337`).

### A8. Scoring-results card shows "Top score 0" / "Highest losing 0" for null scores and for games without scoring
- `BoardGameTracker.Core/Games/GameStatisticsRepository.cs:175-181` (`GetHighestScoringPlayer`) and `183-190` (`GetHighestLosingPlayer`) use `OrderByDescending(x => x.Score)` with no `Score != null` filter. PostgreSQL sorts NULLs first in DESC order, so any player session with a null score (non-scoring games, or a scoring game edited after `HasScoring` was toggled) is picked and rendered as `0` by `ScoreRank.Make` (`BoardGameTracker.Common/Models/Charts/ScoreRank.cs:46-59`).
- Only `GetPlayerScoringChart` is guarded by `HasScoring` (`GameChartService.cs:75-79`); `GetScoringRankedChart` (lines 118-137) is not, and `ScoringResultsCard.tsx:24` only hides when the list is empty → co-op/no-score games get a "Scoring results" card full of zeros.
- **Fix:** return `[]` when `!HasScoring`; add `.Where(x => x.Score != null)` to the four rank queries.

### A9. Dashboard money figures are wrong for anyone who imports from BGG or owns wishlist/sold games
- `GetTotalPayedAsync`/`GetMeanPayedAsync` (`GameStatisticsRepository.cs:102-122`) sum/average `BuyingPrice` over **all** states — Wanted, PreviouslyOwned and ForTrade included — yet the card is labelled "Collection value" (`routes/index.tsx:120-127`).
- BGG add form defaults `price: 0` and the zod schema coerces it as a required number (`routes/games/bgg.tsx:50-57`, `models/Games/BggSearch.ts:20-22`); the collection importer sets `price: existingGame?.buyingPrice ?? 0` (`routes/games/import/-hooks/useList.ts:49`). `GameFactory.CreateFromBggAsync` stores `Price(0)` (`BoardGameTracker.Core/Games/Factories/GameFactory.cs:73`). Every imported game therefore counts as €0 in "Average cost", and "Price per play" shows `0` instead of "unknown" (`GetPricePerPlay`, lines 19-37).
- "Active players" is simply `Players.Count()` (`DashboardService.cs:37`); "Expansions owned" is `Expansions.CountAsync()` regardless of the parent game's state (`GameRepository.cs:83-86`).
- **Fix:** treat 0 as null on import, filter `State == Owned` for value/avg, rename or recompute "active".

### A10. Shelf of Shame shames a game the day you buy it
- `GamesWithNoRecentSessionsSpec.cs:12` / `ShameGamesSpec.cs:13`: `State == Owned && !Sessions.Any(s => s.Start >= cutoff)`. `AdditionDate` is ignored, so a game added yesterday with the default 6-month threshold is immediately "shamed". Docs promise "games you own but have never played" (`docs/src/content/docs/index.mdx:52-55`) — the implementation is "not played in the last N months".
- `ShameService.GetShameGames` (lines 43-49) ignores `ShelfOfShameEnabled` (only the menu count does, lines 29-41), so `/shames` keeps working when the feature is disabled.
- **Fix:** `&& (g.AdditionDate == null || g.AdditionDate <= cutoff)`; honour the toggle in the service.

### A11. Game "Top players" hides everyone without a win and mislabels a one-game delta as a "trend"
- `GameChartService.GetTopPlayers` (`BoardGameTracker.Core/Games/GameChartService.cs:55-70`) filters `.Where(x => x.Wins > 0)` and ranks by raw wins. For co-op games or players who never won, the card renders an empty body with no message (`routes/games/-components/TopPlayersCard.tsx:22-30`).
- `TopPlayerDto.CreateTopPlayer` (`BoardGameTracker.Common/DTOs/TopPlayerDto.cs:26-44`) computes "trend" as win% including the last game vs. excluding it — i.e. "did you win your last game", shown as an up/down arrow.

### A12. Session location is mandatory in the UI, optional in the domain — and there is no way to create one inline
- `models/Session/CreateSession.ts:26-32` requires `locationId > 0`; `Session.LocationId` is `int?` (`BoardGameTracker.Common/Entities/Session.cs:33-34`). A fresh install cannot log a session until a Location is created under the "More" menu. The i18n key `player-session:new.location.create-new` exists (`player-session.json`) but no code uses it (grep).
- **Fix:** make the select clearable/optional or add an inline "create location" action like players have (`CreateSessionPlayerModal.tsx:92-96`).

### A13. Fields that exist in the domain but can never be edited from the UI
- `hasScoring` switch is hidden on edit (`routes/games/-components/GameForm.tsx:100-106` renders it only when `game === undefined`) even though `GameService.UpdateGame` applies it (`GameService.cs:130-131`). A game imported with the wrong scoring mode is stuck.
- `Rating`, `Weight`, `SoldPrice` are on `UpdateGameCommand` (`CreateGameCommand.cs:27-33`) and `GameDto`, but no form field exists (grep for `soldPrice` in `src` → 0 hits). "Previously owned" games can never record what they sold for.
- Min/max player count and play time are silently dropped unless **both** are filled (`BaseGame.UpdatePlayerCount/UpdatePlayTime`, `BoardGameTracker.Common/Entities/Helpers/BaseGame.cs:113-135`), while the form accepts either alone (`GameFormPlayerFields.tsx:109-118`).

### A14. Locations page "Count" column is always blank and the sessions warning on delete can never appear
- Frontend `Location.playCount` (`models/Location/Location.ts:1-5`) is rendered at `routes/locations/index.tsx:51-55` and used for the delete warning (lines 138-143), but `LocationDto` only carries `Id`/`Name` (`BoardGameTracker.Common/DTOs/LocationDto.cs:3-7`, `LocationDtoExtensions.cs:8-20`) although `Location.GetPlayCount()` exists (`Location.cs:26`).

### A15. Score is compulsory and non-negative for scoring games; 0 pollutes every average
- `CreatePlayerSessionSchema` (`models/Session/CreateSession.ts:11-19`) requires a `nonnegative` number; the add-player modal defaults it to `0` (`routes/sessions/-modals/CreateSessionPlayerModal.tsx:43-49`). You cannot log "score unknown" or a negative score; unknown scores enter `GetAverageScore`/`GetLowestScoringPlayer` as 0. Backend `PlayerSession.Score` is `double?` and would accept null.

### A16. Day-of-week statistics and the Saturday badge are computed in UTC, not the user's timezone
- `GameStatisticsRepository.GetPlayByDayChart` (`156-163`) and `SessionRepository.GetSessionsByDayOfWeek` (`105-111`) group by `Start.DayOfWeek` on a `timestamptz` column; `ConsistentScheduleBadgeEvaluator.cs:12-17` does the same in C#. Everything is stored as UTC (`UtcDateTimeValueConverters.cs`, `UtcDateTimeConverter`). A Friday 21:00 session in UTC-5 is counted on Saturday; `TZ` only affects the app container's clock. **Confidence: high for the C# evaluator; DB `date_part` depends on the Postgres session timezone (UTC in the stock image).**

### A17. Several badge rules cannot fire as described or fire for the wrong reason
- "Almost There!" (close loss): `CloseLossBadgeEvaluator.IsCloseLoss` (`48-66`) also returns true when you *beat the lowest scorer* by ≤2 while losing to everyone else (`closeLossToLowestScorer`).
- "Rookie Hours — Play for 5 hours": `DurationBadgeEvaluator.cs:12-15` sums only **won** sessions.
- "Monthly Goal": `MonthlyGoalBadgeEvaluator.cs:20` uses `UtcNow.AddMonths(-1)`, not the session date, so back-filling history never qualifies.
- `BadgeEvaluatorConstants.ConsistentWeeksRequired = 4` is unused; the evaluator hard-codes 10 (`ConsistentScheduleBadgeEvaluator.cs:20`).

### A18. Raw translation keys are shown for lockout / image-upload errors in production
- `public/locales/base/error.json` has 26 keys; `en-US`, `es-ES`, `nl-BE`, `nl-NL` have 21. Missing: `account-locked-out`, `invalid-auth-session`, `invalid-redirect-uri`, `too-large`, `unsupported-format`. Production loads `/locales/{{lng}}/…` (`utils/i18n.ts:35-38`), so a locked-out user sees `error.auth.account-locked-out`; a too-large avatar shows `error.image.too-large`.

### A19. "Require authentication for RSVPs" is enforced only in the SPA; the RSVP API is anonymous and id-guessable
- `GameNightController.UpdateRsvp` is `[AllowAnonymous]` (`GameNightController.cs:70-83`) and accepts either an `Id` (small integer) or `(GameNightId, PlayerId)`; `GameNightService.UpdateRsvp` (`130-152`) never checks the `LinkId`. `RsvpAuthenticationEnabled` is read only by `routes/_bare/rsvp.tsx:39`; grep shows no backend consumer besides settings get/set. The toggle is cosmetic and any RSVP can be flipped by POSTing integers.
- **Fix:** require `linkId` in the anonymous command and validate `rsvp.GameNight.LinkId == linkId`; enforce the setting server-side.

### A20. Loans: an overdue loan is treated as "ended" for the double-loan check, and delete has no confirmation
- `Loan.IsActiveOn` (`Loan.cs:42-51`) uses `ReturnedDate ?? DueDate` as the end → once the due date passes, `Game.LoanToPlayer` (`Game.cs:52-62`) allows a second concurrent loan, while `IsCurrentlyOnLoan` (`36-40`) still flags the game as loaned.
- `LoanCard` delete button (`routes/loans/-components/LoanCard.tsx:151-159`) calls `actions.handleDelete` → `deleteLoan` immediately (`routes/loans/-hooks/useLoanActions.ts:12-18`); `useLoanModals().deleteModal` exists but is never shown. Every other entity gets a `BgtDeleteModal`.
- `PUT api/loans` (edit dates) has a service wrapper `updateLoanCall` (`services/loanService.ts:24-28`) but no UI; due dates cannot be extended.

### A21. Settings overridden by environment variables look editable and silently do nothing when saved
- `SettingsService.ResolveValue` (`SettingsService.cs:172-189`) and `ConfigRepository.GetConfigValueAsync` (`ConfigRepository.cs:20-33`) prefer `ENV[KEY.ToUpper()]` for **every** config key (currency, date/time format, shelf months, public URL, game-nights toggle…). `UpdateSettingsAsync` (`58-99`) writes the DB anyway. Only the BGG key surfaces `IsReadOnly` (`125-146`); every other env-overridden setting shows the env value, accepts an edit, toasts "saved", and keeps the env value.

### A22. Known mobile gap, confirmed: the game table hides everything but title/shop/BGG on phones
- `routes/games/table.tsx:136-210, 263` mark players, time, weight, rating, language, state, added, price and current-price `hideOnMobile`; `BgtDataTable` simply applies `hidden md:table-cell` (`components/BgtTable/BgtDataTable.tsx:169-178, 220-228`) with no card fallback. Live prices are invisible on mobile apart from the icon tooltip.

### A23. Smaller misleading bits
- Score `0` is hidden because of truthiness checks: `routes/players/-components/PlayerSessionCardItem.tsx:51`, `routes/games/-components/TopPlayersCard.tsx:70`.
- Dashboard "Recent activity" draws a trophy for every session, winner or not (`routes/-components/dashboard/RecentActivity.tsx:71-73`); `RecentActivity.winnerName` is typed non-null while the DTO is nullable.
- Session tables sort "high score" with `b.score! - a.score!` after filtering `!== undefined` (API sends `null`) → NaN comparisons for mixed rows (`routes/games/$gameId_.sessions.tsx:91-99`, `routes/players/$playerId_.sessions.tsx:110-119`).
- Player-sessions page shows edit/delete for Readers (no `canWrite` gate at `routes/players/$playerId_.sessions.tsx:122-131`; the game-sessions page gates it at `$gameId_.sessions.tsx:101-116`).
- BGG single add returns the *existing* game when the BGG id is already in the collection (`BggImportService.cs:45-49`) and the UI toasts "Game is successfully created" (`routes/games/-hooks/useBggGameModal.ts:22-33`); a not-found id yields a bare 400 → generic "Game creation failed" (`GameController.cs:90-93`).
- `GameChartService.GetPlayerScoringChart` (`72-116`) is computed on every statistics call, uses `TryAdd(session.Start)` (two sessions with the same start collapse), and is **never rendered** — no component consumes `playerScoringChart`.
- Compare page "Closest game" credits `playerTwo` whenever `playerOne` did not win, even if a third player won (`CompareRepository.cs:172-178`).

---

## (b) Half-finished features

| Feature | What exists | What is missing to finish | Effort |
|---|---|---|---|
| **OIDC login** | Full backend flow (`OidcService.cs`, PKCE, discovery, linking), admin CRUD `api/admin/oidc-providers`, login button (`routes/_bare/login.tsx:60-66`), `auth-callback.tsx`. | Mis-wired end-to-end: the login button navigates the browser to `GET …/oidc/{p}/login` which returns JSON (`OidcController.cs:38-45`), and `Callback` returns JSON tokens (`47-54`) while `auth-callback.tsx:28-55` expects them in the URL. No provider-config UI; `OIDC_PLAN.md` documents this as "mis-wired and non-functional". Also: once a provider is enabled, local user creation is blocked (`AuthService.RegisterAsync`, lines 135-139) with no UI to disable the provider. | Per `OIDC_PLAN.md` (multi-phase) |
| **Price tracking (changedetection.io)** | Settings UI, manual watch-id on the game form, live fetch with 30-min cache (`ChangeDetectionClient.cs:15-16`), wanted-games table column + refresh, header icon. | No history, no poller/alerts, watch must be created by hand in changedetection.io, only "Wanted" games are batch-fetched (`WantedGamesWithWatchIdSpec.cs:12`). Known roadmap — not repeated here. | Medium |
| **Session photos** | `Image` entity with `GamePlayId`, `Session.ExtraImages`, cascade config (`MainDbContext.cs:227-230`). | Nothing creates an `Image` (grep `new Image(` → only DbContext), `SessionDto` has no images, `UploadFileType` has only `Profile`/`Game`. | Low-medium (upload type + DTO + gallery) |
| **Game accessories** | `GameAccessory` entity/DbSet, eagerly included on every game load (`GameByIdWithDetailsSpec.cs:12`, `…ForReadSpec.cs:12`). | No DTO field, no endpoint, no UI. Dead weight on the hot path. | Low |
| **Manual expansions** | Expansions are BGG-only: `SearchExpansionsForGame`/`UpdateGameExpansions` require the API key (`GameService.cs:158-159, 182-183`); dialog hides "Add" unless BGG is configured (`routes/games/$gameId.tsx:119`). Expansion = title + bggId only (`Expansion.cs`). | Manual creation, image/price/state per expansion, per-expansion play stats (plays only increment the base game via `Session.GameId`). | Medium |
| **Update checker** | Background check, `POST api/update/check`, `UpdateStatusDto` with `LastChecked`/`ErrorMessage`. | UI shows only the "new version" pill (`routes/-components/VersionCard.tsx`); no "check now" button, no last-checked/error display. `POST api/update/check` has no caller. | Low |
| **Email** | Three mails: game-night invite (`GameNightService.cs:200-243`), RSVP notice to host (`154-198`), password reset (`AuthService.cs:284-319`). | No "send test email" in settings; hard-coded English bodies (`GameNightService.cs:178-186`); invite lacks date/location (`217-218`); silently skips players without email and only reports a count; no overdue-loan or "you were invited" reminders. | Low-medium |
| **Game nights** | CRUD, RSVP link page, manuals for invitees, host notification. | No calendar/iCal, no link from a past game night to the sessions played, no "log session from this night", `GameNightStatistics` (`models/GameNight/GameNight.ts:54-58`) and `gamenight/statistics` call have no backend (`services/gameNightService.ts:19-23`). | Low-medium |
| **Rulebook chat (RAG)** | Upload/index/reindex, page images, citations, per-game/per-manual scope. | Transcripts live in component state only (`routes/chat/-hooks/useRagChat.ts:28`) — lost on navigation; no streaming; 120 s client timeout (`services/ragService.ts:8`); no per-user/day budget for hosted providers (only `TopK`, `ConfigDefaults.cs:356`); AI settings are env-only and invisible in the UI. Degrades gracefully: `RagController` 404s when disabled, `ManualsDialog` hides badges. | Medium |
| **User ↔ player linking** | Users can link a Player; dashboard "welcome back" uses it (`routes/index.tsx:82-89`). | Data is global, not per-user; "my games"/"my sessions" filters absent; Reader role is view-only everywhere except the ungated player-sessions actions (A23). | — |
| **BGG integration** | Single import, collection import (25 at a time, `routes/games/import/list_.$username.tsx:40`), expansions lookup. | One-way only: no re-sync of rating/weight/image, no plays sync (client wraps `GetPlaysAsync`, `LazyBoardGameGeekClient.cs:43-44`, unused). README still says BGG "is under review". | Medium |
| **Import/export/backup** | Danger-zone reset/factory reset only (`MaintenanceController.cs`). | No export of any kind (grep `csv|backup` in Api/Core → none), no JSON import, no BG Stats import. | Low-medium |
| **Sessions model** | Per-player `Score/Won/FirstPlay`; multiple winners allowed (ties/co-op implicitly). | No team play, no explicit co-op outcome, no "incomplete/abandoned", no ranks/placements, no notes per player, no duration timer. | Medium |
| **Lists without paging** | Games grid/table, sessions per game/player, loans, game nights all load everything (`GamesOverviewSpec.cs:10-15`, `getGameSessions(gameId)` with no `count` at `routes/games/$gameId_.sessions.tsx:28`). | Server paging/sorting once collections pass a few thousand sessions. | Medium |

---

## (c) Inconsistencies between similar features

1. **Delete semantics differ per entity:** game → cascades sessions + deletes files (`GameService.cs:68-81`); player → deletes shared sessions (A1); location → sessions set to null but game nights cascade (A2); session → badges kept; loan → no confirm (A20); game night → confirm + 404 check (`GameNightController.cs:55-68`); player → no controller 404 check (`PlayerController.cs:58-65`, relies on exception).
2. **Create responses:** loans return `201 CreatedAtAction` (`LoansController.cs:41-47`); every other create returns `200 Ok`.
3. **Player CRUD vs location CRUD:** players have both a page (`/players/new`) and a modal (`CreatePlayerModal`), plus inline creation from session/loan forms; locations are modal-only and cannot be created inline from the session form (A12). Games are edited on a page, players in a modal.
4. **Session defaults:** `/sessions/new/:gameId` presets minutes = `maxPlayTime` and start = now − maxPlayTime (`routes/sessions/new_.$gameId.tsx:42-46`); `/sessions/new` presets 30 min / now − 30 (`routes/sessions/new.tsx:52-58`).
5. **Percent conventions:** compare `winPercentage` is a 0–1 fraction (`CompareService.cs:33-34`), `MostPlayedGame.WinningPercentage` is 0–100 (`PlayerRepository.cs:64-66`), the player grid recomputes with `GetPercentage` (`routes/players/-components/PlayerStatisticsGrid.tsx:34-38`).
6. **Currency/number formatting:** raw string concatenation with inconsistent spacing — `BgtTextStatistic.tsx:68-70` (`prefix&nbsp;value`), `table.tsx:268` (`${currency}${value}`), `ShameGame.tsx:85` (`{currency} {price}`), `RecentAddedGames.tsx:56-57`. `toLocaleString()` uses the browser locale, never `uiLanguage`.
7. **Date formatting:** `format(date, settings.dateFormat)` without locale in tables (`$gameId_.sessions.tsx:53`, `GameNightCard.tsx:54`) vs `toDisplay(...)` with locale elsewhere; `AccountSettings.tsx:280` uses `toLocaleDateString()` ignoring the configured format.
8. **Reader-role gating:** hidden on game sessions page, visible on player sessions page (A23); `POST api/update/check` and `GET api/settings/environment` are `[Authorize]` only (any role).
9. **API naming:** `api/gamenight` (singular) vs `api/loans` (plural) vs `api/game`/`api/player`.
10. **Empty states:** most cards use `BgtNoData`, but `RecentAddedGamesCard` (`routes/-components/dashboard/RecentAddedGames.tsx:19-27`), `MostPlayedGamesCard` (player) and `TopPlayersCard` (game) render nothing when empty.
11. **BGG duplicate handling:** collection import silently skips existing games (`BggImportService.cs:130-137`); single add silently *returns* the existing game and the UI says "created" (A23).

---

## (d) Dead endpoints / orphaned UI / stale code

**Backend endpoints with no UI consumer**
- `GET/DELETE api/auth/external-logins[/{id}]` (`AuthController.cs:130-144`)
- `GET/POST/PUT/DELETE api/admin/oidc-providers` (all five, `Admin/OidcProvidersController.cs`)
- `GET api/auth/oidc/{provider}/link` and `/link-callback` (`OidcController.cs:56-73`)
- `GET api/admin/users/{id}` (`Admin/UsersController.cs:31-36`); `PUT api/admin/users/{id}/role` — wrapper `updateUserRoleCall` exists (`services/authService.ts:98-100`), no caller found (Edit modal uses `PUT {id}`)
- `POST api/update/check` (`UpdateController.cs:20-26`)
- `GET api/loans/{id}` (only referenced as `CreatedAtAction` target), `PUT api/loans` (wrapper `updateLoanCall` unused)

**Frontend calling a route that does not exist**
- `GET gamenight/statistics` (`services/gameNightService.ts:19-23`, `services/queries/gameNights.ts:8`); its query key is invalidated after every game-night mutation (`routes/game-nights/-hooks/useGameNightData.ts:30-35`).

**Frontend model fields with no backend counterpart**
- `Game.type`, `Game.baseGameId`, `Game.baseGame` (`models/Games/Game.ts:23-27`, `GameType.ts`); `PlayerSession.isBot` (`models/Session/PlayerSession.ts:6`, i18n `player-session:bot.label`); `GameState.NotOwned` (A4); `GameNightStatistics`; `Location.playCount` (A14).

**Backend computed but never shown**
- `GameChartService.GetPlayerScoringChart` (`72-116`) → `GameStatisticsResponse.PlayerScoringChart`, no component renders it.
- `GameStatisticsRepository.GetHighScorePlay/GetLowestScorePlay` (`132-154`), `PlayerRepository.GetBestGame` (`38-47`), `GetWinCount` (`100-103`): no callers.
- `IBadgeProgressionService`/`BadgeProgressionService` registered (`ServiceCollectionExtensions.cs:130`) with no consumer.
- Value objects `GameScore`, `PlayerName`, `SessionTimeRange` (`BoardGameTracker.Common/ValueObjects/`) unused.
- Entities `Image` (session photos) and `GameAccessory` (see (b)).
- `BadgeEvaluatorConstants.ConsistentWeeksRequired`, `MonthlyGoalLookbackMonths` unused.

**Dead i18n keys (sample)**
- `player-session:new.location.create-new`, `player-session:bot.*`, `settings:titles.*`, `settings:time-zone.*`, `settings:environment.*`, `common:flags.*` (no `t("…")` callers found).

**Settings toggles with no effect**
- `rsvpAuthenticationEnabled` (frontend-only, A19); `SettingsSchema.statistics` is posted but `Statistics` is env-derived (`SettingsService.cs:39`) and not rendered.

---

## (e) Docs vs reality

| Where | Says | Reality |
|---|---|---|
| `README.md:65-110` compose sample | No `JWT_SECRET`, `postgres:16`, no `manuals` volume | App throws `JWT_SECRET not set` at startup when auth is enabled (`BoardGameTracker.Host/Program.cs:115-120`); migrations require the `vector` extension (`MainDbContext.cs:204-212`) → plain `postgres:16` fails; manuals are lost without the volume. Root `docker-compose.yml` and `docs/quick-start.mdx` are correct; `docs/docker.mdx:16-56` repeats the wrong image and omits the manuals volume. |
| `README.md:150-161` env table | `STATISTICS`, `DATE_FORMAT` default `yyyy-MM-dd` | Variable is `STATISTICS_ENABLED` (`EnvironmentProvider.cs:15-16`); default date format is `yy-MM-dd` (`ConfigDefaults.cs:337`). `JWT_SECRET`, `AUTH_ENABLED`, `ADMIN_PASSWORD`, SMTP, RAG, `BGG_API_KEY` absent from README. |
| `README.md:51` | "Integration with BGG is under review" | Implemented (API key in settings/env, import, expansions). |
| `README.md:188-199` | ".NET 8.0", "React 18" | `net10.0` (`BoardGameTracker.Host.csproj:4`); React 18 correct. |
| `docs/index.mdx:59-61` | "OIDC support — secure authentication with OpenID Connect" | Non-functional and no config UI (see (b)). |
| `docs/index.mdx:56-58` | "Locations — see which venues host the best game nights" | Locations page shows name only; play count never populated (A14); no per-venue stats. |
| `docs/index.mdx:52-55` | "Shelf of shame — games you own but have never played" | "not played in the last N months", including brand-new purchases (A10). |
| `docs/index.mdx:48-51` | "coordinate everyone's schedule" | No calendar/iCal/availability; RSVP only. |
| `docs/user-guide.mdx` | "Coming soon" | No user guide at all. |
| `docs/environment-variables.mdx` | Lists core/SMTP/RAG vars | Undocumented: `BGG_API_KEY`, `PORT`, and the fact that **any** setting key (`CURRENCY`, `DATE_FORMAT`, `TIME_FORMAT`, `UI_LANGUAGE`, `PUBLIC_URL`, `SHELF_OF_SHAME_*`, `GAME_NIGHTS_ENABLED`, `CHANGEDETECTION_*`, `UPDATE_*`) can be forced via env (A21). |
| Docs (none) | — | Undocumented features: changedetection.io price tracking, manual uploads outside RAG, Reader role, factory reset, beta update track, Swagger toggle, player↔user linking. |

---

## (f) Top-10 missing features (ranked by value ÷ effort)

1. **Export / backup / import (JSON + CSV)** — nothing exists today. Build on the existing DTO extensions (`*DtoExtensions.cs`) and `MaintenanceController`; add `GET api/export` (zip of JSON + `images/` + `manuals/`) and a BG Stats/CSV session importer via `SessionService.CreateFromCommand`. Highest value for a self-hoster (migration, disaster recovery); effort low-medium.
2. **Proper session outcome model** — optional score, negative scores, explicit co-op win/loss, teams, placements, "incomplete". Build on `PlayerSession` (`BoardGameTracker.Common/Entities/Helpers/PlayerSession.cs`) + `CreateSessionSchema`; fixes A8/A11/A15 at the root.
3. **Session photos & notes gallery** — `Image`/`ExtraImages`/cascade already modelled; add `UploadFileType.Session`, reuse `ImageService.SaveImage` (`ImageService.cs:77-137`), expose in `SessionDto`, show in session cards. Effort low.
4. **Yearly recap / H-index / plays-per-month** — all inputs exist in `SessionRepository`/`PlayerRepository`; add a `GET api/player/{id}/statistics?year=` and a chart on the dashboard (`BgtBarChart`). Popular BG Stats feature; effort low-medium.
5. **"What should we play?" recommender** — filters already exist (`routes/games/-components/GamesFilters.tsx`); add "not played in X months", "best at N players" (from `PlayerCountChart`), random pick, and push the result into `GameNight.SuggestedGames`. Effort low.
6. **iCal / calendar feed for game nights** — `GET api/gamenight/{linkId}/ics` from `GameNight.StartDate/Location/Title` (`GameNight.cs`); reuse `PublicUrlBuilder`. Effort very low, high perceived value.
7. **BGG re-sync + plays logging** — `LazyBoardGameGeekClient` already wraps `GetThingAsync`/`GetPlaysAsync`; add "refresh from BGG" on the game page (rating/weight/image) and optional push of sessions to BGG plays. Effort medium.
8. **API tokens for automation** (Home Assistant, scripts, mobile shortcuts) — `TokenService`/`RefreshToken` infra exists (`Auth/TokenService.cs`); add long-lived scoped PATs listed under Account settings, and publish the Swagger spec by default. Effort low-medium.
9. **Light theme + PWA offline shell** — dark is hard-coded (`index.html:2`, `main.tsx:14`); `site.webmanifest` exists but no service worker. Radix `Theme appearance` + a `prefers-color-scheme` switch; Vite PWA plugin for an installable shell. Effort low-medium.
10. **Public read-only shelf/profile link** — reuse the `LinkId` pattern from game nights (`GameNight.LinkId`, `ToPublicDto`) for a shareable collection/wishlist page (also gives the "wanted list" a purpose for gift-givers). Effort low-medium.

Also worth queuing: player merge (needed before fixing A1), manual expansions, overdue-loan reminders (email infra exists), sold-price/trade tracking UI (fields exist), and server-side paging for sessions.

---

## (g) Appendix — endpoint ↔ UI inventory

Auth column: `A` = `[Authorize]` any role, `U` = User/Admin, `Adm` = Admin, `anon` = `[AllowAnonymous]`. "UI" is the consuming route/component; **none** = no caller found.

| Verb | Route | Auth | UI consumer |
|---|---|---|---|
| POST | api/auth/login | anon | `_bare/login.tsx` |
| POST | api/auth/refresh | anon | `utils/axiosInstance.ts` |
| POST | api/auth/logout | A | `hooks/useAuth.ts` |
| POST | api/auth/register | Adm | `settings/-modals/CreateUserModal` |
| GET/PUT | api/auth/profile | A | `settings/-components/AccountSettings`, dashboard |
| GET | api/auth/linkable-players | A | AccountSettings |
| POST | api/auth/change-password | A | ChangePasswordModal |
| POST | api/auth/reset-password/{userId} | Adm | AccountSettings |
| POST | api/auth/forgot-password | anon | `_bare/forgot-password.tsx` |
| POST | api/auth/reset-password | anon | `_bare/reset-password.tsx` |
| GET | api/auth/status | anon | `hooks/useAuth.ts` |
| GET | api/auth/external-logins | A | **none** |
| DELETE | api/auth/external-logins/{id} | A | **none** |
| GET | api/auth/oidc/provider | anon | `_bare/login.tsx` |
| GET | api/auth/oidc/{p}/login | anon | `_bare/login.tsx` (browser navigation; returns JSON → broken) |
| GET | api/auth/oidc/{p}/callback | anon | **none** (`auth-callback.tsx` expects URL tokens) |
| GET | api/auth/oidc/{p}/link, /link-callback | A | **none** |
| GET/GET{id}/POST/PUT/DELETE | api/admin/oidc-providers | Adm | **none** |
| GET | api/admin/users | Adm | AccountSettings |
| GET | api/admin/users/{id} | Adm | **none** |
| PUT | api/admin/users/{id}/role | Adm | wrapper only (`authService.ts:98`) |
| PUT | api/admin/users/{id} | Adm | EditUserModal |
| DELETE | api/admin/users/{id} | Adm | AccountSettings |
| GET | api/badge | A | `players/$playerId.tsx` |
| GET | api/compare/{a}/{b} | A | `compare/index.tsx` |
| GET | api/count | A | Sidebar / BottomNav |
| GET | api/dashboard/statistics | A | `routes/index.tsx` |
| GET | api/game | A | games grid/table, session/loan/game-night forms, chat |
| POST | api/game | U | `games/new.tsx` |
| PUT | api/game | U | `games/$gameId_.update.tsx`, table inline edit |
| DELETE | api/game/{id} | U | `games/$gameId.tsx` |
| GET | api/game/{id} | A | game detail, session forms |
| POST | api/game/bgg/search | U | `games/bgg.tsx` |
| GET/POST | api/game/bgg/import | U | `games/import/list_.$username.tsx` |
| GET | api/game/{id}/price | A | game detail (`useGameData`) |
| GET | api/game/prices/wanted | A | `games/table.tsx` |
| GET | api/game/{id}/sessions | A | game detail (5) + `$gameId_.sessions.tsx` (all) |
| GET | api/game/{id}/expansions | A | ExpansionSelectorModal (model mismatch, A7) |
| POST | api/game/{id}/expansions | U | ExpansionSelectorModal |
| DELETE | api/game/{id}/expansion/{eid} | U | ExpansionsDialog |
| GET | api/game/{id}/statistics | A | game detail (`playerScoringChart` part unused) |
| GET | api/game/shames, /shames/statistics | A | `shames/index.tsx` |
| GET | api/gamenight | A | `game-nights/index.tsx` |
| POST/PUT | api/gamenight | U | Create/Edit modals |
| POST | api/gamenight/{id}/send-invites | U | GameNightActions |
| DELETE | api/gamenight/{id} | U | game-nights page |
| PUT | api/gamenight/rsvp | anon | ManageRSVPsModal, `_bare/rsvp.tsx` |
| GET | api/gamenight/link/{linkId} | anon | `_bare/rsvp.tsx` |
| — | api/gamenight/statistics | — | **called by client, does not exist** |
| POST | api/image | U | player/game forms |
| GET | api/loans | A | `loans/index.tsx` |
| GET | api/loans/{id} | A | **none** (Location header only) |
| POST | api/loans | U | NewLoanModal |
| PUT | api/loans | U | wrapper only (`loanService.ts:24`) |
| PUT | api/loans/return | U | LoanCard |
| DELETE | api/loans/{id} | U | LoanCard (no confirm) |
| GET/POST/PUT/DELETE | api/location | A/U | `locations/index.tsx` + forms |
| POST | api/maintenance/reset, /factory-reset | Adm | DangerZoneSection |
| GET/POST | api/manual/game/{gameId} | A/U | ManualsDialog, chat |
| POST | api/manual/{id}/reindex | U | ManualsDialog |
| DELETE | api/manual/{id} | U | ManualsDialog |
| GET | api/manual/{id}/download | A | ManualsDialog |
| GET | api/manual/{id}/page/{page}/image | A | chat `PageImage` |
| GET | api/manual/gamenight/{linkId}[/manual/{id}/download] | anon | `_bare` RsvpManuals |
| GET/POST/PUT | api/player | A/U | players pages, modals |
| GET/DELETE | api/player/{id} | A/U | `players/$playerId.tsx` |
| GET | api/player/{id}/statistics | A | player page, dashboard personal card |
| GET | api/player/{id}/sessions | A | player page (5) + `$playerId_.sessions.tsx` (all) |
| POST | api/rag/game/{gameId}/ask | A | `chat/index.tsx` |
| GET | api/session/{id} | A | `sessions/update_.$sessionId.tsx` |
| POST/PUT | api/session | U | SessionForm |
| DELETE | api/session/{id} | U | sessions tables |
| GET | api/settings | anon | everywhere |
| GET | api/settings/version-info | anon | Sidebar/BottomNav |
| PUT | api/settings | Adm | `settings/index.tsx` |
| GET | api/settings/environment | A | root (Sentry init), settings loader |
| GET | api/settings/languages | anon | GeneralSettings |
| POST | api/update/check | A | **none** |

**Frontend routes:** `/`, `/login`, `/forgot-password`, `/reset-password`, `/auth-callback`, `/rsvp` (bare); `/games`, `/games/table`, `/games/add`, `/games/bgg`, `/games/new`, `/games/import/start`, `/games/import/list/:username`, `/games/:id`, `/games/:id/sessions`, `/games/:id/update`; `/sessions/new`, `/sessions/new/:gameId`, `/sessions/update/:id`; `/players`, `/players/new`, `/players/:id`, `/players/:id/sessions`; `/locations`; `/loans`; `/game-nights` (toggle `gameNightsEnabled`); `/shames` (toggle `shelfOfShameEnabled`); `/compare`; `/chat` (env `RAG_ENABLED`); `/settings`. Feature reachability gated by settings: shelf of shame, game nights, RAG chat, BGG add/import/expansions (API key), live prices (changedetection config), forgot-password link (SMTP env).

---

## (h) Overall assessment

1. The core loop (collection → sessions → per-game/per-player stats) is solid and well-structured, but several statistics are quietly wrong at the edges: badge timing (A3), null-score ranking (A8), collection value (A9), shelf-of-shame semantics (A10), UTC weekdays (A16).
2. The most dangerous behaviours are destructive cascades a user would not expect: deleting a player wipes shared sessions (A1) and deleting a location/host wipes game nights (A2).
3. A handful of plain contract bugs block or degrade daily use: `NotOwned` state (A4), session-edit expansions (A6), the expansion picker payload (A7), the no-op language setting (A5), mandatory location (A12).
4. Roughly a fifth of the backend surface is unreachable from the UI (OIDC admin, external logins, update check, loan edit, player-scoring chart, accessories, session images), and the README/docs drift is large enough to break a first install (`JWT_SECRET`, pgvector image).
5. For a self-hoster the biggest gaps versus BG Stats-class tools are export/backup, a richer session model (co-op/teams/optional scores), session photos, yearly recaps/H-index, and calendar/iCal — most of which can be built on entities and services that already exist.
