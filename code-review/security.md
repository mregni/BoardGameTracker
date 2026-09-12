# BoardGameTracker — Application Security Review (read-only)

Date: 2026-09-09 · Branch: `feature/236-change-detection` @ `2bd981bb` (+ uncommitted changes) · Scope: ASP.NET Core API (Api/Core/Common/Host), React SPA, Docker/compose, CI workflows.
Method: every finding below is backed by code read during this review; `file:line` references are to the working tree. Nothing was modified; git was used read-only (`log`, `show`, `ls-files`, `check-ignore`, `branch --contains`).

Threat model covered: anonymous internet user (instance exposed), authenticated non-admin (`User`/`Reader`), malicious external services (BGG, changedetection.io, Ollama/OpenAI/Groq, DockerHub, SMTP), malicious uploads (images/PDF), malicious OIDC provider config.

---

## 1. Ranked findings

| ID | Sev | Title | CWE | Effort | Conf. |
|----|-----|-------|-----|--------|-------|
| F-01 | High | Shipped placeholder `JWT_SECRET` passes the ≥32-char guard → forgeable admin tokens | 798/1392 | S | High |
| F-02 | High | Default `admin`/`admin` seeded silently, never forced to change | 1393/521 | S–M | High |
| F-03 | Medium | Live Groq API key in a local git stash commit (not on any branch/remote) | 798/540 | S | High |
| F-04 | Low | Live Sentry auth token on disk, leaks into Docker build context | 538 | S | High |
| F-05 | Medium | OIDC `state` not bound to browser/user → login-CSRF & account-link takeover once the flow is wired | 352/384 | M | High (code) / Med (exploitability today) |
| F-06 | Medium | RAG endpoint: any role, no rate limit, no length cap, infinite HTTP timeout → LLM bill / GPU DoS | 770/400 | S | High |
| F-07 | Medium | Rate limiting + lockout keyed by IP; behind an untrusted proxy all clients share one bucket; fixed `admin` username is trivially lockable | 770/645 | S–M | High |
| F-08 | Medium | Multi-frame image decode bomb (no `DecoderOptions`, pixel cap per frame only; BGG downloads have no pixel cap) | 409 | S | High |
| F-09 | Medium | Untrusted PDFs → PdfPig + `pdftoppm` (poppler) with no magic-byte check, no timeout, no concurrency cap, 1 GB request limit | 400/434 | M | High |
| F-10 | Low | Username-enumeration timing oracles (login, forgot-password) and log forging of raw username | 208/117 | S | High |
| F-11 | Low | Refresh tokens and OIDC client secret stored in plaintext; no reuse-family revocation | 312 | S | High |
| F-12 | Low | Anonymous disclosure: `/api/settings` (internal changedetection URL), version info, unauthenticated `/images/*` | 200 | S–M | High |
| F-13 | Low | Outbound HTTP: redirects followed, `x-api-key` re-sent cross-host, OIDC authority not forced to https, no private-range block | 918/200 | S–M | High |
| F-14 | Low | Authenticated users can use the SMTP account as a relay via game-night invites | 799 | S | High |
| F-15 | Low | Data Protection keys not persisted and the warning is silenced → reset tokens die on restart | 693 | S | High |
| F-16 | Low | Runtime data dirs (`postgres-data/`, `images/`, `ollama/`, `manuals/`) have no ignore rule | 538 | S | High |
| F-17 | Info | Source maps shipped in the production bundle | 540 | S | High |
| F-18 | Info | Hardcoded backend Sentry DSN; personal SMTP account details in AppHost | 200 | S | High |
| F-19 | Info | Supply chain / Docker hygiene (tag-only pins, unused packages, `.dockerignore` gaps) | 1104 | S | High |
| F-20 | Info | `AUTH_ENABLED=false` opens settings/uploads/imports to the whole network with only a one-line log | 306 | S | High |
| F-21 | Info | Minor hardening (HSTS, password policy, access-token lifetime, unbounded MemoryCache on anonymous OIDC start) | — | S | High |

