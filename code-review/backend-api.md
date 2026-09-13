# BoardGameTracker — Web/API layer review (Api, Host, AppHost, auth stack)

Scope reviewed: `BoardGameTracker.Api/**`, `BoardGameTracker.Host/**`, `BoardGameTracker.AppHost/**`, `BoardGameTracker.Core/Auth/**`, `Common/DTOs/Auth`, `Common/Entities/Auth`, `Core/Extensions/ServiceCollectionExtensions.cs`, `Common/Extensions/{WebHostBuilder,LogLevel,Environment}Extensions.cs`, Dockerfile / entrypoint / compose, and the client files needed to confirm CSP and OIDC behaviour. Build: `BoardGameTracker.Api` compiles clean; `Host` compiles but the Debug output copy fails on locked `Core/Common/Api.dll` (a process is holding them) — not a code problem. Read-only review; nothing modified.

All paths below are relative to `C:\Users\mikha\Documents\Repositories\BoardGameTracker\`.

---

## Ranked findings

### F1 — Factory reset silently recreates `admin` / `admin`, ignoring `ADMIN_PASSWORD`
- **Severity:** High  **Category:** Auth / correctness  **Effort:** S  **Confidence:** High
- `BoardGameTracker.Core/Maintenance/MaintenanceSeeder.cs:36-39` calls `DbSeeder.SeedAuthData(_roleManager, _userManager, _logger)` without the 4th argument; `BoardGameTracker.Core/Auth/DbSeeder.cs:10-14, 46-48` defaults `adminPassword` to `null` → password `"admin"`. Startup seeding (`BoardGameTracker.Host/Program.cs:392-395, 439-446`) does pass `environmentProvider.AdminPassword`, so a deployment that set a strong admin password is downgraded to `admin/admin` the moment an admin runs `POST /api/maintenance/factory-reset` (`BoardGameTracker.Api/Controllers/MaintenanceController.cs:29-34`). `ClearSettingsAndAuthAsync` (`Core/Maintenance/MaintenanceRepository.cs:36-42`) deletes all users first, so there is no other account left.
- **Also:** `DbSeeder.cs:51` writes `PasswordHash` directly, bypassing Identity password validators for *any* seeded password (`ADMIN_PASSWORD=a` is accepted). New detail on the known default-admin item.
- **Fix:** `MaintenanceSeeder` already injects `IEnvironmentProvider` — pass `_environmentProvider.AdminPassword`. In `DbSeeder`, use `userManager.CreateAsync(admin, password)` so validators run, and log a loud warning when the default is used. Consider having factory reset revoke the caller's session (return 401 after reset) since the JWT stays valid for up to 60 min.

### F2 — Data Protection keys live inside the container; the "unencrypted keys" warning is silenced
- **Severity:** Medium  **Category:** Startup / config  **Effort:** S  **Confidence:** High
- No `AddDataProtection()` anywhere (grep). `docker-compose.yml:8-11` mounts only `images`, `logs`, `manuals`; container logs confirm keys went to `/home/app/.aspnet/DataProtection-Keys` ("may not be persisted outside of the container", `logs/app-20260810.log`). `Program.cs:43` raises `XmlKeyManager` to `Error`, which hides the "No XML encryptor configured … persisted in unencrypted form" warning. The only consumer is Identity's `DataProtectorTokenProvider` (password-reset tokens: `Core/Auth/AuthService.cs:307, 329`), so every image upgrade/recreate invalidates outstanding "forgot password" emails, and the log line that would explain it is suppressed.
- **Fix:** `builder.Services.AddDataProtection().PersistKeysToDbContext<MainDbContext>().SetApplicationName("boardgametracker")` (package `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`; the DB is already the durable store) — or persist to a mounted `/app/data/keys`. Remove the `XmlKeyManager` override.

### F3 — `UnauthorizedAccessException` is overloaded: I/O permission errors become unlogged 401s, and lockout vs. bad-password is indistinguishable
- **Severity:** Medium  **Category:** Error handling  **Effort:** S  **Confidence:** High
- `Api/Infrastructure/GlobalExceptionHandler.cs:48` maps every `UnauthorizedAccessException` to `401 "Unauthorized"` and `:25` only logs `>= 500`. .NET throws the same exception for file-permission failures (image/manual writes under a wrong `PUID`/volume owner: `entrypoint.sh:26`), so a broken volume surfaces to users as "Unauthorized" with nothing in the log. Separately, `AuthService.cs:60, 67, 73, 97` throw it with i18n keys (`Constants.Errors.AccountLockedOut`, `InvalidRefreshToken`, `Common/Constants.cs:53-55`) that the handler discards — `login.tsx:55` therefore always shows "invalid credentials", even during a lockout.
- **Fix:** Introduce `AuthenticationFailedException(string errorKey) : Exception` in `Common/Exceptions`, throw it from `AuthService`, map it to 401 with `exception.Message` as the title, and let `UnauthorizedAccessException` fall to 500 (logged). Update `WebHostBuilderExtensions.cs:9-17` ignore list accordingly.

### F4 — `DbUpdateException` → 400 swallows transient DB failures during writes, unlogged
- **Severity:** Medium  **Category:** Error handling  **Effort:** S  **Confidence:** High
- `GlobalExceptionHandler.cs:53` maps any `DbUpdateException` to `400 "references data that does not exist…"`. EF wraps *every* `SaveChanges` failure in it — connection drops, deadlocks, disk full — so a DB outage mid-write is reported as a client error and never logged (`:25` logs only 5xx). `Tests/Infrastructure/GlobalExceptionHandlerTests.cs:435-438` enshrines the current behaviour with a bare `new DbUpdateException("FK violation")`.
- **Fix:** `DbUpdateException { InnerException: PostgresException { SqlState: var s } } when s.StartsWith("23")` → 400/409; everything else → 500 + log. Adjust the test to construct a `PostgresException` inner. Also log 4xx at `Debug` so diagnosis is possible.

### F5 — Production CSP/COEP blocks the web font, OIDC provider icons, and the frontend Sentry DSN
- **Severity:** Medium  **Category:** Startup / config (silent prod-only regressions)  **Effort:** S  **Confidence:** High
- `Program.cs:298-300` sets `default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'` and `:292-294` sets `Cross-Origin-Embedder-Policy: require-corp` on every response. Verified consumers that violate it:
  - `boardgametracker.client/src/index.css:1` `@import url("https://fonts.googleapis.com/…Chakra+Petch")` survives the build (`dist/assets/index-DXwAcP03.css`, built today) → blocked by `style-src`; the font files are blocked by the implicit `font-src 'self'`. Production renders the fallback font.
  - `src/routes/_bare/login.tsx:150` renders `<img src={oidcProvider.iconUrl}>` — any external icon URL an admin configures is blocked by `img-src` and COEP.
  - `src/utils/sentry.ts:9-10` initialises `@sentry/react` with `VITE_SENTRY_DSN`, which `.github/workflows/publish-container.yml:303` injects into the image → the ingest POST to `*.ingest.us.sentry.io` is blocked by the implicit `connect-src 'self'`. Frontend Sentry has never reported from a production container.
