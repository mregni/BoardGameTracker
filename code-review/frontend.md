# BoardGameTracker frontend — deep read-only review

Scope: `boardgametracker.client` (React 18, TS 5, Vite 8, TanStack Router/Query/Form/Table, react-aria-components, Radix, Tailwind v4, i18next, zod 4, zustand, axios, Sentry, Biome, Vitest). Branch `feature/236-change-detection` with uncommitted changes. All paths are relative to `boardgametracker.client/`.

Tooling baseline (run 2026-09-09):
- `pnpm typecheck` — clean (exit 0).
- `pnpm lint` — 140 errors, all `format` (CRLF/tabs, pre-existing); `biome lint src/` alone reports **1 real warning**: `noExplicitAny` at `src/routes/settings/-components/BggSettings.tsx:35`.
- `pnpm test:run` — 82 files / 1018 tests pass.
- `pnpm build` — OK; chunk-size warning. Largest outputs: `index-*.css` 759 kB, `localeUtils-*.js` 608 kB, `index-*.js` 319 kB, `BgtForm-*.js` 318 kB, `BgtIconButton-*.js` 269 kB (Radix Themes runtime), `BgtPieChart-*.js` 214 kB. `dist/assets` = 14 MB incl. **126 `.map` files** that the `Dockerfile:53` copies into `wwwroot`.

Excluded as known: refresh token in localStorage; `apiUrl.ts` bypassing the Vite proxy; OIDC flow being non-functional (I still list concrete callback bugs for when it is wired).

---

## Ranked findings

### FE-01 — Backend error keys are never translated; users see raw `error.auth.*` strings
- **Severity:** High · **Category:** Bugs / i18n · **Effort:** S · **Confidence:** High
- **Where:** `src/utils/axiosInstance.ts:57-58` (message = `data.reason`), `src/App.tsx:41` (`case "client": return error.message` → toast), `src/routes/settings/-modals/CreateUserModal.tsx:72`, `EditUserModal.tsx:70`, `ChangePasswordModal.tsx:45` (`t(e.message)` with ns `settings`), `src/routes/chat/-components/ChatMessage.tsx:30`.
- **Problem:** The backend emits reason codes as `error.auth.username-already-exists`, `error.loan.game-already-on-loan`, … (`../BoardGameTracker.Common/Constants.cs:45-64`, dot-separated). The frontend has these in `public/locales/base/error.json` under the `error` **namespace** (`error:auth.username-already-exists`). i18next's namespace separator is `:` and `.` is the key separator, so `t("error.auth.username-already-exists")` looks for `common.error.auth…` / `settings.error…` and returns the key verbatim. Every 4xx with a `reason` (duplicate username, loan conflict, OIDC restrictions, invalid reset token…) therefore surfaces as `error.auth.username-already-exists` in the toast and in the inline modal errors. Additionally 5 backend codes have no entry at all in `base/error.json`: `auth.player-already-linked`, `email.not-configured`, `auth.invalid-reset-token`, `loan.game-already-on-loan`, `changedetection.invalid-base-url`.
- **Fix:** In `classifyError` (or `getErrorToastMessage`) map `reason` → i18n key: `const key = reason.startsWith("error.") ? "error:" + reason.slice(6) : reason; message = i18n.exists(key) ? i18n.t(key) : i18n.t("error:something-went-wrong")`. Add the 5 missing keys to `base/error.json` (+ locales). Make the modals call the same helper instead of `t(e.message)`.

### FE-02 — `GameState.NotOwned` does not exist on the backend → 400 on save
- **Severity:** High · **Category:** Bugs / model drift · **Effort:** S · **Confidence:** High
- **Where:** `src/models/Games/GameState.ts:5`, `src/utils/ItemStateUtils.ts:15,39`; offered via `Object.values(GameState)` in `src/routes/games/bgg.tsx:125`, `src/routes/games/-components/GameFormPlayerFields.tsx:87`, `src/routes/games/table.tsx:90,105`, `src/routes/games/import/list_.$username.tsx:165`.
- **Problem:** Backend enum is `Wanted, Owned, PreviouslyOwned, ForTrade` (`../BoardGameTracker.Common/Enums/GameState.cs`) with `JsonStringEnumConverter(CamelCase)`. Selecting "Not owned" in the BGG-add form, the manual game form, the inline table editor or the BGG import sends `"notOwned"` → `JsonException` → 400 → "update failed" toast, and the optimistic table update is rolled back. Dashboard `GameStateChart` also maps it.
- **Fix:** Remove `NotOwned` from the enum and the two switch cases in `ItemStateUtils.ts` (and the `game:state.not-owned` key), or add it on the backend if it is intended.