### F-01 · High · Shipped placeholder JWT secret is accepted as valid
- `docker-compose.yml:20`, `docker-compose.gpu.yml:20`, `.env.example:10` (and local `.env:10`) ship `JWT_SECRET=CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS` — 40 characters.
- `BoardGameTracker.Host/Program.cs:115-128` only rejects an empty secret or one shorter than 32 chars; the placeholder passes. (`docker-compose.build.yml:29` defaults to `CHANGEME`, 7 chars, which correctly fails at startup — inconsistent.)
- `BoardGameTracker.Core/Auth/TokenService.cs:33-58` signs HS256 with that key; issuer/audience are the public defaults in `BoardGameTracker.Host/appsettings.json:10-11`. Authorization is claim-only (`[Authorize(Roles = "Admin")]`) — no per-request user lookup.
- Attack: anyone running the stock compose without editing has a publicly known signing key. An internet attacker mints `{"sub":"x","unique_name":"x","role":"Admin","iss":"boardgametracker-api","aud":"boardgametracker-client"}` and calls `POST /api/maintenance/factory-reset`, `POST /api/auth/register`, `PUT /api/settings` (steal BGG / changedetection keys) — full takeover, no password needed, no lockout.
- Fix: at startup refuse a denylist of shipped placeholders (`CHANGEME*`, the AppHost dev key) and low-entropy values (e.g. < 3.5 bits/char); or, better, generate a random 64-byte secret on first start and persist it to a volume (`/app/keys`) when `JWT_SECRET` is unset, logging that it did so. Also align `docker-compose.build.yml`.

### F-02 · High · Default admin credentials, silent, never rotated
- `BoardGameTracker.Core/Auth/DbSeeder.cs:46-48` uses password `admin` when `ADMIN_PASSWORD` is empty; lines 63-66 log **only** when a custom password was used — the insecure path is silent. `.env.example:13` ships `ADMIN_PASSWORD=` empty. Username is fixed (`admin`, line 39). No "must change password" flag exists on `ApplicationUser`.
- Docs do say "change it after first login" (`docs/src/content/docs/getting-started/quick-start.mdx:84`), but nothing enforces it. Lockout (`Program.cs:103-105`) does not help against a known password.
- Attack: exposed instance → `POST /api/auth/login {"username":"admin","password":"admin"}` → admin JWT.
- Fix: when `ADMIN_PASSWORD` is empty generate a random password, print it once at Warning level (Grafana/Jellyfin pattern), and/or add a `MustChangePassword` flag checked by a policy on every non-auth endpoint; log a loud warning every startup while the seeded default is still valid.

### F-03 · Medium · Live Groq API key in a local git stash
- Confirmed: `git show f0c6f1fb:BoardGameTracker.AppHost/AppHost.cs` line 57 contains `env["AI_API_KEY"] = "gsk_VJecvEE6…"`. `f0c6f1fb` is the *index* commit of `stash@{0}` ("WIP on feature/download-ai-models").
- Verified **not** public: `git log --all -S "<key>"` hits only `f0c6f1fb`; `git branch -a --contains f0c6f1fb` is empty; `HEAD`, `27b7e9d2`, `origin/feature/236-change-detection` and `origin/master` versions of `AppHost.cs` contain 0 matches. Current `AppHost.cs:8` correctly reads `Parameters:groq-api-key` from user-secrets.
- Risk: a working credential sat in plaintext in the tree and still lives in `.git`; any `git push --all`/`--mirror`, repo backup, or stash-apply-then-commit would publish it. Gitleaks in CI cannot see stashes.
- Fix: revoke the key in the Groq console now; `git stash drop stash@{0}`; `git reflog expire --expire=now --all && git gc --prune=now`; install a local `gitleaks protect --staged` pre-commit hook.

### F-04 · Low · Sentry auth token on disk and in the Docker build context
- `boardgametracker.client/.env.sentry-build-plugin:5` holds `SENTRY_AUTH_TOKEN=sntrys_…` (org `boardgametracker`). Gitignored (`boardgametracker.client/.gitignore:27`) and absent from history (`git log --all -S sntrys_` empty).
- But `.dockerignore:2` only excludes files named exactly `.env`; `Dockerfile:22` (`COPY boardgametracker.client/ ./`) copies the token into the frontend build stage for local `docker-compose.build.yml` builds. The runtime stage only copies `dist` (`Dockerfile:53`), so it is not in shipped images.
- Fix: add `**/.env.*` to `.dockerignore`; keep the token CI-only (`publish-container.yml:302` already passes it as a build arg); rotate if the file was ever shared.