- **Fix:** Self-host the font (`@fontsource/chakra-petch`) — best for privacy and drops the exception entirely; otherwise add `https://fonts.googleapis.com` to `style-src` and `https://fonts.gstatic.com` to `font-src`. Add `connect-src 'self' https://*.ingest.us.sentry.io` (or use Sentry's `tunnel` through the API). For icons either add the configured origin to `img-src` dynamically or proxy/upload icons. Drop `COEP: require-corp` unless SharedArrayBuffer is actually needed — it adds friction with no benefit here.

### F6 — The "global 30 s HttpClient timeout" only applies to the unnamed client
- **Severity:** Medium  **Category:** Startup / config  **Effort:** S  **Confidence:** High
- `Program.cs:87-93` uses `Configure<HttpClientFactoryOptions>(…)` which configures `Options.DefaultName` (`""`) only. Named clients — BGG `nameof(IBoardGameGeekXmlApi2Client)` (`:255`), the Refit `IDockerHubApi` (`:265-269`), `"ai"` (`:174-175`, intentionally infinite) and `"changedetection"` (`:176-181`, explicit 10 s) — get their own options instance, so BGG and Docker Hub run on `HttpClient`'s 100 s default. Combined with `Core/Games/LazyBoardGameGeekClient.cs:18-19` (`MaxRetries = 10`, `Delay = 2 s`), a single BGG import can run for minutes while the SPA's axios timeout is 30 s (see memory: "false fails-first, works-second").
- **Fix:** `builder.Services.ConfigureHttpClientDefaults(b => b.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30)))` (applies to all named clients; per-client `ConfigureHttpClient` still overrides), and give Docker Hub an explicit short timeout. Consider `AddStandardResilienceHandler()` for Docker Hub / OIDC / changedetection (see packages).

### F7 — `/api/health` has no checks; Docker reports "healthy" with the database down
- **Severity:** Medium  **Category:** Startup / ops  **Effort:** S  **Confidence:** High
- `Program.cs:62` `AddHealthChecks()` registers nothing; `:324` maps it. `Dockerfile:98-99` and `docker-compose.yml:22-27` gate restarts on it, so a container whose DB connection is dead (or whose migrations failed after a restart) is never restarted/flagged.
- **Fix:** `AddHealthChecks().AddDbContextCheck<MainDbContext>()` (package `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`), optionally a second liveness endpoint without the DB check for the container probe, plus `AddNpgSql` if you want a raw connectivity check.

### F8 — OIDC PKCE/state cache is not bound to a flow or a user → account-linking CSRF once the link UI ships
- **Severity:** Medium now (High when linking is exposed in the SPA)  **Category:** Auth  **Effort:** M  **Confidence:** High
- New detail on the known "OIDC state CSRF": `Core/Auth/OidcService.cs:59-94` stores `state → codeVerifier` in a process-wide `IMemoryCache` and the same method serves both `GET …/login` and `GET …/link` (`Api/Controllers/OidcController.cs:38-45, 56-63`). `HandleLinkCallbackAsync` (`OidcService.cs:171-207`, endpoint `OidcController.cs:65-73`) takes the *victim's* userId from the JWT but the attacker's `code`/`state`, exchanges it successfully with the attacker's verifier, and links the attacker's IdP identity to the victim's account → attacker logs in as victim. Today the SPA has no link-callback page (grep: none), so it requires the victim's browser to be driven to that endpoint with its bearer token; it becomes a one-click takeover as soon as linking is wired up.
- **Fix:** Cache a record `{ verifier, flow: Login|Link, userId?, redirectUri }` keyed by state; in the link callback require `flow == Link && userId == currentUser`; in login require `flow == Login`; verify `redirectUri` equals the cached one instead of trusting the query. Bind login state to the browser (short-lived `SameSite=Strict` cookie carrying a hash of state) to close the login-CSRF half.

### F9 — Behind any reverse proxy the default config keys the `auth` limiter on the proxy's IP and never emits HSTS
- **Severity:** Medium  **Category:** Startup / config  **Effort:** S  **Confidence:** High
- `Program.cs:70-71` clears `KnownProxies`/`KnownIPNetworks` (removing the loopback default) and only re-adds `TRUSTED_PROXIES` entries, which `.env.example:15` leaves empty. Behind Traefik/nginx/Caddy (the normal self-host setup), `RemoteIpAddress` is the proxy → the `auth` policy (`:154-161`, 10/min) becomes one shared bucket: one user's five typos + a couple of refreshes lock *everyone* out of login for a minute. `IsHttps` also stays false so the HSTS header (`:302-305`) is never sent. New detail on the known "single global bucket". `AddRateLimiter` also lacks an `OnRejected` writing `Retry-After`.
- **Fix:** Fail loudly at startup when `TRUSTED_PROXIES` is empty and `X-Forwarded-For` is present on the first requests (or log a warning once), document Docker bridge CIDRs (`172.16.0.0/12`) in the compose sample, and add `OnRejected` with `Retry-After`. Consider keying the `auth` limiter on username+IP for `/login`.

### F10 — Exception handler: cancellations become 500s, ProblemDetails bypass the framework, `BadHttpRequestException` unmapped
- **Severity:** Medium  **Category:** Error handling  **Effort:** S  **Confidence:** High (OCE) / Medium (BadHttpRequest path)
- `GlobalExceptionHandler.cs:40-56` has no arm for `OperationCanceledException`; now that `GetGamePrice`/`GetWantedPrices`/`Ask`/`GetManualPageImage` pass `CancellationToken` (`GameController.cs:122, 136`, `RagController.cs:25`, `ManualController.cs:79`), every navigated-away price refresh or aborted RAG question logs an `Error` and writes a 500. `BadHttpRequestException` (carries its own `StatusCode`, e.g. 413 on oversized JSON) falls to 500. `:31` writes `ProblemDetails` by hand — no `traceId`, no `type`, content-type `application/json` instead of `application/problem+json`, and it ignores `AddProblemDetails()` (`Program.cs:65`), so 400s from model validation look different from 400s from the handler. No `Response.HasStarted` guard.
- **Fix:** Map `OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested` → 499/400 without logging; map `BadHttpRequestException b => (b.StatusCode, …)`; inject `IProblemDetailsService` and call `TryWriteAsync` so all ProblemDetails share the same shape/extensions; early-return when `HasStarted`.

### F11 — No request validation beyond implicit non-nullable `[Required]`
- **Severity:** Medium  **Category:** API correctness  **Effort:** M  **Confidence:** High
- No `System.ComponentModel.DataAnnotations` attribute exists in `Common` or `Api` (grep; only `Schema` in `BaseGame.cs`). Concrete gaps: `AskRagCommand.Question` (`Common/DTOs/Commands/AskRagCommand.cs:5`) is unbounded and `RagService.cs:46-49` only rejects whitespace → any `[Authorize]` user (including `Reader`) can send megabyte prompts to a paid LLM; `CreateGameCommand` numeric fields (`CreateGameCommand.cs:9-18`) accept negatives; `RegisterRequest.Email` (`Common/DTOs/Auth/RegisterRequest.cs:4`) unformatted; `CreateOidcProviderRequest.Authority`/endpoint overrides (`OidcModels.cs:11-21`) unvalidated URLs (admin-only, so SSRF impact limited); `[FromQuery] int? count` (`GameController.cs:144`, `PlayerController.cs:77`) accepts negatives; `RegisterRequest.CreatePlayer` + `PlayerId` mutual exclusivity is enforced nowhere.
- **Fix:** Start with DataAnnotations (`[StringLength(2000)]` on `Question`, `[Range]` on ints, `[EmailAddress]`, `[Url]`), which `[ApiController]` already enforces. Move to FluentValidation only for the cross-field rules.

### F12 — `ValidateIdFilter` misses several id-shaped parameters
- **Severity:** Low  **Category:** API correctness  **Effort:** S  **Confidence:** High
- `Api/Infrastructure/ValidateIdFilter.cs:12-13` only checks `int` args named `id` or ending in `Id`. Misses: `CompareController.GetPlayerComparison(int playerOne, int playerTwo)` (`CompareController.cs:20-21`), `page` in `GetManualPageImage` (`ManualController.cs:79`), and every body-carried `Id` (`UpdateGameCommand.Id`, `UpdateSessionCommand.Id`, `UpdateRsvpCommand.Id`…), so `PUT /api/game` with `Id = 0` reaches the service.
- **Fix:** Rename to `playerOneId`/`playerTwoId` (covered automatically), add `page` handling, and either validate command ids with `[Range(1, int.MaxValue)]` or extend the filter to reflect over `IHasId` bodies.

### F13 — Status-code and response-shape inconsistencies
- **Severity:** Low  **Category:** API design  **Effort:** S–M  **Confidence:** High
- Creates return `200` everywhere except `LoanController.CreateLoan` → `201 CreatedAtAction` (`LoansController.cs:46`). Auth mutations return empty `200 Ok()` (`AuthController.cs:54, 92, 110, 119`) while the rest of the API uses `204`. `OidcController.GetProvider` returns `Ok()` with an *empty body* when no provider exists (`OidcController.cs:32`), which JSON clients must special-case. `SearchOnBgg` returns bare `400` when BGG has no such item (`GameController.cs:92`) — that is a 404. `LoansController` routes `{id}` without `:int` (`LoansController.cs:29, 65`) so `/api/loans/abc` is a 400 while `/api/game/abc` is a 404. Controller `NotFound()` (empty) coexists with handler-produced 404 `ProblemDetails`. Unknown `/api/*` GETs fall through to the SPA and return `index.html` with 200 (`Program.cs:370-387`).
- **Fix:** Pick 201 for creates or 200 for all; use `NoContent()` for void mutations; `Ok(provider)` (serialises `null`) or 204; `NotFound()` for missing BGG items; add `:int`; short-circuit `/api` in the SPA branch (`UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api") && GET/HEAD)`).

### F14 — `CancellationToken` plumbing missing on most actions
- **Severity:** Low  **Category:** API correctness  **Effort:** M  **Confidence:** High
- Only four actions accept a token. Long or fan-out endpoints — `GetGameStatistics` (six sequential queries, `GameController.cs:176-196`), `ImportBgg` (BGG "collection preparing" retry loop, `GameController.cs:98-109`), `GetDashboardStatistics`, `GetMenuCounts`, `SendInvites` (SMTP) — keep running after the browser navigates away, burning BGG rate-limit budget and DB time.
- **Fix:** Add `CancellationToken cancellationToken` to the read endpoints and thread it through the service/repository signatures (the Ardalis repositories already accept one). Pair with the F10 mapping so cancellations are quiet.

### F15 — `UpdateController.CheckNow` is reachable by the `Reader` role
- **Severity:** Low  **Category:** Auth  **Effort:** S  **Confidence:** High
- `UpdateController.cs:10, 20-26` is class-level `[Authorize]` only, so a read-only user can trigger an outbound Docker Hub call and rewrite `update_*` config rows. New detail on the known "update/check unthrottled".
- **Fix:** `[Authorize(Roles = Constants.AuthRoles.Admin)]` on the action (the SPA only shows it in admin settings anyway), plus the `changedetection` limiter or a 1-per-minute policy.

### F16 — Sentry ignore-list is exact-type and misses handled BGG/config exceptions; Serilog bypasses Sentry's log provider
- **Severity:** Low  **Category:** Startup / observability  **Effort:** S  **Confidence:** Medium
- `Common/Extensions/WebHostBuilderExtensions.cs:9-17, 33` filter by `GetType()` equality and omit `BggRateLimitException`, `BggCollectionPreparingException`, `BggFeatureDisabledException`, `ConfigMissingException`, `DbUpdateConcurrencyException`, `BoardGameGeekHttpException` — all of which the handler turns into 4xx/5xx *handled* responses. Because Sentry.AspNetCore's middleware captures via `IExceptionHandlerFeature`, every BGG 429/"preparing" round trip lands in the maintainer's Sentry as an error. `builder.Host.UseSerilog()` (`Program.cs:60`) replaces the MEL logger factory, so `_logger.LogError` in the handler never reaches Sentry as a log event. The DSN is hard-coded (`:29`) — acceptable for opt-in telemetry, but worth stating in the docs next to `STATISTICS_ENABLED`.
- **Fix:** Use `IsAssignableFrom` and add the missing types (or filter on the mapped status code < 500 via `SetBeforeSend` reading `HttpContext`), and add `Sentry.Serilog` (`WriteTo.Sentry(...)`) if you want error logs in Sentry.

### F17 — Request logging emits `/api/health` at Information every 30 s; hashed assets lack `immutable`
- **Severity:** Low  **Category:** Startup / ops  **Effort:** S  **Confidence:** High
- `Program.cs:278` `UseSerilogRequestLogging()` uses source `Serilog.AspNetCore.RequestLoggingMiddleware`, which the `Microsoft.AspNetCore` override (`:42`) does not cover; with `LOGLEVEL=info` (the AppHost default, `AppHost.cs:49`) the Docker probe adds ~2.9k lines/day. `SetUnhashedAssetCacheHeaders` (`:401-413`) only handles `/locales`; Vite's hashed `/assets/*` get no `Cache-Control` and revalidate on every load.
- **Fix:** `options.GetLevel = (ctx, _, ex) => ctx.Request.Path.StartsWithSegments("/api/health") ? Verbose : Information;` and set `public,max-age=31536000,immutable` for `/assets`.

### F18 — Cover/profile images are served anonymously regardless of `AUTH_ENABLED`; file names embed the original name
- **Severity:** Low  **Category:** Auth / privacy  **Effort:** M  **Confidence:** High
- `Program.cs:343-353` static-file middleware for `/images/cover` and `/images/profile` runs with no auth; names come from `Common/Extensions/StringExtensions.cs:17-24` (`{original}_{Path.GetRandomFileName()}`), so a player photo is at `/images/profile/John_x4k2lqzv.abc.webp`. Unguessable in practice, but any link shared once is permanent and public.
- **Fix:** If profile photos are sensitive for your users, serve `/images/profile` through an authorised endpoint (or short-lived signed URLs) and drop the original name from the stored file name.

### F19 — JWT/identity configuration duplicated and parsed ad hoc
- **Severity:** Low  **Category:** Startup / auth  **Effort:** S  **Confidence:** High
- Secret sourcing is implemented twice with different whitespace semantics: `Program.cs:112` (`EnvironmentProvider.JwtSecret` null-on-whitespace, `Core/Configuration/EnvironmentProvider.cs:27-34`) vs. `Core/Auth/TokenService.cs:26-28` (raw env var) — a whitespace `JWT_SECRET` with `Jwt__Secret` set validates with one key and signs with another (every login yields a token that 401s). `int.Parse` on `Jwt:AccessTokenExpiryMinutes`/`RefreshTokenExpiryDays` (`TokenService.cs:31, 65, 104`) turns a config typo into a 500 at first login; the code default is 15 min but `Host/appsettings.json:12` says 60. `AddIdentity` (`Program.cs:95-108`) registers unused cookie schemes; `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddSignInManager()` is what this app uses. `RequireUniqueEmail = false` (`:102`) but `OidcService.cs:123` uses `FindByEmailAsync`, which throws `InvalidOperationException` (→ 500) when two local users share an email.
- **Fix:** Bind a `JwtOptions` record via `AddOptions<JwtOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`, inject `IOptions<JwtOptions>` into `TokenService` and the JwtBearer setup, delete the env-var lookups from `TokenService`. Switch to `AddIdentityCore`. Either require unique emails or replace `FindByEmailAsync` with a `Users.Where(...).Take(2)` check.

### F20 — Non-atomic multi-save sequences in auth
- **Severity:** Low  **Category:** Auth / correctness  **Effort:** S  **Confidence:** High
- `AuthService.RefreshAsync` (`Core/Auth/AuthService.cs:103-104`) saves the new token, then revokes the old in a second `SaveChanges`; a failure between leaves two active refresh tokens. `UserAdminService.UpdateUserAsync` (`Core/Auth/UserAdminService.cs:155-157`) does `RemoveFromRolesAsync` then `AddToRoleAsync`; a failure leaves a user with no role. `RefreshToken.Create` (`Common/Entities/Auth/RefreshToken.cs:25-26`) concatenates two `Guid`s — fine entropy-wise, but `RandomNumberGenerator.GetBytes(32)` states the intent (and is what the known "plaintext refresh token" fix should hash).
- **Fix:** Wrap in `IUnitOfWork.BeginTransactionAsync` (already exists) or a single `SaveChanges`; use `RandomNumberGenerator`.

### F21 — Anonymous `/api/settings` exposes deployment details
- **Severity:** Low  **Category:** API / info disclosure  **Effort:** S  **Confidence:** High
- `SettingsController.cs:35-41` is `[AllowAnonymous]` and returns `UIResourceDto` including `PublicUrl` and `ChangeDetectionBaseUrl` (`Core/Settings/SettingsService.cs:45, 51-52`; internal hostname of the changedetection instance) plus feature flags. API keys are correctly masked (`:50, 54`). `SettingsController.GetEnvironment` (`:59-73`) gives any authenticated user (incl. `Reader`) `Port`, `EnvironmentName`, `LogLevel`.
- **Fix:** Split a minimal anonymous "bootstrap" DTO (language, date/time formats, currency, auth/oidc flags) from the authenticated settings payload; restrict `GetEnvironment` to Admin.

### F22 — Manual download without range processing; 1 GiB request limit vs 200 MB per file
- **Severity:** Low  **Category:** API  **Effort:** S  **Confidence:** High
- `ManualController.cs:74` returns `File(stream, contentType, fileName)` without `enableRangeProcessing: true`, so in-browser PDF viewers cannot seek and mobile clients re-download on resume. `:16, 38-39` allow a 1 GiB multipart body (per-file cap is 200 MB at `Core/Manuals/ManualService.cs:21`), buffered to disk by the form feature — fine, but worth a comment in the docs and maybe 512 MB.
- **Fix:** `File(stream, contentType, fileName, enableRangeProcessing: true)`.

### F23 — Login audit logs the submitted username at Information
- **Severity:** Low  **Category:** Auth / privacy  **Effort:** S  **Confidence:** High
- `AuthController.cs:35` logs `request.Username` for every attempt (and `AuthService.cs:59` at Warning for unknown names). Users occasionally type a password in the username box; it then sits in `logs/app-*.log` for 30 days.
- **Fix:** Log at Debug, or log only after `FindByNameAsync` succeeds (log the user id, not the raw input).

### F24 — AppHost hygiene (Groq key, SMTP identity, parameters, readiness)
- **Severity:** Low  **Category:** Aspire  **Effort:** S  **Confidence:** High
- **Groq key:** not in `HEAD` and not on any `origin/*` branch. It exists only in the local stash commit `f0c6f1fb` ("index on feature/download-ai-models") as `env["AI_API_KEY"] = "gsk_VJec…"` (full value in `git log -p --all -- BoardGameTracker.AppHost/AppHost.cs`). Because the working tree once contained it, treat it as compromised: revoke/rotate at Groq, then `git stash drop` (and `git reflog expire --expire=now --all && git gc --prune=now` if you want it gone locally). Current `AppHost.cs:8` correctly reads `Parameters:groq-api-key` from user-secrets.
- **SMTP identity committed:** `AppHost.cs:65-70` hard-codes `mail.smtp2go.com`, username `nobelenoedelMailer`, `noreply@nobelenoedel.be` — not secrets, but paired with any future password leak they are a full credential; move to user-secrets like the password.
- `groq-api-key`/`smtp-password` are read via raw `builder.Configuration` rather than `builder.AddParameter(..., secret: true)` like `jwt-secret` (`:3-8`), so they are invisible in the dashboard and cannot be prompted for.
- `bgt-host` has no `.WithHttpHealthCheck("/api/health")` (`:28-29`), so `bgt-client.WaitFor(backend)` (`:88`) only waits for process start; `WaitFor(ollama)` (`:78`) blocks the backend even when you only want to work on non-RAG features (make it conditional on the same `Ollama:UseGpu`-style switch, or `WaitForStart`).

### F25 — Project-file cruft
- **Severity:** Low  **Category:** Build  **Effort:** S  **Confidence:** High
- `Host/BoardGameTracker.Host.csproj:37-38` references `BoardGameTracker.Core.csproj` twice. `:18` references `Microsoft.AspNetCore.OpenApi` but nothing calls `AddOpenApi`/`MapOpenApi` (grep) — Swashbuckle 10 already brings `Microsoft.OpenApi` v2 (`OpenApiSecuritySchemeReference` in `Program.cs:251` comes from there). `:31` ships `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` in the runtime project.
- **Fix:** Remove the duplicate reference and the two packages.

### F26 — Style: single-line `if` without braces
- **Severity:** Low  **Category:** Conventions  **Effort:** S  **Confidence:** High
- `Api/Infrastructure/AuthDisabledFilter.cs:21-22` (`if (path.EndsWith("/status", …)) return;`) violates the project rule that every `if` body is braced. (Core has more, e.g. `GameService.cs:305-306`, `ConfigRepository.cs:29, 92` — outside this scope.)

---

## AUTH_ENABLED=false — what is exposed anonymously (verified)

`Api/Infrastructure/AuthDisabledMiddleware.cs:18-29` stamps an `Admin` principal on every request before `UseAuthentication` (`Program.cs:320-322`), so all `[Authorize]`/`[Authorize(Roles=…)]` checks pass. Endpoints then split as:

- **409 via `AuthDisabledFilter`:** `AuthController` (except `/status`), `OidcController`, `admin/users`, `admin/oidc-providers`, `maintenance/*`.
- **Fully anonymous:** everything else, including `PUT /api/settings` (stores BGG and changedetection API keys), `POST /api/update/check`, image/manual uploads, `POST /api/rag/**` (LLM spend), BGG imports, deletes, `GET /api/settings/environment`. This matches the docs ("allow anyone to access", `docs/.../environment-variables.mdx:21`) but the docs should say explicitly that "anyone" includes writing third-party API keys and spending LLM credits. CORS is *not* `AllowAnyOrigin` outside Development (`Program.cs:198-204`), so cross-site abuse needs a same-origin foothold — the known "CORS AllowAnyOrigin while auth disabled" item appears resolved.

## Known items — status check (no re-report, one-line each)

- OIDC round trip mis-wired: confirmed. `login.tsx:65` navigates the browser to `GET /api/auth/oidc/{p}/login`, which returns JSON (`OidcController.cs:38-45`); `auth-callback.tsx:10-15` expects `accessToken`/`refreshToken` in the query string, which no API endpoint ever issues (`Callback` returns JSON, `:47-54`). When re-wiring, do not put the refresh token in a URL query (history, Referer, proxy logs) — use a fragment or a one-time code exchange.
- Exception handler "dev-excluded": `app.UseExceptionHandler()` is unconditional at `Program.cs:313` — appears resolved.
- Swagger in prod: gated by `SWAGGER_ENABLED` (default off outside Development, `EnvironmentProvider.cs:42-45`); still unauthenticated when enabled.
- DockerHub client no timeout: still true, and F6 explains why the "global" timeout did not cover it.
- Default admin/admin: F1 adds the factory-reset regression and the validator bypass.
- Refresh-token reuse detection unused: `RefreshToken.ReplacedByToken` is written (`AuthService.cs:104`) but never read.

---

## Quick wins (≤ 30 min each)

1. `MaintenanceSeeder.cs:38` → pass `_environmentProvider.AdminPassword` (F1).
2. `Program.cs`: add `app.UseResponseCompression()` right after `UseForwardedHeaders` — `AddResponseCompression()` (`:185`) is registered but the middleware is never used (grep) — and set `WriteIndented = builder.Environment.IsDevelopment()` (`:455`). Free bandwidth on every JSON response.
3. Replace `Configure<HttpClientFactoryOptions>` (`:87-93`) with `ConfigureHttpClientDefaults(...)` (F6).
4. `AddHealthChecks().AddDbContextCheck<MainDbContext>()` (F7).
5. Map `OperationCanceledException` and `BadHttpRequestException` in `GlobalExceptionHandler` (F10).
6. CSP: add `connect-src` for Sentry and either self-host Chakra Petch or whitelist the two Google font hosts (F5).
7. `[Authorize(Roles = Admin)]` on `UpdateController.CheckNow` (F15).
8. Rename `playerOne/playerTwo` → `playerOneId/playerTwoId` in `CompareController` (F12).
9. `enableRangeProcessing: true` on `ManualController.DownloadManual` (F22).
10. Serilog `GetLevel` to demote `/api/health` (F17).
11. Remove duplicate `Core` ProjectReference and the unused `Microsoft.AspNetCore.OpenApi` / Azure container tools packages (F25).
12. Brace the `if` in `AuthDisabledFilter.cs:21-22` (F26).
13. `OidcController.GetProvider` → `return Ok(provider);` (F13).
14. Revoke the Groq key and drop the stash (F24).

## Missing packages / patterns (only where there is a concrete pain point)

- **`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`** — `/api/health` is meaningless today (F7); one line fixes the Docker probe.
- **`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`** — keys currently die with the container and the warning is muted (F2); DB persistence needs no new volume.
- **`Microsoft.Extensions.Http.Resilience`** — `AddStandardResilienceHandler()` on the Docker Hub, OIDC (unnamed client used in `OidcService.cs:259, 320`) and changedetection clients gives timeout + retry + circuit breaker in one call; today each client has ad-hoc or default timeouts (F6). Keep BGG on its library's own retry to avoid double retries.
- **Options pattern for `Jwt`** (`AddOptions<JwtOptions>().ValidateDataAnnotations().ValidateOnStart()`, in the shared framework) — removes duplicated secret lookup and `int.Parse` 500s (F19).
- **DataAnnotations first, FluentValidation second** — zero request validation exists (F11). DataAnnotations covers 90 % with no dependency; add FluentValidation only for the cross-field rules (`RegisterRequest`, `UpdateRsvpCommand`).
- **Do not add** Asp.Versioning, OutputCache, or a second OpenAPI stack now: there is a single client, no cache-hit pain measured, and Swashbuckle is needed for the `UseResponseInterceptor` token capture (`Program.cs:333-334`). Just delete the dead `Microsoft.AspNetCore.OpenApi` reference.
- **`Sentry.Serilog`** only if you want handler `LogError` events in Sentry; otherwise fix the ignore list (F16).

## Overall assessment

The web layer is thin and mostly disciplined: controllers delegate to services, `[Authorize]`/role gating is consistent (every write is `User|Admin`, admin surfaces are `Admin`), the security headers/CSP effort is above average for a self-hosted app, and the auth-disabled path is deliberately fenced. The highest-value fixes are small and local: the factory-reset admin regression (F1), the exception-handler mappings that hide real failures behind 401/400 (F3, F4), and the production-only CSP breakage that nobody sees in Development (F5). Startup has a handful of "registered but not wired" gaps (compression, health checks, the HttpClient default timeout) that each cost one line. Nothing here needs an architectural change; the OIDC flow binding (F8) is the only item that should be designed before the linking UI ships.