### FE-03 — Date-only values are parsed as UTC midnight → off-by-one day west of UTC (and time picker date drift)
- **Severity:** High · **Category:** Bugs / date-timezone · **Effort:** M · **Confidence:** High
- **Where:** `src/components/BgtForm/BgtDateTimePicker.tsx:32` (`new Date("yyyy-MM-dd")` then `setHours` in local time), `src/models/Games/CreateGame.ts:17` and `src/models/Games/BggSearch.ts:23` (`z.coerce.date()` on the `"yyyy-MM-dd"` string produced by `BgtDatePicker.tsx:58`), `src/models/Loan/CreateLoan.ts:16,22` (`new Date(val)`), `src/routes/games/import/list_.$username.tsx:192`, `src/components/BgtForm/BgtInputField.tsx:41`, `src/components/BgtForm/BgtSimpleInputField.tsx:14`.
- **Problem:** Per ECMAScript, `new Date("2024-03-10")` is **UTC** midnight; it is then sent as `2024-03-10T00:00:00.000Z` and displayed with `format()`/`toDisplay()` in **local** time. For any user in a UTC-negative zone (Americas) the addition date, loan date, due date and BGG import date display as the previous day; in the datetime picker the *date* segment moves to the previous day when choosing a date in the calendar for users west of UTC. The author's own zone (CET) hides the bug.
- **Fix:** Parse date-only strings with `parse(value, "yyyy-MM-dd", new Date())` (date-fns) or `parseDate(value).toDate(getLocalTimeZone())` (`@internationalized/date`, already a dependency) before `setHours`/submission; in the zod schemas replace `z.coerce.date()` with a `z.string().transform(parseLocalDate)`. Better long-term: send date-only fields as `yyyy-MM-dd` strings and map them to `DateOnly` on the backend.

### FE-04 — ISO-date regex in the axios interceptor misses .NET fractional-second formats → many "Date" fields are strings at runtime
- **Severity:** Medium · **Category:** Bugs / date handling · **Effort:** S · **Confidence:** High
- **Where:** `src/utils/axiosInstance.ts:7` (`/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$/`).
- **Problem:** `System.Text.Json` writes `DateTime` with trailing-zero-trimmed fractions of 1–7 digits (`…:57.5Z`, `…:57.123456Z`); PostgreSQL stores microseconds, so every server-generated timestamp (`createdAt`, `lastLoginAt`, `uploadDate`, `indexedDate`, `fetchedAt`, `lastChecked`, BGG import `additionDate`, …) fails the 3-digit-only pattern and stays a string although the models declare `Date`. Concrete breakage: date-fns v4 `isValid(string)` is **false** (`node_modules/date-fns/isValid.js`), so `getDaysSincePurchase` (`src/utils/dateUtils.ts:181`) returns 0 and `ShameGame` shows "never" for games with a microsecond-precision `lastSessionDate`; `BgtSimpleInputField` renders an empty date input for string values; `GamePrice.fetchedAt`/`GameStats.lastPlayed`/`GameManual.indexedDate` are typed `string | null` or `Date | string` as a workaround rather than a fix.
- **Fix:** `(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})?$`, or drop the recursive walker and parse at the model boundary (`parseISO`) in the few places that need real `Date`s. Then remove the `Date | string` unions.

### FE-05 — Cache invalidation gaps after mutations (stale lists/stats for up to `staleTime` 5 min)
- **Severity:** High · **Category:** Bugs / React Query · **Effort:** M · **Confidence:** High
- **Where / what is missed:**
  - `src/routes/games/-hooks/useNewGame.ts:23-26` — manual game create invalidates `counts` + `dashboard` only; **`[games]`** (list/table/session selects) and `[shames]` are not invalidated. `useBggGameModal.ts:24-25` does invalidate `games`, so behaviour differs per creation path.
  - `src/routes/games/-hooks/useGameData.ts:52-53` (`deleteGame`) — no `dashboard`, `shames`, `loans`, `wantedPrices`.
  - `src/routes/games/-hooks/useGameSessionsData.ts:31-36` and `src/routes/players/-hooks/usePlayerSessionData.ts:33-38` (`deleteSession`) — invalidate `[game,id,sessions]` / `[player,id,sessions]` but not `[game,id,statistics]`, `[game,id]`, `[player,id,statistics]`, `dashboard`, `[sessions,id]`, `locations`. The game detail play-count/charts stay stale after deleting a session from the sessions page. A complete `QueryInvalidator.invalidateSession()` already exists (`src/services/queries/invalidations.ts:31`) but is used by only one of the three session mutation paths (`useNewSessionData`); `useNewSessionWithGameData.ts:28-39` and `useUpdateSessionData.ts:25-36` each hand-roll a different, smaller subset (none touch `[player,x,statistics]`, `players` badges, `dashboard`, `locations.playCount`).
  - `src/routes/loans/-hooks/useLoans.ts:23-25,35-37`, `useNewLoanModal.ts:26-28` — invalidate `[games]` but not `[game, gameId]`, so the game header keeps showing/hiding "on loan" for 5 min after loan/return.
  - `src/routes/games/-hooks/useInlineGameUpdate.ts:26-30` — optimistic list update but no `[game,id]`, `[wantedPrices]`, `dashboard` invalidation; `useUpdateGame.ts:24` never invalidates `[wantedPrices]` although `changeDetectionWatchId`/`state` drive that list.