### F-05 · Medium · OIDC state is not bound to the browser or user
- `BoardGameTracker.Core/Auth/OidcService.cs:75-76` stores `state → code_verifier` in a global `IMemoryCache`; `:235-243` accepts *any* cached state from *any* client; `:171-207` (`HandleLinkCallbackAsync`) links the IdP `sub` to whatever user is currently authenticated. No `nonce`, no `id_token` validation (`:269-274` uses only `access_token` → userinfo).
- Today the end-to-end flow is non-functional (`boardgametracker.client/src/routes/_bare/login.tsx:65` navigates the browser to a JSON endpoint; `auth-callback.tsx:10-14` expects `accessToken`/`refreshToken` in the URL, which the API never issues) — `OIDC_PLAN.md` already records this and forbids tokens-in-URL. The server-side weakness will become exploitable the moment the SPA calls `link-callback` with the victim's bearer token.
- Attack (once wired): attacker starts a flow (`GET /api/auth/oidc/{p}/login`, anonymous, unlimited), authenticates at the IdP with **their** account, captures `code`+`state`, and lures a logged-in admin to the SPA link-callback URL → admin's account is linked to the attacker's IdP identity → attacker signs in as admin. Same primitive gives classic login-CSRF on `/callback`.
- Fix: bind `state` to a short-lived, HttpOnly, SameSite=Lax cookie (or, for link flow, to the `userId` stored alongside the verifier) and reject mismatches; validate `id_token` (`iss`, `aud`, `exp`, `nonce`, signature via JWKS); rate-limit `/oidc/{p}/login` and `/callback` with the `auth` policy; require the linking user to re-enter their password.

### F-06 · Medium · RAG endpoint is a cost/DoS amplifier for any account
- `BoardGameTracker.Api/Controllers/RagController.cs:11,23-34`: `[Authorize]` only (Reader can call), no `[EnableRateLimiting]`; `AskRagCommand.cs` has no length limit; `Program.cs:174-175` gives the AI `HttpClient` `Timeout.InfiniteTimeSpan`. Each call makes an embedding request plus a chat completion (`RagService.cs:53-75`); the dev config points at paid Groq (`AppHost.cs:54-57`).
- Attack: a low-privilege user scripts thousands of 100 KB questions → provider bill / token quota exhaustion, or pins the local GPU so nothing else indexes.
- Fix: `[Authorize(Roles = UserOrAdmin)]`, a per-user fixed-window limiter (e.g. 10/min), `[MaxLength(2000)]` on `Question`, a finite client timeout (120 s) and pass the request `CancellationToken` through (already threaded), cap `TopK`.

### F-07 · Medium · Rate limiting and lockout are easy to turn against the instance
- `Program.cs:152-171` partitions `auth`/`changedetection` limiters by `RemoteIpAddress`; `Program.cs:67-85` clears `KnownProxies` and trusts only `TRUSTED_PROXIES` (empty by default in `.env.example:15`). Behind Traefik/nginx without that variable every client has the proxy's IP → one attacker spends the shared 10 req/min `auth` bucket and blocks *all* logins and refreshes (SPA logs users out on refresh failure, `axiosInstance.ts:170-175`).
- Independently, `Program.cs:103-105` locks any account after 5 failures for 15 min and the admin username is fixed (`DbSeeder.cs:39`) → an anonymous attacker keeps `admin` permanently locked.
- Fix: at startup, if `X-Forwarded-For` arrives from an untrusted source log a warning naming `TRUSTED_PROXIES`; key the login limiter on `ip + username`; use a sliding window; consider exponential delay instead of hard lockout for the admin role, or an env knob to disable lockout for admins on LAN deployments.

### F-08 · Medium · Image decompression bomb via multi-frame formats
- `BoardGameTracker.Core/Images/ImageService.cs:113-131`: `Image.Identify` + a 50 MP check on `Width*Height`, then `Image.LoadAsync(buffered)` with default `DecoderOptions` (`MaxFrames` unlimited; grep shows no `DecoderOptions`/`MemoryAllocator` anywhere). The check is per frame; a 15 MB animated GIF/TIFF (LZW compresses flat frames extremely well) can carry dozens of 50 MP frames → multi-GB allocation → container OOM. `DownloadImage` (`:60`) applied to BGG-supplied URLs has no pixel cap at all.
- Fix: `Image.Load(new DecoderOptions { MaxFrames = 1 }, stream)`; reuse the pixel check in `DownloadImage`; set `Configuration.Default.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions { AllocationLimitMegabytes = 256 })` at startup.

### F-09 · Medium · Untrusted PDF pipeline
- Validation is header + extension only (`BoardGameTracker.Core/Manuals/ManualService.cs:243-248`), no `%PDF-` magic check. Files feed PdfPig (`Rag/PdfTextExtractor.cs:17`) and `pdftoppm` from unpinned `poppler-utils` (`Dockerfile:74`, `Rag/PdfPageRenderer.cs:69-85`). The renderer has no timeout and no concurrency limit; `WaitForExitAsync(ct)` (`:96`) stops waiting on cancel but never kills the child.
- `ManualController.cs:16,38-39` allows a **1 GB** request body from any `User`; the 200 MB per-file check (`ManualService.cs:250-253`) runs after the whole multipart is buffered.
- Attack: a `User` uploads a poppler-crashing/CPU-burning PDF; any authenticated account (Reader included) then hammers `GET /api/manual/{id}/page/{n}/image` → unbounded `pdftoppm` processes. Separately, repeated 1 GB uploads fill the volume.
- Fix: magic-byte check; `RequestSizeLimit` = 200 MB × max files; `SemaphoreSlim(2)` around `pdftoppm`; `CancellationTokenSource` with 30 s timeout and `process.Kill(entireProcessTree: true)`; keep poppler current (Trivy already gates the image).

