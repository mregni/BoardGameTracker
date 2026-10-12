# UI scan round 2 — dev-games.nobelenoedel.be v0.2.514 (2026-10-10)

Regression check of the round-1 fixes (commits e94ad1f4…a92e44cc on master) plus a new bug hunt.
Same rules as round 1: own `UI-SCAN` test data only, settings reverted, no BGG/external imports.

Dev state at start: version 0.2.514, Public URL https://dev-games.nobelenoedel.be, date format setting `yy-MM-dd` (stored value from before; the new default only applies to fresh installs).

## Regression check (round-1 IDs)

| ID | Result | Evidence |
|----|--------|----------|
| D3 | ✅ | API sessionsByDayOfWeek Monday…Sunday, wednesday 0 |
| D2 | ✅ (API) | pricedGameCount = 2 |
| L1 | ✅ (API) | minimumPlayersForPodium = 4 |
| ST1/GN6 | ✅ config | Public URL now https |
| D1/D7/ST6 | ✅ | "Welcome back, Administrator!" / "As reggi, you've played 10 of 12 sessions"; sidebar shows Administrator |
| D8 | ✅ | New game 55 additionDate = exact creation time (21:18:20Z). gresrg/dfgh (midnight) were created before the deploy |
| G1/GD1/GD6/AV1 | ✅ | 0 broken images on /games; Carcassonne detail shows centred letter tile |
| G2/G3 | ✅ | ?q=zzzz → empty state + Clear filters; typing "car" writes ?q=car |
| G5/G6/G7/G8/G11 | ✅ | Table keeps q, filters Any, "Total price (owned) €150", "Mean price (owned) · 3 priced games €50", Dino Path weight "-", 53/53 titles are links |
| GD2/GD3/GD4/GD5/GD8/GD10 | ✅ | Show more; "about 10 hours ago"; y ticks 0,1,2; top players only winners; year/min age/rating/weight rows |
| GF1/GF2/GF3/GF6/GD9/S7/GF5 | ✅ | Errors under inputs; "Enter a whole number", year message, max-below-min, rating range; rating 7.5/weight 2.5 saved on create; sentence-case empty state; "successfully" |
| GS1/GS2/S8 | ✅ | Session avatars have "Name (score)" tooltips, colours match the dashboard; Comment column present |
| S1/S2/S3/S4/S5/S6 | ✅ | No premature error; "Duration must be greater than 0"; "Start can't be in the future"; min suffix/"+ Add player"/"Create session"; nested dialog dimmed; new player auto-selected |
| P6 | ✅ | "  REGGI " → "A player with this name already exists" |
| P1/P2/P3/P4/P5 | ✅ | Players grid letter tiles; "Game" column + "Add session"; "100%"; "Achievements (1/15)", "Win 30% of at least 5 sessions"; "Update player" |
| SH1/SH2/SH3 | ✅ | Price "-", no duplicate never badge, "Avg. price paid · 1 priced game"; Back from a shelf game returns to /shames |
| C1/C2/C3/C4 | ✅ | No toast; opponent excluded from the other list; crown above avatar; URL ?left=7&right=2 |
| L1/L2/L3 | ✅ | Podiums header hint, "Min. 5 plays", one duration format, Rank |
| LO1/LO2/LO3 | ✅ | "   " → "Location name is required"; " THUIS " → "A location with this name already exists"; no Id column, Sessions right-aligned and sortable |
| GN1/GN2/GN3/GN4/GN5/GN6 | ✅ | Badge 0→1 right after create; icon buttons; default tomorrow 19:30; Escape keeps the dialog; search focus + filter; RSVP link https |
| GN7/GN8 | ✅ | Host pill in Manage RSVPs, host not in the public picker; success view without "host notified" (email off) + Change response |
| LN1/LN2/LN3/LN4/LN5 | ✅ | Start defaults to today, English "yyyy-mm-dd" and calendar, days before start disabled; only 39 owned games listed; Return dialog with date; returned loan "Returned on" editable; "Delete loan of UI-SCAN R2 Game" |
| ST2/ST4/ST5/ST1 | ✅ | ?tab=advanced/shelf-of-shame; Shelf off → menu item gone and /shames redirects to /; on again → badge 6 without reload; Save above Danger Zone; no http warning (URL is https) |
| ST3 | ❌ **not fixed** | EN→NL save shows "Settings are successfully saved"; NL→EN shows "Instellingen zijn opgeslagen" — toast still in the previous language |
| I18N1 | ✅ | "UI-SCAN R2 Player heeft UI-SCAN R2 Game gewonnen"; "Gem. betaalde prijs · 3 spellen met prijs" |
| D5/D6 | ✅ | No CSP or view-transition errors in the console (only 404s for old images, D4) |
| M1 | ✅ | 18 routes at 375px all scrollWidth 375 (incl. game nights, leaderboard, locations, session tables) |
| GA1/EX1/GD11 | ✅ | "Add one game from BGG" / "Import BGG collection"; disabled Add/Ask buttons are clearly grey |
| G9 | ⚠️ setting | New default is dd-MM-yyyy, but dev keeps its stored `yy-MM-dd` — change it in Settings → General on dev |
| NAV1 | ⚠️ not seen | Did not recur during round 2 |

## New findings (round 2)