- **Fix:** Route every mutation through `QueryInvalidator` and extend it: `invalidateGame` → also `shames`, `wantedPrices`; `invalidateSession` → also `shames`, `locations`, `[game,id,statistics]`; add `invalidateLoan(gameId)` that hits `[game, gameId]`. Consider a coarser strategy (`invalidateQueries({ predicate })` by domain) given the app's size.

### FE-06 — Bundle: all date-fns locales + Radix Themes CSS shipped for a handful of components
- **Severity:** High · **Category:** Performance · **Effort:** S–M · **Confidence:** High
- **Where:** `src/utils/localeUtils.ts:2` (`import * as locales from "date-fns/locale"` → 608 kB chunk for 6 used locales); `src/main.tsx:1` (`@radix-ui/themes/styles.css`, 813 kB source → 759 kB CSS asset) while `@radix-ui/themes` is only used for `Text` (6×), `TextArea` (2×), `Dialog` (2×), `Heading` (1×), `Theme`.
- **Problem:** ~1.3 MB of assets on first load that Tailwind already covers; `localeUtils` is pulled by `dateUtils`, i.e. on almost every route.
- **Fix:** `import { enUS, nl, fr, de, es, it } from "date-fns/locale"` and a static map; replace `Text`/`Heading`/`TextArea` with `BgtText`/`BgtHeading`/a plain `<textarea>`, and `Dialog` with `@radix-ui/react-dialog` (primitives are already a dependency) — then drop `@radix-ui/themes` and its stylesheet. Also consider `build.rollupOptions.output.manualChunks` for nivo.