### F-10 · Low · Enumeration timing oracles and log forging
- `AuthService.cs:56-62` returns immediately for unknown users but runs the PBKDF2 check for known ones (`:63`); `ForgotPasswordAsync` (`:285-319`) awaits the SMTP round-trip only when the user exists and has an email → both leak account existence by response time.
- `AuthController.cs:35` and `AuthService.cs:59,72` log the raw request username before Identity validates it; Serilog console/file sinks render it inline → CR/LF forged log lines. CodeQL's `cs/log-forging` is explicitly disabled (`.github/codeql/codeql-config.yml:6-8`).
- Fix: hash a dummy password when the user is unknown; enqueue reset e-mails (fire-and-forget with logging); strip control characters or log only a hash of the username.

### F-11 · Low · Plaintext long-lived secrets in the database
- `RefreshToken.cs:25-26` stores the raw token; `TokenService.cs:74-79` looks it up by equality; `AuthService.cs:93-113` rejects revoked tokens but does not revoke the family on reuse. `OidcProvider.cs:11` stores `ClientSecret` in clear (plan D-7 acknowledges, blocked on F-15).
- Impact: DB read (backup leak, SQL access) → 7-day session replay; client secret exposure.
- Fix: store SHA-256(token) and compare hashes; on presentation of a revoked token revoke every active token for that user; encrypt the client secret with Data Protection once F-15 is done.

### F-12 · Low · Anonymous information disclosure
- `SettingsController.cs:35-41` (`[AllowAnonymous]`) returns `PublicUrl` and the DB-stored `ChangeDetectionBaseUrl` (`SettingsService.cs:51-52`) — the internal hostname/IP of the changedetection instance — plus feature flags; `:43-49` returns exact version and update state.
- `Program.cs:343-353` serves `/images/cover/*` and `/images/profile/*` with no authentication regardless of `AUTH_ENABLED`; player photos are reachable by anyone holding a URL (names are `<original>_<8 random chars>.webp`, `StringExtensions.cs:17-24`; URLs are embedded in the public RSVP page).
- Fix: split a minimal public settings DTO (locale/date formats) from the authenticated one; when auth is enabled, gate `/images` with a small middleware that requires a valid bearer/cookie or serve through a controller.

### F-13 · Low · Outbound HTTP hardening
- `OidcService.cs:320-326` fetches `{Authority}/.well-known/openid-configuration` with the default client: http allowed, redirects followed, no size cap; `:259-283` posts the client secret to whatever `token_endpoint` the (admin-supplied or discovered) document says.
- `ChangeDetectionClient.cs:164-168` adds `x-api-key` as a default header; `HttpClient` strips only `Authorization` on cross-host redirects, so a compromised/MITM'd changedetection host can redirect and harvest the key (base URL is admin-set, http allowed, `SettingsService.cs:79-84`).
- `ImageService.cs:41-42` fetches BGG-supplied image URLs with no private-range denylist (blind SSRF only if BGG is compromised).
- Exploitable by admins or compromised third parties only. Fix: `AllowAutoRedirect = false` on the named clients, require https for `Authority`/`ChangeDetectionBaseUrl` unless the host is `localhost`/RFC1918 and the operator opts in, set `MaxResponseContentBufferSize` on the default client, optional RFC1918/link-local denylist for BGG downloads.

### F-14 · Low · SMTP relay via invites
- `GameNightController.cs:46-53` (`User`/`Admin`) → `GameNightService.SendInvitesAsync` (`:200-242`) mails every pending player; any `User` can set arbitrary player e-mails (`PlayerService.cs:50,72-75`) and the subject embeds the attacker-chosen title (`:217`). Body is HTML-encoded (`:215-218`) and MimeKit encodes headers structurally, so no injection — just relay abuse against the operator's SMTP reputation.
- Fix: per-game-night send cooldown, recipient cap, Admin-only or verified addresses.

### F-15 · Low · Data Protection keys not persisted, warning silenced
- No `AddDataProtection().PersistKeysTo*` anywhere (grep); `Program.cs:43` raises `XmlKeyManager` logging to Error, hiding the "keys will not be persisted" warning. Password-reset tokens (`AuthService.cs:308`) and any future encrypted secret die on every restart/replica.
- Fix: `builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))` on a volume (add to compose/`entrypoint.sh` chown); this also unblocks OIDC secret encryption.