| # | Page | Type | Finding | Code cause |
|---|------|------|---------|------------|
| R2-01 | Dashboard | UI | Stat label "Avg. price paid · 2 priced games" is truncated on desktop (1280px) too, not only on mobile ("Avg. price paid · 2 priced…") — label too long for the tile || `components/BgtStatistic/BgtTextStatistic.tsx:72` title has `md:line-clamp-1`; fix: keep `line-clamp-2` on all breakpoints (or label + "N priced games" subtitle) |
| R2-02 | Game list table | Data | With 0 priced owned games in the result (e.g. ?q=car) the tiles show "Total price (owned) €0" and "Mean price (owned) · 0 priced games €0" — mean of nothing should be "-" || `routes/games/table.tsx:416-419` + `-utils/gamesSearch.ts:58-62` mean is 0 when pricedCount is 0; fix: show "-" |
| R2-03 | New session | UX | Submitting with other errors (duration/date) plus no players shows only the field errors; "Add at least one player" appears only on the next submit || `routes/sessions/-components/SessionPlayerManager.tsx:55` first sync uses `dontValidate`, and TanStack skips submit validation when other fields are invalid; fix: drop `dontValidate` (keep `dontUpdateMeta`) or validate in `onSubmitInvalid` |
| R2-04 | Game detail | UX | With a single scored player, "Scoring results" shows the same player/score four times (Top, Average, Lowest winning, Lowest) — collapse or hide when there is only one score | |
| R2-05 | Shelf → game | UI | Back button says "Back to games" but (correctly) returns to the shelf — label should follow the destination ("Back") | |
| R2-06 | Compare | UX | Default pair = first two players alphabetically ("dfgh" with 0 sessions vs Kathleen) → page opens on an empty comparison; better default: the two players with the most sessions together | |
| R2-07 | Compare | UI | "Total gameplay time 0" (no unit) next to "7 hours"; "won 0 times against" reads odd — e.g. "No wins yet" | |
| R2-08 | Game nights | **Bug (pre-existing)** | Manage RSVPs: Accept/Maybe/Decline never work — `ManageRSVPsModal.handleStatusChange` sends `playerId: 0`, server `UpdateRsvpCommand` requires PlayerId ≥ 1 → `PUT /api/gamenight/rsvp` 400; no error toast, UI just doesn't change | `client/src/routes/game-nights/-modals/ManageRSVPsModal.tsx:22-29` |
| R2-09 | Manage RSVPs | UI | Middle state is labelled "Maybe" on the buttons but "Pending" in the counters and lists; public page also says "Maybe" || `ManageRSVPsModal.tsx:86` uses `game-nights:rsvp.maybe`; counters use `common:pending` (`GameNightCard.tsx:84`, `_bare/rsvp.tsx:138`) for the same state |
| R2-10 | Game night dialog | a11y | Host/Location comboboxes and the two multi-select triggers have no accessible name (labels not associated) || `BgtSelect.tsx:92,100`, `MultiSelectField.tsx:96-113` pass no `htmlFor`/`id`; fix: `useId()` + `FormFieldWrapper htmlFor` + trigger `id` (as in `BgtInputField.tsx:63,67`) |
| R2-11 | Game night dialog | UX | Host isn't defaulted to the signed-in user's linked player, and the host has to be added to "Invited players" separately | |
| R2-12 | Public RSVP | UX | Date shown as "26-10-11" (stored yy-MM-dd) without weekday — invitees see an ambiguous date; "Games We Might Play" Title Case | |
| R2-13 | Public RSVP | UX | "Change response" opens an empty form (name and previous answer not preselected) | |
| R2-14 | Locations | UX | Duplicate/required name errors from the server appear as a toast bottom-right instead of under the name field | |
| R2-15 | Settings | Bug (ST3 regression) | Language-change toast still in the previous language (see regression table) || `routes/-hooks/useToasts.tsx:5-13` uses the render-time fixed `t` from `useTranslation()`; fix: `i18n.t(key)` at call time |
| R2-16 | New session (sidebar) | UI | Game select is completely blank (no "Choose game" placeholder) when no game is preselected; duration always defaults to 30 instead of the game's play time | |
| R2-17 | Loans | UI | Edit loan form calls the due date "Return date" right above "Returned on"; the card calls it "Due date" | |
| R2-18 | Loans | UX | Loan cards show no "due in N days"/"overdue" state; returned/active lists are cards with lots of empty space — see mockup | |
| R2-19 | Game nights | UX | Each card always renders three RSVP boxes (Accepted/Pending/Declined) even when empty — very tall on mobile; past nights have no link to the sessions played that night — see mockup | |
| R2-20 | Player sessions | UX | On a player's own sessions page the "Winners/Other players" columns are less useful than a "Result" column (won/lost, score, rank) | |

## Test data

| Entity | Name | Deleted? |
|--------|------|----------|
| Game | UI-SCAN R2 Game (id 55, owned, €10) | ✅ deleted in UI ("…and all its content?") |
| Player | UI-SCAN R2 Player (id 8) | ✅ deleted from detail page: navigated to /players, no 404 refetch, green toast (DEL1 ✅) |
| Player | UI-SCAN R2 Guest (id 9) | ✅ deleted via API |
| Session | UI-SCAN R2 Game, 40 min, score 42, comment "UI-SCAN R2 comment" | ✅ removed with the player |
| Location | UI-SCAN R2 Location | ✅ deleted in UI (green toast) |
| Game night | UI-SCAN R2 Night (guest accepted via public page) | ✅ deleted in UI ("Delete game night", badge 1→0) |
| Loan | UI-SCAN R2 Game → UI-SCAN R2 Guest | ✅ returned + deleted in UI |

End state matches the start: 52 games, 6 players, 12 sessions, €140; language English, Shelf of Shame on.

## UX mockups

https://claude.ai/artifact/DDwRkBL6bdQnqtGHnQU7BB: Game nights, Loans, Log session, Players (covers R2-03/04/06/11/12/16/17/18/19/20).