### FE-07 — Token refresh desynchronises the zustand store; logout revokes the wrong refresh token
- **Severity:** Medium · **Category:** Auth/security · **Effort:** S · **Confidence:** High
- **Where:** `src/utils/axiosInstance.ts:155-162` (writes rotated tokens straight to `localStorage`), `src/hooks/useAuth.ts:55-57` (`logout` reads `refreshToken` from the in-memory store), `axiosInstance.ts:141-146` + `clearAuthState()` `:222-235`.
- **Problem:** zustand `persist` does not re-hydrate on external `localStorage` writes. After a silent refresh the store still holds the **old, revoked** refresh token; the backend rotates tokens (`../BoardGameTracker.Core/Auth/AuthService.cs:103-104`), so `logoutCall(oldToken)` fails/no-ops and the **new** refresh token stays valid server-side after "logout". In the no-refresh-token branch `clearAuthState()` clears storage but the store keeps `isAuthenticated: true`, so the root guard never redirects and every query 401s silently (401 toasts are suppressed).
- **Fix:** Have the interceptor call `useAuth.getState().setTokens(...)` / `clearAuth()` instead of touching `localStorage` directly (zustand stores are usable outside React; no circular import — `useAuth` already imports the service, so move the interceptor's auth access into a tiny `authStore` module). Also `queryClient.clear()` on logout (FE-38).

### FE-08 — Dialogs cannot be closed with Escape/outside click; close button has no accessible name
- **Severity:** Medium · **Category:** Accessibility/UX · **Effort:** S · **Confidence:** High
- **Where:** `src/components/BgtDialog/BgtDialog.tsx:19` (`<Dialog.Root open={open}>` without `onOpenChange`), `:31-37` (icon-only `<button>` without `type="button"`/`aria-label`). `BgtDeleteModal.tsx:24`, `ExpansionSelectorModal.tsx:34`, `ResetConfirmModal.tsx:31`, `CreateSessionPlayerModal.tsx:71`, `UpdateSessionPlayerModal.tsx:47` pass no `onClose` at all.
- **Problem:** Radix calls `onOpenChange(false)` on Escape/overlay click; nothing handles it, so the only way out is the Cancel button (keyboard users must tab to it). Screen readers announce the X as "button".
- **Fix:** `<Dialog.Root open={open} onOpenChange={(o) => { if (!o) onClose?.(); }}>`, `aria-label={t("common:close")}` and `type="button"` on the X; require `onClose` in the props.

### FE-09 — `BgtSelect`/`BgtSimpleSelect` swallow every later `window` resize listener
- **Severity:** Medium · **Category:** Bugs · **Effort:** S · **Confidence:** High
- **Where:** `src/components/BgtForm/BgtSelect.tsx:80-88`, `src/components/BgtForm/BgtSimpleSelect.tsx:61-69`.
- **Problem:** `window.addEventListener("resize", e => e.stopImmediatePropagation())` while the component is mounted prevents any resize listener registered *after* it (Radix Popover/Tooltip/Select positioning via floating-ui `autoUpdate`, other selects, charts' fallbacks) from firing. On the games table there are 2 selects per row, so this is always active.
- **Fix:** Delete the effect. If it was added to stop Radix Select closing on mobile keyboard resize, use `onPointerDownOutside`/`onInteractOutside` on `Select.Content` instead.

### FE-10 — Cleared number inputs are saved as `0` (known latent coercion, concrete impact)
- **Severity:** Medium · **Category:** Bugs / forms · **Effort:** S · **Confidence:** High
- **Where:** `src/components/BgtForm/BgtInputField.tsx:100` (`+event.target.value`); schemas `src/models/Games/CreateGame.ts:12,13,27-31`.
- **Problem:** React's controlled `type="number"` immediately re-renders `""`→`0` as "0" (so users cannot even clear the field) and the value is persisted: `bggId` becomes `0` (a real BGG id 0 stored; `yearPublished` alone has `value || null`), `buyingPrice` `0` (counted in collection value/average price instead of "unknown"), `minAge`, `minPlayers`, `maxPlayers`, `minPlayTime`, `maxPlayTime` `0` (shown as "0 - 4" / used by filters). In session forms `minutes` and `score` 0 are semantically fine.
- **Fix:** In `BgtInputField`: `const value = type === "number" ? (raw === "" ? undefined : Number(raw)) : raw`; keep the raw string in local state to allow intermediate input; make the game schema `z.number().int().positive().nullable().optional()` with `.transform(v => v ?? null)`.

### FE-11 — Session player modal accepts "no player selected" (`playerId` 0)
- **Severity:** Medium · **Category:** Bugs / validation · **Effort:** S · **Confidence:** High
- **Where:** `src/models/Session/CreateSession.ts:4` (`playerId: z.coerce.number()` — no `.positive()`), `src/routes/sessions/-modals/CreateSessionPlayerModal.tsx:45` (default `playerId: ""`).
- **Problem:** `Number("") === 0` passes coercion, so pressing Save without picking a player adds a player-session with `playerId: 0`; `BgtPlayerSelector` renders an empty avatar and the backend rejects/creates a broken row on submit.
- **Fix:** `.int().positive({ message: "player-session:new.player.required" })` (as `CreateLoanSchema` already does).

### FE-12 — Removing a game image on update is impossible
- **Severity:** Medium · **Category:** Bugs · **Effort:** S · **Confidence:** High
- **Where:** `src/routes/games/$gameId_.update.tsx:36` (`image: data.image ?? game.image`), `src/routes/games/-hooks/useImageUpload.ts:14-15` (returns `null` for "removed").
- **Problem:** The `null` that means "user removed the poster" is coalesced back to the old image.
- **Fix:** `image: data.image` (the schema already yields `string | null`), or return a discriminated result from `uploadPoster`.

### FE-13 — "UI language" setting never changes the UI language; `es-ES` locale is unreachable
- **Severity:** Medium · **Category:** Half-finished feature / i18n · **Effort:** S · **Confidence:** High
- **Where:** `src/utils/i18n.ts:42-45` (`LanguageDetector`, `supportedLngs: ["en-US","nl-NL","nl-BE"]`), `src/routes/settings/-components/GeneralSettings.tsx:22-34`, no `i18n.changeLanguage` anywhere (only the test mock).
- **Problem:** `settings.uiLanguage` is only used to pick a date-fns locale; i18next follows the browser detector. Changing the language in Settings has no visible effect on labels. `public/locales/es-ES/*` (complete, 26 files) is never served because it is not in `supportedLngs`; in DEV `loadPath` is hard-wired to `base`.
- **Fix:** After settings load/save call `i18n.changeLanguage(settings.uiLanguage)`; add `es-ES` to `supportedLngs`; set `document.documentElement.lang` on `languageChanged` (`index.html:2` is static `lang="en"`).

### FE-14 — i18n keys used but absent from `base`, plus hard-coded English strings
- **Severity:** Medium · **Category:** i18n · **Effort:** S · **Confidence:** High (verified programmatically against `public/locales/base/*.json`)
- **Missing keys (render as raw key):**
  - `settings:ui-language.required` — `src/models/Settings/Settings.ts:44` (key lives at `settings:general.ui-language.required`)
  - `game-nights:validation.title-required|location-required|host-required` — `src/routes/game-nights/-components/GameNightForm.tsx:13,17,18`
  - `game-nights:empty.upcoming|past|all`, `game-nights:create-first` — `src/routes/game-nights/-components/NoGameNights.tsx:17,19,21,34` (component is rendered from `game-nights/index.tsx:156`)
  - `loans:active` — `src/routes/loans/-components/LoanCard.tsx:113`
  - `settings:account.users.no-users` — `src/routes/settings/-components/AccountSettings.tsx:313`
  - `game:added-date.placeholder` — `GameFormPlayerFields.tsx:77`, `bgg.tsx:115` (prop is ignored by `BgtDatePicker` anyway)
  - `common:required` — 6 usages with an English default (`t("common:required", "Required")`) in `ChangePasswordModal.tsx:61,79`, `CreateUserModal.tsx:88,106,124`, `reset-password.tsx:78`
- **Hard-coded English:** `src/routes/settings/index.tsx:123` (`header={"Settings"}`), `BgtSelect.tsx:130` / `BgtSimpleSelect.tsx:119` / `MultiSelectField.tsx:136` (`"Search..."`), `MultiSelectField.tsx:157` (`"No results"`), `BgtBarChart.tsx:103` (`{value} sessions`), `SessionCardItem.tsx:49-52` (`pts`, `p`, `m`), `BgtDatePicker.tsx:76,93,113,126,138` (aria-labels), `BgtImageSelector.tsx:23` / `BgtAchievement.tsx:21,46` / `compare/index.tsx:120` (alt texts), `GeneralSettings.tsx:46,57,72` (placeholders — acceptable), `GameStaticSection.tsx:152` (`formatDuration` without `locale` → English units).
- **Fix:** Add the keys (and to the 4 locales), replace literals with `t()`; add a CI step running the key checker (script used for this review is trivial to keep).

### FE-15 — OIDC callback: tokens linger in history, base64url decode bug, unvalidated `redirect`
- **Severity:** Medium · **Category:** Auth/security · **Effort:** S · **Confidence:** High (flow itself is known non-functional)
- **Where:** `src/routes/_bare/auth-callback.tsx:37` (`atob(accessToken.split(".")[1])` — `atob` throws on base64url `-`/`_`, which appear whenever the payload contains `>`, `?`, `~` at certain offsets or non-ASCII display names), `:48` (`navigate({ to: redirect ?? "/" })` without `replace: true`, leaving `?accessToken=…&refreshToken=…` in browser history), `src/routes/_bare/login.tsx:53,62-65` and `axiosInstance.ts:184` (`redirect` is any string).
- **Fix:** decode with `atob(b64.replace(/-/g,"+").replace(/_/g,"/").padEnd(…,"="))` (or `jose`), `navigate({ …, replace: true })`, and accept only `redirect` values matching `/^\/(?!\/)/`. Prefer moving tokens out of the URL entirely (fragment or one-time code) when the OIDC plan lands.

### FE-16 — `TrackedPriceIcon` and other status indicators are colour/title-only; icon-only buttons lack names
- **Severity:** Medium · **Category:** Accessibility · **Effort:** S · **Confidence:** High
- **Where:** `src/routes/games/-components/TrackedPriceIcon.tsx:34-35` (green/red/amber `Target` inside a `<span title>`; no `role="img"`/`aria-label`/sr-only text, not focusable so no keyboard tooltip), `src/routes/games/table.tsx:262-264` (red `!` with `title`), `src/routes/games/-components/ManualsDialog.tsx:38` (status badge OK — has text). Systemic: 14 of 15 `<BgtIconButton>` usages have no `aria-label`/`title` (`BgtEditDeleteButtons`, `Sidebar.tsx:35` logout, `BottomNav.tsx:70`, `locations/index.tsx:62-76`, `ManualsDialog.tsx:119-125`, `BgtPlayerSelector.tsx:73-74`, …); `list_.$username.tsx:86-94` row checkboxes have `id=""`/`label=""` (duplicate empty ids, unlabeled controls); `BottomNav.tsx:120` "more" button has no `aria-expanded`.
- **Fix:** `TrackedPriceIcon`: `<span role="img" aria-label={t(key)} title={t(key)}>` plus a visually-hidden text, and use distinct icon shapes (check/cross/warning) rather than colour alone. Give `BgtIconButton` a required `aria-label` prop (type-enforce). Set `id={`import-${bggId}`}` and an sr-only label for row checkboxes. Re-enable Biome's `useButtonType`/`useKeyWithClickEvents` (currently off in `biome.json`).

### FE-17 — Backend features with no UI, and `Location.playCount` never exists
- **Severity:** Medium · **Category:** Half-finished / model drift · **Effort:** M · **Confidence:** High
- **No UI for existing endpoints** (compared against `../BoardGameTracker.Api/Controllers`): `GET/POST/PUT/DELETE api/admin/oidc-providers[/{id}]` (OIDC provider management), `GET/DELETE api/auth/external-logins[/{id}]` and `GET api/auth/oidc/{provider}/link` (link external login to account), `POST api/update/check` (manual update check — `VersionCard` is display-only), `GET api/loans/{id}`. Every endpoint the client calls does exist (incl. `api/admin/users*`).
- **Model drift with visible impact:** `src/models/Location/Location.ts:5` `playCount` — `LocationDto` has only `Id`/`Name`, so `src/routes/locations/index.tsx:53` always renders an empty "count" column and the delete warning at `:138-142` never appears. Other drift (latent): `Game.type`/`baseGame`/`baseGameId` (no DTO fields), `Environment.logLevel: number` (backend serialises `LogEventLevel` as a string; `ToLogLevel` is dead), `BadgeType.differentGames = "different_games"`, `cLoseLoss = "cLoseLoss"` vs backend `differentGames`/`closeLoss` (unused for comparisons today), `ProfileResponse.roles: string` vs `IEnumerable<string>`, `RecentActivity.winnerId/winnerName` non-nullable vs `int?/string?`, `Session.locationId: number` vs `int?` (update form would fail validation for a null location), `GameNightRsvps.gameNightId: string` vs `int`, `GameLink.id: string` vs `int`, `PlayerScoringChartData[]` vs `Dictionary<DateTime, XValue[]>` (unused).
- **Fix:** Either add `PlayCount` to `LocationDto` (backend) or drop the column; generate the TS models from the API (NSwag/`openapi-typescript`) to stop drift; decide whether the OIDC-provider/external-login admin screens are in scope for the OIDC plan.

### FE-18 — Games table mounts two Radix Selects + a number input per row for the entire collection
- **Severity:** Medium · **Category:** Performance · **Effort:** M · **Confidence:** High
- **Where:** `src/routes/games/table.tsx:167-210` (`EditableSelectCell` ×2, `EditableNumberCell` per row), `src/components/BgtTable/BgtDataTable.tsx` (no pagination/virtualisation; `rows` rebuilt every render).
- **Problem:** Each `BgtSimpleSelect` registers effects and a Portal; with a few hundred games this is hundreds of Radix roots, plus each select's global resize hook (FE-09). Filtering recomputes on every keystroke.
- **Fix:** Render read-only cells and swap in the editor on click/focus (single "editing cell" state), or paginate with `BgtPaging` (already exists) / `@tanstack/react-virtual`. Memoise `BgtDataTable`.

### FE-19 — Empty-state flashes before data arrives
- **Severity:** Low–Medium · **Category:** Bugs / UX · **Effort:** S · **Confidence:** High
- **Where:** `src/routes/sessions/new.tsx:37` (`games?.length === 0` — `useNewSessionData` exposes no `isLoading`), `src/routes/loans/index.tsx:45`, `src/routes/game-nights/index.tsx:105`, `src/routes/locations/index.tsx:87` (no loading state at all), `src/routes/compare/index.tsx:87`. `games/index.tsx:99` and `players/index.tsx:31` do it right.
- **Problem:** `data ?? []` is empty while loading, so on a cold load the "add your first X" page (and its create modal) renders for a moment, then swaps. Route loaders only `prefetchQuery` (not awaited), so `defaultPendingComponent` never shows either.
- **Fix:** Check `isLoading` first (or `await queryClient.ensureQueryData` in the loader so the router's pending component is used consistently).

### FE-20 — Misc. concrete bugs (Low, S each, High confidence)
- `src/routes/games/-modals/ExpansionSelectorModal.tsx:51` — `onClick={saveModal || isPending}` / `disabled={isLoading}`: pending state never disables the button → double submit.
- `src/routes/games/-components/GameStaticSection.tsx:152-160` — `formatDuration` yields `""` when in collection < 1 day (blank statistic) and ignores locale.
- `src/routes/players/-components/PlayerSessionCardItem.tsx:51` — `playerSession?.score &&` hides a score of 0; `src/routes/games/-components/SessionCardItem.tsx:49` renders " pts" for non-scoring games.
- `src/routes/games/import/list_.$username.tsx:137` — BGG-id column header reuses `t("name")`.
- `src/routes/settings/-components/AccountSettings.tsx:280` — `toLocaleDateString()` ignores the configured `dateFormat`.
- `src/routes/settings/-components/BggSettings.tsx:62` — `bggStatus.isConfigured = false` mutates the React-Query-cached settings object; `:35` `form: any`.
- `src/hooks/useAuth.ts:47-50` + `src/routes/_bare/login.tsx:54-55` — every login failure (network, 500, lock-out) is shown as "invalid credentials"; the `ApiError` kind is discarded.
- `src/utils/routeSchemas.ts:6` — throwing `TypeError` inside a zod transform bypasses zod's issue reporting (use `ctx.addIssue`).
- `src/services/queries/queryFactory.ts:11-17` + `src/services/gameService.ts:56` — `queryFn: fetchFn` passes the `QueryFunctionContext` as `refresh`, serialised as `?refresh[queryKey][0]=wantedPrices&refresh[signal]=…` (verified with axios `getUri`); harmless today because ASP.NET ignores it, but wrap: `queryFn: () => fetchFn()`. `createNestedQuery` (`:35-41`) also omits `params` from the key.
- `src/components/BgtForm/BgtImageSelector.tsx:93` — `URL.createObjectURL` on every render, never revoked; `:118` `<input type="file">` without `accept="image/*"`.
- `src/routes/game-nights/index.tsx:246` — `ManageRSVPsModal isLoading={isLoading}` uses page loading, not the RSVP mutation's pending state.
- `src/routes/games/new.tsx:21-30` — navigates twice (in `onSuccess` and after `saveGame`).
- `src/hooks/usePermissions.ts:6` — `authStatus === null` (status fetch failed, swallowed in `__root.tsx:48`) → treated as auth disabled → admin UI shown (backend still enforces).
- `src/utils/axiosInstance.ts:104-116` — the Bearer token is attached to **every** axios request incl. absolute URLs; `getManualPageImageCall(url)` takes a server-provided URL, so a mis-configured `imageUrl` would leak the token cross-origin. Attach only when `config.baseURL`-relative.
- `src/hooks/useAuth.ts:52-64` — logout does not `queryClient.clear()`; cached data (gcTime 1 h) is visible to the next user on a shared browser.

### FE-21 — Sourcemaps and secrets/exposure hygiene
- **Severity:** Low · **Category:** Security/tooling · **Effort:** S · **Confidence:** High
- **Where:** `vite.config.ts:41` (`build.sourcemap: true`), `sentryVitePlugin` without `sourcemaps.filesToDeleteAfterUpload`; `Dockerfile:53` copies `dist` wholesale → 126 `.map` files served from `wwwroot`. `src/utils/sentry.ts:15` `tracePropagationTargets: [/^\/api/]` never matches the absolute DEV base URL. `src/index.css:1` imports Google Fonts at runtime (external call from a self-hosted app). `.env.sentry-build-plugin` is present locally (git-ignored — OK) and `VITE_SENTRY_DSN` is not defined in any tracked env file, so `Sentry.init({ dsn: undefined })` is a silent no-op in production builds unless injected at build time.
- **Fix:** `sourcemaps: { filesToDeleteAfterUpload: ["./dist/**/*.map"] }` (or `sourcemap: "hidden"`), self-host the font, document the DSN build arg.

### FE-22 — Dead code / unused exports (Low, S)
Files never imported: `src/components/BgtCard/BgtMostWinnerCard.tsx`, `src/components/BgtIcon/BgtIcon.tsx`, `src/hooks/useInfiniteScroll.ts`, `src/hooks/useMultiQuery.ts`, `src/routes/-hooks/useElementSize.ts`, `src/routes/-components/dashboard/SessionsByDay.tsx`, `src/routes/compare/-components/CompareEmptyState.tsx`, `src/routes/games/-modals/CreateGameModal.tsx` (superseded by `/games/add`), `src/routes/players/-types/playerTypes.ts` (defines a second, conflicting `PlayerStatistics`). Unused exports: `updateUserRoleCall`, `updateLoanCall`, `getGameNightStatistics`, `useAuth.fetchOidcProvider`, `ToLogLevel`, `createNumericParamConfig`, `toInputDateTime/toDisplayDateTime/isValidDate/safeParseDate` (tests only), `Environment.logLevel`, `BggUserName`, `MenuItems`. ~190 base locale keys are not referenced anywhere (mostly `badges:*` used via dynamic `badge.titleKey` — fine — but also e.g. `auth:session-expired`, `common:items`).

### FE-23 — Architecture / consistency notes (Low, informational)
- The `routes/<area>/-components|-hooks|-modals|-utils` convention is followed consistently; services + typed `queries/*` factories are used everywhere (good). Two form-library entry points coexist: `useAppForm`/`withForm` (games, sessions, settings) vs raw `useForm` (players, account modals) with hand-written `safeParse` validators duplicating `zodValidator`.
- Three modal patterns: `BgtDialog` (Radix Themes), `MultiSelectField` (hand-rolled portal + click-outside), `SourcesOverlay`; two date pickers (`BgtDatePicker` react-aria vs `BgtInputField type="date"` / `BgtSimpleInputField type="date"`); two switches, two checkboxes, two selects (field vs simple) — reasonable but `BgtSimpleSwitch` is uncontrolled (`defaultChecked`) and `BgtCheckboxList` copies `selectedIds` into state once, so they ignore later prop changes.
- Loaders are fire-and-forget `prefetchQuery` (49 call sites) → every page implements its own loading UI; `useSuspenseQuery` is used once (`useUpdateSessionData`). Pick one model.
- `useMenuInfo` recomputes `menuItems` each render; `Sidebar`/`BottomNav`/`__root` subscribe to the whole auth store (`useAuth()` without selector).
- 17 `memo` components but callbacks passed to them are frequently unstable (`renderSession` is memoised, most `onClick` lambdas are not); `useMultiQuery`'s `useMemo(..., [results])` is a no-op because `useQueries` returns a new array each render.
- `models/index.ts` barrel re-exports everything, creating import cycles (`GameStatistics.ts` imports `Player` from `".."`).

### FE-24 — Tooling (Low)
- `package.json`: `build` does not type-check (`build2` does); CI runs `pnpm typecheck` separately so this is fine, but rename/remove `build2`.
- `tsconfig.json`: `strict` on, but `noUncheckedIndexedAccess`/`exactOptionalPropertyTypes` off; `tsconfig.node.json` has `strict: false`.
- `biome.json`: a11y rules `useKeyWithClickEvents`, `useButtonType`, `noStaticElementInteractions` and `noNonNullAssertion` disabled; `useExhaustiveDependencies` only warns. Formatter is `indentStyle: tab` while most files use tabs but ~140 files fail on CRLF — add `.gitattributes` `* text=auto eol=lf` and run `pnpm lint:fix` once.
- `vitest.config.ts` duplicates the Vite config instead of `mergeConfig(viteConfig, …)`; coverage `reporter: ["lcov"]` only, no thresholds; no `msw` — hooks/services are tested by mocking modules, so the axios interceptor (date conversion, refresh queue) has no tests at all (`axiosInstance.ts` has no `.test`).
- `react-query.d.ts` registers `defaultError: ApiError` — good; but `useAuth.login` and `useToasts` bypass it.

---

## Quick wins (≤ 30 min each)
1. Map `error.<x>` → `error:<x>` in `classifyError`/`getErrorToastMessage`; add the 5 missing `error.json` keys (FE-01).
2. Delete `GameState.NotOwned` + its two switch cases + `game:state.not-owned` (FE-02).
3. Widen the ISO regex to `(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})?$` (FE-04).
4. `useNewGame`: add `invalidateQueries([games])` and `[shames]`; loans hooks: add `[game, gameId]` (FE-05 partial).
5. Replace `import * as locales` with named imports in `localeUtils.ts` (−~550 kB) (FE-06 partial).
6. Wire `onOpenChange` in `BgtDialog`, add `aria-label`/`type="button"` to the X (FE-08).
7. Remove the two `resize` `stopImmediatePropagation` effects (FE-09).
8. `playerId: z.coerce.number().int().positive(...)` in `CreatePlayerSessionNoScoringSchema` (FE-11).
9. `image: data.image` in `$gameId_.update.tsx` (FE-12).
10. Fix `settings:ui-language.required` key path; add `game-nights:validation.*`, `game-nights:empty.*`, `create-first`, `loans:active`, `account.users.no-users`; replace `"Settings"` literal (FE-14).
11. `navigate({ to: redirect ?? "/", replace: true })` + base64url-safe decode in `auth-callback.tsx` (FE-15).
12. `ExpansionSelectorModal` button: `disabled={isLoading || isPending} onClick={saveModal}` (FE-20).
13. `queryFn: () => fetchFn()` in `createListQuery`/`createSingletonQuery` (FE-20).
14. Add `sourcemaps.filesToDeleteAfterUpload` to the Sentry plugin (FE-21).
15. Delete the 9 never-imported files (FE-22).
16. `.gitattributes` + `pnpm lint:fix` to clear the 140 CRLF lint errors (FE-24).

---

## Locale / i18n sync table
Base namespaces: 26 (`auth, badges, bgg-import, chat, common, compare, dashboard, error, expansions, game, game-nights, games, images, language, loans, location, log-levels, not-found, player, player-session, rsvp, sessions, settings, shames, statistics, version`). Keys compared flat (nested `a.b.c`), plural suffixes tolerated.

| Namespace | en-US missing/extra | es-ES missing/extra | nl-BE missing/extra | nl-NL missing/extra |
|---|---|---|---|---|
| error | **5 / 0** | **5 / 0** | **5 / 0** | **5 / 0** |
| all 25 others | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |

- The 5 keys missing from every locale: `image.too-large`, `image.unsupported-format`, `auth.account-locked-out`, `auth.invalid-redirect-uri`, `auth.invalid-auth-session` (present only in `base/error.json`).
- Keys the **backend** emits that are missing from `base/error.json` too: `auth.player-already-linked`, `email.not-configured`, `auth.invalid-reset-token`, `loan.game-already-on-loan`, `changedetection.invalid-base-url`.
- Keys used in code but missing from base: 13 distinct (see FE-14). Namespaces referenced in code are all registered in `i18n.ts`.
- `es-ES` is complete but not in `supportedLngs`; DEV always loads `base`.
- Locale files are otherwise perfectly in sync (likely Crowdin-managed), so the process gap is only for keys added on this branch.

---

## Missing packages / patterns (only where there is a concrete pain point)
- **API-driven models** (`openapi-typescript` / NSwag against the ASP.NET Swagger doc): would have caught FE-02, FE-17 (`playCount`, `NotOwned`, enum value drift) and the `Date | string` unions.
- **`msw`** for the axios layer: `axiosInstance.ts` (date conversion, refresh queue, error classification) is untested; FE-04/FE-07 are exactly the kind of regressions an msw-backed test catches.
- **`@tanstack/eslint-plugin-query` equivalent**: Biome has no query-key/exhaustive-deps rule for React Query; a tiny custom check (or keep the `QueryInvalidator` as the only allowed API and lint for direct `invalidateQueries` outside it) would prevent FE-05 recurring.
- **i18n key checker in CI** (`i18next-parser --fail-on-warnings` or the script used here) — FE-14 shows keys drift on feature branches.
- **`@tanstack/react-virtual`** or pagination for the games table (FE-18).
- **`jose`** (or a 10-line base64url helper) for JWT decoding (FE-15).
- Not needed: a state library change — zustand is only used for auth, which is appropriate; everything else is correctly in React Query.

---

## Overall assessment
1. Solid skeleton: typed services + query factories, consistent route folder convention, zero `any` outside one file, clean typecheck, 1018 green tests — the codebase is well above average for a hobby-scale app.
2. The highest-impact defects are integration seams with the backend: untranslated error codes (FE-01), the phantom `NotOwned` state (FE-02), and date/timezone handling that only works from a UTC+ timezone (FE-03/04).
3. React Query is used well for reads but mutation invalidation is ad hoc; `QueryInvalidator` exists and should become the single path (FE-05).
4. ~1.3 MB of easily removable payload (all date-fns locales + Radix Themes CSS) and shipped sourcemaps are the main perf/hygiene items (FE-06, FE-21).
5. Accessibility is the weakest area (dialogs, icon-only buttons, colour-only price status) and the a11y lint rules are switched off — re-enabling them plus a required `aria-label` on `BgtIconButton` would fix most of it mechanically.