### F-16 · Low · Runtime data directories are not ignored
- `git check-ignore`: `postgres-data/` (17 entries), `images/`, `ollama/`, `manuals/` and `BoardGameTracker.Host/wwwroot` have **no** ignore rule (only `logs`, `local-packages`, `.env`, `BoardGameTracker.Host/manuals` do). `.dockerignore:25` (`**/data`) does not match `postgres-data`.
- Risk: a `git add -A` from the repo root can commit a live Postgres cluster (password hashes, refresh tokens, BGG/changedetection keys, OIDC client secret) and player photos; the build context ships them to the Docker daemon.
- Fix: add `postgres-data/`, `images/cover/`, `images/profile/`, `manuals/`, `ollama/`, `BoardGameTracker.Host/wwwroot/` to `.gitignore` and `.dockerignore`.

### F-17 · Info · Source maps in production
- `boardgametracker.client/vite.config.ts:49` `sourcemap: true`; the Sentry plugin (`:14-17`) has no `filesToDeleteAfterUpload`; `dist/assets/*.js.map` exist and are copied to `wwwroot` (`Dockerfile:53`) and served publicly. Open-source, so recon value only. Fix: `sourcemaps: { filesToDeleteAfterUpload: ["./dist/**/*.map"] }` or `sourcemap: "hidden"`.

### F-18 · Info · Hardcoded DSN / personal infra details
- Backend Sentry DSN literal at `BoardGameTracker.Common/Extensions/WebHostBuilderExtensions.cs:29` (public since commit `86cfaa30`); DSNs are semi-public but allow event flooding of the project quota. `AppHost.cs:65-70` commits the owner's SMTP host, username (`nobelenoedelMailer`) and from-address; the dev JWT literal at `AppHost.cs:5` is 45 chars and would pass F-01's guard if ever reused. Fix: read DSN from `SENTRY_DSN`; move SMTP identity to user-secrets.

### F-19 · Info · Supply chain / Docker hygiene
- Base images tag-pinned only (`Dockerfile:8,30,66`; compose `pgvector/pgvector:pg16`, `ollama/ollama:latest`); `renovate.json:155-156` sets `pinDigests: false`; `apk add` unpinned (`Dockerfile:74`).
- Container starts as root and drops via `su-exec` after `chown -R` (`entrypoint.sh:9-27`) — good pattern; `PUID=0` would keep root. `DOTNET_EnableDiagnostics=0` set.
- `BoardGameTracker.Core.csproj:14,19` reference `HtmlAgilityPack` and `Newtonsoft.Json` with zero usages (grep) — remove. `.dockerignore` is case-sensitive: `BoardGameTracker.Host/Logs/` (capital L, contains DSN at Debug) and `Host/manuals` are copied into the build stage (not runtime).

### F-20 · Info · `AUTH_ENABLED=false` surface
- `AuthDisabledMiddleware.cs:18-30` makes every request an `Admin`; `AuthDisabledFilter` 409s only Auth/OIDC/Admin/Maintenance controllers. Everything else — `PUT /api/settings` (BGG key, changedetection URL+key), image/manual upload, BGG import, RAG, all data — is open to anyone reaching `0.0.0.0:5444` (compose publishes it). Only a one-line log (`Program.cs:364`). Suggest a multi-line startup warning and refusing the mode unless `ASPNETCORE_URLS` is loopback or an explicit acknowledgement variable is set.

### F-21 · Info · Minor hardening
- HSTS `max-age=2592000` without `includeSubDomains`/`preload` (`Program.cs:304`); `AllowedHosts: *` (`appsettings.json:8`) — harmless here because `PublicUrl` is config, not `Host`.
- Password policy 8 chars, no complexity or breached-password check (`Program.cs:97-101`); access tokens 60 min (`appsettings.json:12`) with no server-side revocation — role changes/deletions take effect only at refresh.
- `GET /api/auth/oidc/{p}/login` is anonymous, unlimited, and writes a 10-min entry into an unbounded `IMemoryCache` per call (`OidcService.cs:76`, `Program.cs:182`) — attach the `auth` limiter.
- Swagger CSP uses `unsafe-eval`/`unsafe-inline` and the UI is unauthenticated when `SWAGGER_ENABLED=true` (`Program.cs:297-300,328-336`) — default off in production, known.

---

