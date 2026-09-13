# BoardGameTracker — GitHub triage (2026-09-09)

Local checkout: `feature/236-change-detection` @ `2bd981bb` (master @ `0668858c`). Read-only review; nothing was changed on GitHub or in the repo.

## Headline finding: the stable release is 3.5 months stale

- Last GitHub release / `latest` Docker tag: **v0.2.181 (2026-05-21, commit 71a88d5e)**.
- `publish-container.yml` pushes master only to the `beta` tag (`type=raw,value=beta`, line 273); `latest` needs a manual `workflow_dispatch` with `stable_release=true` (lines 17-30, 285).
- `git merge-base --is-ancestor fa254333 v0.2.181` → **no**. Every fix since May (BGG 429 handling, rootless entrypoint, year<1900, loan date, manuals, RAG, player linking) is beta-only.
- Users on `uping/boardgametracker:latest` (e.g. the compose in #217) therefore still hit D#150, D#191 and #216 even though they are fixed on master. **Cutting a stable release is the single highest-leverage action in this triage.**

---

## (a) Summary counts

| Bucket | Count | Items |
|---|---|---|
| Open issues | **12** | |
| — bug | 3 | #17, #28, #217 |
| — feature/enhancement | 6 | #18, #21, #33, #198 (unlabeled), #209, #236 |
| — infra/bot | 3 | #53 (ZAP baseline), #64 (ZAP API), #61 (Renovate dashboard) |
| — question / docs | 0 | (questions live in Discussions) |
| Open issues already implemented | **6 of 12** | #18, #28, #198, #209, #217, #236 (see §c) |
| Discussions | **6** | Q&A 3 (#137 answered, #150 open, #191 open) · Ideas 3 (#196, #197, #201 — none marked answered) |
| Open PRs | **5** | #237 (owner feature), #240-#243 (Renovate) — all 5 red |
| Recently closed issues reviewed | 24 | no wrong closures needing reopen; #16 is superseded by #136/D#191 (see §g) |
| External contributors waiting | 2 | Arvinb1386 (#17 fix on fork, never opened PR), dave-comins (#33 offered a PR) |

---

## (b) "Fix now" — Trivial / Small

| # | Title | Effort | Files | Sketch |
|---|---|---|---|---|
| D#150 (residual bug) | "Game State → invalid input" | **Trivial** | `boardgametracker.client/src/models/Games/GameState.ts:5`, `src/utils/ItemStateUtils.ts:15,39`, `src/utils/ItemStateUtils.test.ts:14,33`, `public/locales/*/game.json` key `state.not-owned` (en-US line 129) | Client enum has `NotOwned = "notOwned"`; backend `BoardGameTracker.Common/Enums/GameState.cs` has only `Wanted/Owned/PreviouslyOwned/ForTrade`. Five selects render `Object.values(GameState)` (`GameFormPlayerFields.tsx:87`, `bgg.tsx:125`, `import/list_.$username.tsx:165`, `table.tsx:90,105`) so "Not self owned" is offered, sent as `"notOwned"`, and rejected by `JsonStringEnumConverter` (`Program.cs:460`) → 400. Delete `NotOwned` from the client enum + the two `switch` cases + tests + the 5 locale keys. |
| #61 | Renovate "Package lookup failures" | **Trivial** | `.github/workflows/ci.yml:196` | Pin comment is `# v3` (floating tag); Renovate needs a semver tag to resolve the digest. Change to `# v3.0.5` (upstream tags: v3.0.5 … v3.0.0). Keeps the SHA. |
| PRs #240-#243 | "Frontend Dependency Scan" red on every Renovate PR | **Trivial** | `boardgametracker.client/package.json:81-82` (`pnpm.overrides`), `pnpm-lock.yaml` | `pnpm audit --audit-level=high --prod` (security.yml step "Audit for vulnerabilities") fails on **browserslist 4.28.2** (2 high advisories, vulnerable `<=4.28.6`; pulled in via `update-browserslist-db`). Add `"browserslist": ">=4.28.7"` to the existing `pnpm.overrides` block and `pnpm install`. Unblocks all four PRs. |
| PR #237 | "Test and SonarCloud Analysis" red | **Trivial** | `boardgametracker.client/src/routes/chat/-components/ChatMessage.test.tsx` (+2 siblings) | .NET 1660/1660 pass; failure is `biome check` → "Found 3 errors" (line-wrap formatting in the chat tests). Run `pnpm biome format --write src/routes/chat` and push. |
| PR #241 | Biome 2.5.12 bump | **Trivial** | TBD (1 file) | Same job: "Found 1 error" — one new lint rule in Biome 2.5.12. Fix the flagged line in the PR branch (after the browserslist override). |
| #136 follow-up / README | Stale BGG statement | **Trivial** | `README.md:51`; new `docs/src/content/docs/getting-started/bgg.mdx` | README still says BGG integration "is under review because of authentication on their API" although #136 shipped (c9575055, 2026-05-18). Replace with: request a key at https://boardgamegeek.com/applications/create (client name "boardgametracker", one key per instance, admin only), paste it in Settings → Integrations. No docs page mentions the BGG key at all (only `AI_API_KEY`). |
| #217 follow-up | Rootless docs + startup message | **Trivial** | `docs/src/content/docs/getting-started/docker.mdx`, `environment-variables.mdx`; `BoardGameTracker.Host/Program.cs:119` | Docs never mention `PUID`/`PGID`/`user:` (grep = 0 hits). Document: defaults 1654/1654 (`entrypoint.sh:4-5`); when the container runs non-root the entrypoint skips `chown` (`entrypoint.sh:10-13`) so `/app/images`, `/app/logs`, `/app/manuals` must be pre-owned by that UID. Improve the fatal message to `JWT_SECRET not set — set JWT_SECRET (>=32 chars) or AUTH_ENABLED=false` (the reporter's compose lacks both; `AuthEnabled` defaults true, `EnvironmentProvider.cs:24-25`). Also note his compose has a typo `DB_USER=:` (invalid key). |
| D#191 (UX) | Collection import "loads forever" / generic error | **Small** | `boardgametracker.client/src/routes/games/import/-hooks/useList.ts:23,101`, `import/list_.$username.tsx:215-219`, `src/utils/axiosInstance.ts:49,79` | Backend already maps `BggRateLimitException`→429 and `BggCollectionPreparingException`→504 (`GlobalExceptionHandler.cs:44-45`), but the page shows one generic `games:import.error-*` for everything. Branch on the axios error kind/status: 429 → "BGG rate-limited this key, wait a minute", 504 → "BGG is still preparing this collection, retry shortly", `timeout` → same with retry button. **Also**: the ImportList batching (`FetchThingsFromBgg`, chunks of 20) that fixes the 30 s axios timeout is **not on master** (`git show master:…/BggImportService.cs \| grep -c FetchThingsFromBgg` = 0); it lives only inside `27b7e9d2` on this branch, bundled with changedetection. Split it into its own commit/PR to master. |
| #17 | Price paid / acquisition date from BGG | **Small** (fork exists) / Medium from scratch | `BoardGameTracker.Core/Games/BggImportService.cs:71-118`, `BoardGameTracker.Common/Models/Bgg/BggImportGame.cs`, `boardgametracker.client/src/models/Games/ImportGame.ts`, `src/routes/games/import/-hooks/useList.ts:49` | `BoardGamer.BoardGameGeek 0.10.0` (csproj:12) does not surface `<privateinfo>` (dll contains no such symbol), so the collection XML must be fetched directly (`/xmlapi2/collection?username=…&subtype=boardgame&showprivate=1`, Bearer key; private info only comes back for the key owner's own collection) and `privateinfo/@pricepaid`, `@pricepaidcurrency`, `@acquisitiondate` parsed. Extend `BggImportGame` (+`PricePaid`, `Currency`, `AcquisitionDate`), `ImportGame.ts`, and prefill `price`/`addedDate` in `useList.ts` (today price defaults to 0 and date to `lastModified`). **Arvinb1386 already did exactly this on their fork** (`Arvinb1386:master` @ 90d14b8b, 2026-08-30, 11 files, +689/-298 incl. `BggCollectionParser.cs` + 353 lines of tests) and pasted the PR description as a comment but never opened the PR. Ask them to open it. Review note: their `ImportBggCollection` drops the `401 → ValidationException("Invalid BGG API key")` mapping — keep it. |
| #53 / #64 | ZAP bot issues | **Trivial** (close) | `.github/workflows/security-full-scan.yml:192` | `allow_issue_writing: false` since PR #202, so these bot issues can never update again; 58 bot comments of noise. Close both; if the residual alerts matter, open one curated "security headers" issue (see §d). |

---

## (c) Already fixed — close candidates (with evidence)

| # | Title | Evidence | In stable v0.2.181? | Suggested note |
|---|---|---|---|---|
| **#18** Lending option | `BoardGameTracker.Common/Entities/Loan.cs` (LoanDate/DueDate/ReturnedDate, `MarkAsReturned`), `BoardGameTracker.Api/Controllers/LoansController.cs` (`api/loans` GET/POST/PUT/PUT return/DELETE, lines 11-65), `boardgametracker.client/src/routes/loans/index.tsx` + `NewLoanModal.tsx`; migrations `20260103071226_AddedLoan`; polish in `87249154 Fix loan date issue`. | Yes (Jan 2026) | "Shipped as the Loans page (menu → Loans): pick friend + start date, optional due date, mark returned. Closing — reopen if something's missing." Requester Koky05 confirmed the minimal scope in Nov 2025. |
| **#28** Manual game creation failing (image URL required) | Root cause was `[Url(ErrorMessage = "Image must be a valid URL")]` on `CreateGameCommand.Image` (9abd29fc:15-16) rejecting the relative path from the image selector. Removed in `4e8e461e "Removing more validation"` (2026-01-25, the day after filing). Now `public string? Image` (`CreateGameCommand.cs:9`) and `image: z.string().nullable().optional()` (`CreateGame.ts:37`). | Yes | Close as fixed in 4e8e461e. |
| **#198** PDF upload for games (multiple) | `998b1a2b "Game manual support added"` (2026-08-05): `Entities/Manual.cs`, `ManualController.cs` (`api/manual/game/{id}` list+upload, `{id}/reindex`, `{id}` delete, `{id}/download`, `{id}/page/{page}/image`, plus `[AllowAnonymous]` `gamenight/{linkId}` list and `…/manual/{manualId}/download` at lines 95-106 — i.e. the invitation-page download the issue asked for), `routes/games/-components/ManualsDialog.tsx`, `useGameManuals.ts`. | **No (beta only)** | Close; it is unlabeled and will otherwise be stale-flagged ~2026-09-30. |
| **#209** RAG generation + chat | `9002d13d "#209 RAG UI first version with chat"` (2026-08-10) → `RagController.cs` (`api/rag/game/{id}/ask`), `BoardGameTracker.Core/Rag/*` (RagService, ManualIndexingService/Queue/BackgroundService, PdfTextExtractor, RulebookChunker, ModelProvisioningBackgroundService), `routes/chat/*`; merged via PRs #211, #212, #215; `10eb2c47 Fix chunking`. PR #237 ("Implemented AI view", 44 files) is the remaining polish. | No (beta only) | Close when #237 merges (or now, with #237 linked). |
| **#217** Does not run rootless | `entrypoint.sh:10-13` non-root branch (skip user setup + `exec dotnet …`), `0bedeb63 Refactor entrypoint.sh`, PR #222 `feat: test entrypoint.sh`. Reporter confirmed on 0.2.342-beta: "Not running as root (UID 1000); skipping user/group setup" — the app now boots and fails only on missing `JWT_SECRET` (config, not rootless). | No (beta only) | Close as fixed with the JWT_SECRET/AUTH_ENABLED explanation + docs follow-up (§b). Keep `bug` label off the follow-up so stale-bot rules stay simple. |
| **#236** changedetection pricing | `27b7e9d2 "#236 Implemented first version"` on `feature/236-change-detection` + 37 uncommitted files with the six review merge-blockers fixed (backend 1699 / frontend 1015 tests green per memory notes). | No | Keep open until the PR merges; then close. Split the unrelated BGG batching + Tailwind fixes out of 27b7e9d2 first. |
| **D#197** Player accounts (link player ↔ user) | `026df65e "Emails, Auth and other bug fixes"` (2026-08-06): migration `20260804223715_AddPlayerLinkAndEmail` (`AspNetUsers.PlayerId`, `Players.Email`), `AuthController.cs:80 [HttpGet("linkable-players")]`, `Player.cs:17,44 Email/UpdateEmail`, settings UI `routes/settings/-utils/playerLinkOptions.ts`, `CreateUserModal.tsx`, `EditUserModal.tsx`, `AccountSettings.tsx`. | No | Reply + mark answered (§e). |
| **D#196** Game instructions (PDF) | Same as #198. | No | Reply + mark answered. |
| **D#137** Default username/password | Already answered (admin/admin). | — | Nothing; but titan-homelab (#136) noted the docs don't state it — add to `quick-start.mdx`. |

Not a reopen candidate but worth a link: **#16** "Unable to import from BGG" was closed 2025-11-10 as "BGG requires auth"; that blocker was resolved by #136 and the live continuation is D#191.

---

## (d) Medium / Large items, ranked by demand

Demand = reactions + non-bot comments + upvotes; all open issues have 0 reactions, so comments/upvotes decide.

| Rank | # | Title | Effort | Demand | Code pointer | Rough plan |
|---|---|---|---|---|---|---|
| 1 | **#33** Add authentication (OIDC) | **Large** (design done; blocking defect itself is Small-Medium) | 2 comments incl. a full external diagnosis (dave-comins offered a PR); 2 ZAP threads flag OIDC endpoints | `routes/_bare/login.tsx:65` sets `location.href` to `GET auth/oidc/{p}/login`, but `OidcController.cs:44` returns `Ok(new AuthorizationUrlResponse(url))` (JSON) → browser shows JSON. `routes/_bare/auth-callback.tsx:10-15` schema only accepts `accessToken/refreshToken/error/redirect` — never `code/state` — so the IdP return falls through to `/login`; `authService.ts` has no callback call. Untracked design doc `OIDC_PLAN.md` (§3 "Blocking defect") already prescribes the fix and re-sequences so no refresh token ever rides a URL. | Follow OIDC_PLAN.md. Minimal unblock: (1) `login.tsx`: fetch the login endpoint via axios, then `location.href = data.url`; (2) `auth-callback.tsx`: accept `code`+`state`, call `GET auth/oidc/{provider}/callback?code&state&redirectUri`, `setTokens` from the JSON body; (3) then the plan's security phases (PKCE state store, `PublicUrl`, secret storage). Reply to dave-comins that a PR for (1)+(2) is welcome. |
| 2 | **#21** Leaderboard | **Medium** | owner-only, milestone MVP | Building blocks exist: `IPlayerRepository.GetTopPlayers(count)` (`IPlayerRepository.cs:17`, returns PlayCount+WinCount), `GetTotalWinCount`, `Players/Specifications/WonPlayerSessionsByPlayerSpec.cs`, `DashboardService.cs:48` (top 4 players). No `leaderboard` symbol anywhere. | New `LeaderboardController` (`api/leaderboard`) + `LeaderboardService`; per ARCHITECTURE.md the GroupBy aggregates go in repo methods (`GetPointsLeaders`, `GetPodiumCounts` (top-3 finishes), `GetPlayDurationLeaders`, `GetSessionCounts`); DTO with the 4 "cards" + full table; new route `routes/leaderboard/index.tsx` + menu entry (`useMenuInfo.tsx`) + 5 locale files. |
| 3 | **D#201** Expansions | **Medium** | 1 upvote, 0 replies (**only unanswered external thread**) | `Entities/Expansion.cs` is a BGG-only child of `Game` (`Guard.Against.NegativeOrZero(bggId)` line 27), managed via `GameController.cs:151-172` (`GET/POST {id}/expansions`, `DELETE {id}/expansion/{expansionId}`), sessions can reference it. `ShameGamesSpec.cs:13` queries `Game` only → expansions already never appear on the Shelf of Shame. | Gap is manual (non-BGG) expansions + ownership/price per expansion. Option A (small): make `Expansion.BggId` nullable + manual create endpoint/dialog. Option B (the ask): `Game.BaseGameId` self-reference + `IsExpansion`, exclude `IsExpansion` in `ShameGamesSpec` and collection counts, show under the base game. B needs one migration + DTO/table changes. Answer the thread now (§e). |
| 4 | **D#191** BGG collection import | **Small** | 3 upvotes, 2 users, same root as #16/D#150 | see §b | Cut stable release + UX branch on 429/504 + merge batching to master. |
| 5 | **#17** BGG price/date | **Small** with fork | 4 comments, contributor waiting | see §b | Invite Arvinb1386's PR; review the 401 regression. |
| 6 | ZAP residuals (#53/#64) | **Small** | bot | `Program.cs` middleware; `.zap/rules.tsv` (3 ignores) | Residual real findings: missing `Cross-Origin-Opener-Policy` / `-Embedder-Policy` / `-Resource-Policy` headers, `CSP style-src unsafe-inline`, "Application Error Disclosure" on OIDC callback endpoints (becomes moot with #33). Path-traversal / format-string hits on `/api/player`, `/api/location`, `bgg/import?username=` are scanner false positives (JSON echo of the input) → add to `rules.tsv`. One small "security headers" issue replaces both bot threads. |
| 7 | **#236** changedetection roadmap | **Medium** (post-merge) | owner | memory: `changedetection-review-findings.md` | Q1+Q2 (read watch object + explicit failure reasons) → B1+B4 (persist history + poller + `GameWatch` table) → B3 (create watch from BGT via ShopUrl). |

---

## (e) Unanswered discussions — draft answers

**D#191 — "Cannot import BGG Games from collection" (Q&A, 3 upvotes, unanswered)**
> Two different things are going on. (1) The log Divinesteel pasted ("BGG API request failed for collection import … StatusCode = TooManyRequests") comes from BGG rate-limiting the API key — with the new per-user keys BGG throttles hard, and the collection endpoint first answers 202 "preparing" and must be polled. Since 0.2.3xx-beta (commit fa254333) the app maps this to a proper 429/"collection is being prepared" response instead of a generic failure, and the game-detail fetches are batched 20-at-a-time (previously 25 sequential calls blew past the UI's 30 s timeout — that is the "loads forever, then works on retry" symptom). (2) None of that is in the `latest` image yet: the last stable release is v0.2.181 from May. Please pull `uping/boardgametracker:beta` (or wait for the next stable, which I'll cut shortly), retry, and if it still fails wait ~1 minute between attempts — BGG's limit is per key. Single-game import works because it's one request, not a collection build.

**D#150 — "Game State — invalid input" (Q&A, 2 upvotes, unanswered)**
> No, this is unrelated to `BGG_API_KEY` — the manual form works without any BGG key. The state dropdown contains a "Not self owned" entry that the server doesn't accept (it only knows Wanted / Owned / Previously owned / For trade), so choosing it fails validation; picking one of the other four works. The stray option is removed in the next build (and the form itself got several fixes in the June/August betas). If you're on the `latest` image you're on v0.2.181 — try `:beta` or the next stable.

**D#197 — Player accounts (Ideas, 1 upvote + "looking forward" from furll)**
> Done in the August betas (commit 026df65e): a user account can be linked to a player when you create or edit the account (Settings → Users → "Linked player"), players got an optional e-mail field, and `GET /api/auth/linkable-players` drives the picker. Linking is optional, as you asked. Stats stay on the player, so linked accounts share them. It's in `:beta` now and will be in the next stable release. Marking this as answered.

**D#196 — Game instructions PDF (Ideas, 2 upvotes)**
> Shipped in the August betas (#198, commit 998b1a2b): upload one or more PDFs per game from the game detail page ("Manuals"), download them there, and invited players get download links on the game-night invitation page without logging in. As a bonus the PDFs are indexed so you can ask questions about the rules in the new Chat page (#209, fully local via Ollama by default, see docs → "RAG"). Marking as answered.

**D#201 — Expansions for games (Ideas, 1 upvote, no reply yet)**
> Expansions are already a separate thing from games: on a game you can attach its expansions (currently picked from BGG), log plays with them, and because the Shelf of Shame only looks at base games (`ShameGamesSpec`), expansions never show up there. What's missing versus your proposal is (a) adding an expansion that isn't on BGG and (b) tracking owned/price per expansion. I'd rather extend the existing expansion model (nullable BGG id + manual add, ownership state) than add an "is expansion" flag on games, so a game can't accidentally be both. Would that cover your use case? Which expansions were you trying to add — from BGG or manual?

---

## (f) Open PRs

| PR | Branch | Author | State | Failing check(s) | Cause / action |
|---|---|---|---|---|---|
| **#237** feat: download ai models | `feature/download-ai-models` | mregni | ready, 44 files +1182/-282 (already merged into this working branch at 2bd981bb) | Test and SonarCloud Analysis | .NET tests pass (1660); Biome formatting, 3 errors in `routes/chat/-components/*.test.tsx`. `biome format --write`, push, merge → closes #209. |
| **#240** fix(deps): update all non-major | `renovate/all-minor-patch` | renovate | red | Frontend Dependency Scan (PR Labeler/Semantic title were cancelled, not failed) | `pnpm audit --audit-level=high --prod` → browserslist 4.28.2 (2 high). Add pnpm override (§b), retrigger. |
| **#241** biome → 2.5.12 | `renovate/biome` | renovate | red | Frontend Dependency Scan + Test and SonarCloud | Same audit + "Found 1 error" from a new Biome rule. Fix after override. |
| **#242** actions/deploy-pages digest | `renovate/actions-deploy-pages-digest` | renovate | red | Frontend Dependency Scan only | Audit blocker only; safe to merge once override lands. |
| **#243** docker/setup-qemu-action digest | `renovate/docker-setup-qemu-action-digest` | renovate | red | Frontend Dependency Scan only | Same. |

Missing PRs that should exist: Arvinb1386's #17 fix (fork ready, never opened); `feature/236-change-detection` (37 uncommitted files, needs split of 27b7e9d2).

---

## (g) Patterns, labels, milestones

**Repeated asks**
- **BGG import is the #1 user pain** — 5 threads by 5 different users: #16 (closed), #17, #136 (closed), D#150, D#191. Root causes are now (1) stale `latest` image, (2) BGG's per-key throttling/202-preparing behaviour, (3) the orphan `notOwned` option. A stable release + the §b fixes close all of them.
- **Manuals → RAG chain**: D#196 → #198 → #209 → PR #237. All delivered; threads never told the requester.
- **Auth/onboarding**: #33 (OIDC), #217's final error (`JWT_SECRET`), D#137 (default admin/admin not in docs), titan-homelab's remark in #136. Docs gap: `quick-start.mdx` should state admin/admin, that `JWT_SECRET` is mandatory unless `AUTH_ENABLED=false`, and PUID/PGID.
- **Contradictions**: `README.md:51` ("BGG under review") vs shipped #136 + Settings UI; #16 closed "BGG requires auth" while the feature now exists; docs say `AUTH_ENABLED` default true but the reporter's compose (and no docs page) explains that this makes `JWT_SECRET` fatal.

**Stale-bot exposure** (`stale.yml`: 60 d + 14 d, exempts `bug`, `enhancement`, `security`, `pinned`, `owner-responded`, `in progress`)
- Unlabeled and therefore stale-eligible: **#198** (created 2026-08-01 → flagged ~2026-09-30 despite being done), #53, #64, #61. Nothing has been closed wrongly so far (24 closed issues checked; all `COMPLETED` with a matching commit or explicit owner note).
- Renovate's dashboard (#61) should carry `pinned` or be added to `exempt-issue-labels` via a `dependencies` label so it never gets stale-closed.

**No owner response / no label**
- **D#201** (Srali99, 2026-08-06) is the only external thread with zero replies.
- #198, #53, #64, #61 have no labels; #21/#28 sit in the "MVP" milestone with everything else unmilestoned.

**Recommended labels / milestones**
| Item | Action |
|---|---|
| #18, #28, #198, #217 | close (fixed) — add `fixed-in-beta` note where not in stable |
| #209 | close on #237 merge |
| #53, #64 | close (bot orphaned); optionally open "security headers (COOP/COEP/CORP, CSP)" `security` |
| #61 | `dependencies` + `pinned`; fix marocchino pin |
| #17 | `bug`, `help wanted`, `good first issue`; ping Arvinb1386 to open the PR |
| #33 | `enhancement`, `security`, `help wanted`; milestone **1.0**; link OIDC_PLAN.md (commit it) |
| #21 | keep `enhancement`/MVP |
| #236 | `in progress` |
| D#150, D#191 | answer, then mark answered after the stable release |
| D#196, D#197 | answer + mark answered |
| D#201 | answer (question back to requester); convert to issue if confirmed |
| New | milestone **"Next stable release"** containing: cut release, browserslist override, marocchino pin, README/docs BGG + PUID + JWT_SECRET, `notOwned` removal, BGG batching to master |

---

## (h) Appendix — every open issue

| # | Title | Type | Effort | Status | Related code |
|---|---|---|---|---|---|
| 17 | Price paid from BGG not imported | bug | Small (fork) / Medium | open; contributor fix on `Arvinb1386:master` 90d14b8b, no PR | `Core/Games/BggImportService.cs:71-118`, `Common/Models/Bgg/BggImportGame.cs`, `client/src/routes/games/import/-hooks/useList.ts:49`, `Core.csproj:12` (BoardGamer 0.10.0 lacks privateinfo) |
| 18 | Lending option | feature | — | **implemented** (Jan 2026, stable) | `Common/Entities/Loan.cs`, `Api/Controllers/LoansController.cs`, `client/src/routes/loans/index.tsx`, 87249154 |
| 21 | Leaderboard | feature | Medium | open, no code | `Core/Players/Interfaces/IPlayerRepository.cs:17` (`GetTopPlayers`), `Core/Dashboard/DashboardService.cs:48`, `Players/Specifications/WonPlayerSessionsByPlayerSpec.cs` |
| 28 | Manual game creation failing | bug | — | **fixed** 4e8e461e (2026-01-25, stable) | `Common/DTOs/Commands/CreateGameCommand.cs:9`, `client/src/models/Games/CreateGame.ts:37` |
| 33 | Add authentication (OIDC) | feature | Large (unblock: Small-Medium) | open; server done, client mis-wired | `client/src/routes/_bare/login.tsx:65`, `client/src/routes/_bare/auth-callback.tsx:10-15`, `Api/Controllers/OidcController.cs:38-53`, `client/src/services/authService.ts:37`, `OIDC_PLAN.md` (untracked) |
| 53 | ZAP Scan Baseline Report | infra/bot | Trivial (close) | orphaned (`allow_issue_writing: false`) | `.github/workflows/security-full-scan.yml:187-202`, `.zap/rules.tsv` |
| 61 | Dependency Dashboard | infra/bot | Trivial | open (keep); lookup failure | `.github/workflows/ci.yml:196` (`# v3` → `# v3.0.5`) |
| 64 | ZAP API Scan Report | infra/bot | Trivial (close) | orphaned; 58 bot comments | same as #53 |
| 198 | PDF upload for games (multiple) | feature | — | **implemented** 998b1a2b (2026-08-05, beta only); unlabeled → stale risk | `Common/Entities/Manual.cs`, `Api/Controllers/ManualController.cs:27-106`, `client/src/routes/games/-components/ManualsDialog.tsx` |
| 209 | RAG generation + chat | feature | — | **implemented** 9002d13d + PRs #211/#212/#215 (beta only); PR #237 polish pending | `Api/Controllers/RagController.cs:23-24`, `Core/Rag/*`, `client/src/routes/chat/*` |
| 217 | Does not run rootless | bug | — (+Trivial docs) | **fixed** 0bedeb63 / PR #222 (beta only); reporter's last error = missing `JWT_SECRET` | `entrypoint.sh:10-13`, `Host/Program.cs:113-127`, `Core/Configuration/EnvironmentProvider.cs:24-31`, `docs/…/environment-variables.mdx:13,21` |
| 236 | changedetection pricing | feature | Medium (roadmap) | implemented on this branch (27b7e9d2 + 37 uncommitted files), PR not yet opened | `Core/ChangeDetection/*`, `Api/Controllers/GameController.cs` (price endpoints), `client/src/routes/games/-components/TrackedPriceIcon.tsx`, migration `20260827220101_AddChangeDetectionWatchIdToGame` |

Discussions: D#137 answered · D#150 open (Trivial residual fix + release) · D#191 open (Small UX + release) · D#196 done (answer) · D#197 done (answer) · D#201 open (Medium, unanswered).