## 2. Verified-good controls (no finding)
- No raw SQL anywhere (`FromSqlRaw`/`ExecuteSqlRaw` grep clean); pgvector query is LINQ (`Rag/Specifications/NearestManualChunksSpec.cs`). System.Text.Json only; no `TypeNameHandling`. BGG XML parsed via `XDocument` defaults in the client DLL (no `XmlReaderSettings`/`DtdProcessing` symbols) → DTD prohibited.
- Path traversal: `PathHelper.MapImageWebPathToPhysical` (`Common/Helpers/PathHelper.cs:19-36`) is used at the only web-path→disk sink (`ImageService.cs:146`, reached from `GameService.cs:76`, `PlayerService.cs:74,107`); `ManualService.cs:273-282` and `ManualIndexingService.cs:146-156` guard manual paths; `GenerateUniqueFileName` strips directories; figures dir keyed by `int`.
- Uploads: ImageSharp rejects SVG/HTML, output is always re-encoded WebP, so nothing script-bearing lands under `/images`. Manuals are served as `attachment` with a fixed `application/pdf` type.
- `ShopUrl` must be absolute http(s) (`BaseGame.cs:75-90`) and the SPA re-checks (`utils/stringUtils.ts:1-4`); `ChangeDetectionWatchId` must parse as a GUID (`BaseGame.cs:92-106`), so it cannot alter the outbound path; the changedetection client has a 10 s timeout and 64 KB response cap (`Program.cs:176-181`).
- `pdftoppm` is started with `ArgumentList` (no shell). CSP has no `unsafe-inline` for scripts, plus nosniff, `X-Frame-Options: DENY`, COOP/COEP/CORP, Referrer-Policy, Permissions-Policy (`Program.cs:284-311`). CORS is closed unless `CORS_ORIGINS` is set. No cookies → no CSRF on the API.
- JWT: HS256, issuer/audience/lifetime/signing-key validated, 30 s skew (`Program.cs:135-148`); refresh rotation with revoke-on-logout/password-change; forgot-password always returns 200; admin temp passwords from `RandomNumberGenerator`.
- Secrets never echoed to the UI: BGG/changedetection keys blanked (`SettingsService.cs:50,54`), OIDC secret mapped to a bool (`AuthDtoExtensions.cs:14`). E-mail bodies HTML-encoded; MimeKit builds headers structurally. Sentry `SendDefaultPii=false` on both sides; frontend Replay uses default masking.
- Frontend: no `dangerouslySetInnerHTML`; `redirect` search param only feeds TanStack `navigate({to})` (router-relative) — no open redirect found; tokens in `localStorage` (`bgt-auth`) are the standard trade-off given the strict CSP.
- Dependencies: `dotnet list package --vulnerable --include-transitive` → **no vulnerable packages** in Host/Api/Common/Core. `pnpm audit --prod` → 3 (2 high: `js-yaml` via `vite-plugin-svgr`, `browserslist` via `@sentry/vite-plugin`; 1 moderate) — all build-time, not shipped; full audit adds `vitest`/`@vitest/mocker` (moderate, dev).

---

## 3. Endpoint authorization table

Legend: **Anon** = `[AllowAnonymous]`; **Any** = any authenticated role incl. `Reader`; **U/A** = `User,Admin`; **Admin**; "409-off" = returns 409 when `AUTH_ENABLED=false`. With `AUTH_ENABLED=false` every non-409-off row is reachable by anyone on the network as Admin.

| Endpoint | Auth | Rate limit | Note |
|---|---|---|---|
| POST /api/auth/login | Anon | auth | 409-off |
| POST /api/auth/refresh | Anon | auth | 409-off |
| POST /api/auth/forgot-password | Anon | auth | 409-off; timing oracle (F-10) |
| POST /api/auth/reset-password | Anon | auth | 409-off |
| GET /api/auth/status | Anon | — | allowed when auth off |
| POST /api/auth/logout, GET/PUT profile, GET linkable-players, GET/DELETE external-logins | Any | — | ownership checked on unlink |
| POST /api/auth/change-password | Any | auth | |
| POST /api/auth/register, POST /api/auth/reset-password/{userId} | Admin | — | |
| GET /api/auth/oidc/provider | Anon | — | 409-off |
| GET /api/auth/oidc/{p}/login, /callback | Anon | **none** | F-05, F-21 |
| GET /api/auth/oidc/{p}/link, /link-callback | Any | none | F-05 |
| /api/admin/users/* (5), /api/admin/oidc-providers/* (5) | Admin | — | 409-off |
| POST /api/maintenance/reset, /factory-reset | Admin | — | 409-off |
| GET /api/settings | **Anon** | — | F-12 |
| GET /api/settings/version-info, /languages | Anon | — | F-12 |
| PUT /api/settings | Admin | — | **not** 409-off → open when auth disabled |
| GET /api/settings/environment | Any | — | |
| POST /api/update/check | **Any** | none | Reader can trigger DockerHub call |
| POST /api/rag/game/{id}/ask | **Any** | **none** | F-06 |
| GET /api/game, /{id}, /{id}/sessions, /{id}/statistics, /shames, /shames/statistics | Any | — | |
| GET /api/game/{id}/expansions | **Any** | none | Reader triggers outbound BGG call |
| GET /api/game/{id}/price, /prices/wanted | Any | changedetection | |
| POST/PUT /api/game, DELETE /{id}, POST /bgg/search, GET+POST /bgg/import, POST /{id}/expansions, DELETE /{id}/expansion/{eid} | U/A | none | BGG import retries 10×2 s per call (`LazyBoardGameGeekClient.cs:18-19`) |
| GET /api/gamenight | Any | — | |
| POST/PUT /api/gamenight, DELETE /{id}, POST /{id}/send-invites | U/A | none | F-14 |
| PUT /api/gamenight/rsvp, GET /api/gamenight/link/{guid} | Anon | — | known, skipped |
| POST /api/image | U/A | — | 15 MB, re-encoded (F-08) |
| GET /api/manual/game/{gameId}, GET /{id}/download | Any | — | |
| GET /api/manual/{id}/page/{n}/image | **Any** | none | spawns pdftoppm (F-09) |
| POST /api/manual/game/{gameId} (1 GB), POST /{id}/reindex, DELETE /{id} | U/A | none | F-09 |
| GET /api/manual/gamenight/{guid}, …/manual/{id}/download | Anon | — | GUID-gated |
| GET /api/player, /{id}, /{id}/statistics, /{id}/sessions | Any | — | |
| POST/PUT /api/player, DELETE /{id} | U/A | — | |
| GET /api/session/{id} | Any | — | |
| POST/PUT /api/session, DELETE /{id} | U/A | — | |
| GET /api/loans, /{id} | Any | — | |
| POST/PUT /api/loans, PUT /return, DELETE /{id} | U/A | — | |
| GET /api/location | Any | — | |
| POST/PUT /api/location, DELETE /{id} | U/A | — | |
| GET /api/badge, /api/compare/{a}/{b}, /api/count, /api/dashboard/statistics | Any | — | |
| GET /api/health | Anon | — | |
| /images/cover/*, /images/profile/* (static) | **Anon** | — | F-12 |
| /swagger/* | Anon | — | only when `SWAGGER_ENABLED=true` |

No per-user data ownership model exists (single-tenant); the only object-level checks are external-login unlink and the RSVP link GUIDs.

---

## 4. Secrets-scan result

| Item | Location | Tracked / public? | Verdict |
|---|---|---|---|
| Groq API key `gsk_VJec…` | stash `stash@{0}` index commit `f0c6f1fb`, `AppHost.cs:57` | Local only — not on any branch, not on `origin/*` | **Revoke + drop stash** (F-03) |
| Sentry auth token `sntrys_…` | `boardgametracker.client/.env.sentry-build-plugin:5` | Gitignored, never in history; copied into Docker build stage | Fix `.dockerignore`, consider rotate (F-04) |
| Backend Sentry DSN | `Common/Extensions/WebHostBuilderExtensions.cs:29`; echoed in untracked `BoardGameTracker.Host/Logs/*.log` | Public since `86cfaa30` | Info (F-18) |
| Frontend Sentry DSN | CI secret → build arg (`Dockerfile:9-10,26`) | Baked into bundle (by design) | OK |
| `JWT_SECRET=CHANGEME_GENERATE_AT_LEAST_32_CHARACTERS`, `DB_PASSWORD=CHANGEME` | `docker-compose*.yml`, `.env.example` (local `.env` gitignored, same placeholders) | Public placeholders | F-01 (JWT) — DB not exposed (no published port) |
| Dev JWT `your-super-secret-jwt-key-that-is-used-in-dev`, SMTP host/user/from | `BoardGameTracker.AppHost/AppHost.cs:5,65-70` | Public | Info (F-18); SMTP password correctly from user-secrets |
| `ADMIN_PASSWORD=` (empty → `admin`) | `.env.example:13` | Public | F-02 |
| AWS/GitHub/OpenAI/Slack key patterns, private keys | whole tree excl. node_modules/postgres-data/logs/bin/obj | none found | — |
| `appsettings*.json` | Host/AppHost | no secrets (`Jwt:Secret` absent) | OK |
| `.gitignore` / `.dockerignore` coverage | see F-16, F-19 | `postgres-data/`, `images/`, `ollama/`, `manuals/`, `wwwroot/` unignored; `.dockerignore` misses `**/.env.*`, `postgres-data`, `Host/Logs` (case) | Fix |

---

## 5. CI security tooling — what runs, gaps, and would it have caught the findings

Present: `security.yml` (PR only): `dotnet list package --vulnerable`, `pnpm audit --prod --audit-level=high`, Gitleaks (full history). `codeql.yml`: C#, JS/TS, Actions (`security-extended`) on push/PR/weekly. `publish-container.yml`: Trivy image scan (fails on CRITICAL/HIGH) + SBOM, SonarCloud. `security-full-scan.yml` (nightly): builds image, deploys to ephemeral Azure Container App, authenticated ZAP API scan (`fail_action: false`), Docker Scout CVEs, full `pnpm audit` (informational). All actions SHA-pinned, harden-runner in audit mode, `welcome.yml` uses `pull_request_target` **without** checkout (safe).

Gaps:
1. `security.yml` triggers only on `pull_request` → direct pushes to `master` bypass Gitleaks and the NuGet/pnpm audits (`publish-container.yml` runs Trivy only). Add `push: [master]` + `schedule`.
2. Gitleaks sees committed history only → cannot catch F-03 (stash) or F-04 (gitignored file). Add a local `gitleaks protect --staged` pre-commit hook and a `gitleaks` config rule for `gsk_`, `sntrys_`, Sentry DSNs (Gitleaks has no DSN rule → F-18 undetected).
3. `codeql-config.yml:6-10` disables `cs/log-forging` and `cs/storing-sensitive-information` — exactly the queries that would have flagged F-10 and F-11. Re-enable and suppress individual false positives inline.
4. No `actions/dependency-review-action` on PRs → newly introduced vulnerable/unlicensed dependencies are only caught after merge by Renovate/audit.
5. `pnpm audit --prod` excludes build-time deps, so the current highs (`js-yaml`, `browserslist`) never fail a PR; nightly full audit is `continue-on-error`. Acceptable, but bump `vite-plugin-svgr`/`@sentry/vite-plugin` anyway.
6. No IaC/Dockerfile/compose linting (hadolint, `trivy config`, checkov) → would flag tag-only base images, root start, `CHANGEME` secrets in compose (F-01/F-19). Nothing tests startup guards (a unit test asserting placeholder secrets are rejected would have caught F-01).
7. `dotnet-version: "8.x"` in `ci.yml:54`, `security.yml:29`, `publish-container.yml:94` while this branch targets `net10.0` (`a1c074f8` "Update to .NET 10" is not yet on `origin/master`). After merge, restore/vuln-scan will only work if the runner image happens to preinstall .NET 10 — pin `10.x` in the same PR.
8. ZAP nightly is informational (`fail_action: false`, no baseline diff) and runs an Admin token against destructive endpoints (fine on the ephemeral stack) but cannot see configuration-level issues (F-01, F-02, F-20).
9. Published images are unsigned (no cosign / `provenance: true`); SBOM is an artifact only, not attached to the release.
10. `harden-runner` is `egress-policy: audit` everywhere — switch to `block` with an allowlist once egress is known.

Would existing tooling have caught the findings? F-01/F-02/F-05/F-06/F-07/F-08/F-09/F-12/F-13/F-14/F-15/F-16/F-20: **no** (logic/config). F-03: only if it had been committed to a PR branch (it was stashed, so no). F-04: no (gitignored). F-10/F-11: **yes, but the queries are disabled**. F-17/F-18/F-19: no.

---

## 6. Overall assessment
1. The code base is in good shape for a self-hosted app: no injection sinks, strong headers, sane JWT/refresh handling, real upload re-encoding, traversal guards at every disk sink, and clean dependency audits — most classic web bugs are simply absent.
2. The two things that would actually get an exposed instance owned are configuration defaults the app *could* refuse but doesn't: a shipped placeholder `JWT_SECRET` that passes validation (F-01) and a silent `admin`/`admin` seed (F-02). Both are small fixes.
3. The credential-hygiene issues (Groq key in a stash, Sentry token on disk, unignored `postgres-data/`) are local-only today but one careless push away from public; revoke/rotate and tighten ignores now.
4. Resource-abuse from low-privilege accounts is the main *logic* theme: unmetered LLM calls, multi-frame image bombs, unbounded `pdftoppm`, IP-keyed limiters behind proxies, mail relay. Each is an S/M effort.
5. OIDC is correctly recognised as unfinished; the server-side state-binding defect (F-05) should be fixed as part of that work before the SPA is wired to `link-callback`, and CodeQL's disabled queries should be re-enabled so the next plaintext-secret/log-forging regression is caught in CI.
