# BoardGameTracker – Build / Deploy / CI / Docs infrastructure review

Read-only review, 2026-09-09. Repo: `mregni/BoardGameTracker` (branch `feature/236-change-detection`, infra files identical to `origin/master` unless stated). Everything below was verified against the files, `gh` API/run logs, Docker Hub, and by running the published images locally.

Severity scale: High = breaks users/releases or defeats a security control; Medium = wrong/inconsistent but survivable; Low = hygiene. Effort: S < 1 h, M = half day, L = day+.

---

## Ranked findings

### INF-01 · High · Docs · README/docs install snippets do not work with the current code
- `README.md:67-110` and `README.md:129-141` – the compose file and `docker run` have **no `JWT_SECRET`**; `BoardGameTracker.Host/Program.cs:115-119` throws `ArgumentException("JWT_SECRET not set")` at startup (auth is on by default, `EnvironmentProvider.cs:24-25`).
- `docs/src/content/docs/getting-started/docker.mdx:32` uses `JWT_SECRET=CHANGEME` (8 chars) → rejected by `Program.cs:124-128` (min 32).
- `README.md:95` and `docker.mdx:42` use `image: postgres:16`, but the schema requires the `vector` extension (`BoardGameTracker.Core/Datastore/MainDbContext.cs:206`, migration `20260807221846_AddManualRag.cs:17`) → `context.Database.Migrate()` (`Program.cs:419`) fails on stock Postgres → crash-loop. `docker-compose.yml:30` and `quick-start.mdx:54` correctly use `pgvector/pgvector:pg16`; the two other pages contradict them. `docker.mdx` also lacks the `./manuals:/app/manuals` volume.
- `README.md:157-161` documents `STATISTICS` (0/1), `DATE_FORMAT`, `TIME_FORMAT`, `CURRENCY` – `STATISTICS` does not exist (it is `STATISTICS_ENABLED=true|false`, `EnvironmentProvider.cs:15-16`); the other three only work through the undocumented generic override (INF-17).
- `README.md:189` "Backend .NET 8.0" – projects target `net10.0` (`BoardGameTracker.Host.csproj:4`). `README.md:15-16` badge points at `publish-container-dev.yml`, which does not exist (HTTP 404 verified).
- **Fix:** make README's compose block a verbatim include/copy of `docker-compose.yml`; replace the env table with a link to the docs page; fix badge to `publish-container.yml`; update tech stack. **Effort S · Confidence High**

### INF-02 · High · Release/Docs · Next stable release will break every existing install; no upgrade or backup guidance
- Docker Hub `latest` = v0.2.181 pushed 2026-05-21 (runtimeconfig `tfm: net8.0`, verified by running the image); master is 163 commits ahead and `beta` (0.2.344-beta, 2026-08-30, net10.0) already contains the pgvector migration. Every user who followed README/docs onto `postgres:16` will crash-loop on the next `latest`.
- Docs have no upgrade page, no migration notes, no backup/restore page (`grep -ri backup|upgrade docs/src` → only `development.mdx`). The release notes generator (`publish-container.yml:394-426`) emits raw commit subjects, so the breaking change will not be called out either.
- **Fix:** (1) add `getting-started/upgrading.mdx` with the pgvector switch (`docker compose down`, swap image, the data dir is compatible since `pgvector/pgvector:pg16` is Postgres 16) and a "backup first" section (`pg_dump`, plus `images/`, `manuals/` volumes); (2) add a startup pre-check in `Program.cs` before `Migrate()` that runs `SELECT 1 FROM pg_available_extensions WHERE name='vector'` and logs an actionable error; (3) pin a "breaking" callout in the release notes / GitHub release body. **Effort M · Confidence High**

### INF-03 · High · Docker/Runtime · `TZ` is a no-op: the image ships no tzdata
- `Dockerfile:66` base `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` has no `/usr/share/zoneinfo` (verified: 0 entries in the base image, in `uping/boardgametracker:beta` and `:latest`; `date` with `TZ=Europe/Brussels` prints UTC). `Dockerfile:74` only adds `curl su-exec poppler-utils`.
- `BoardGameTracker.Core/Common/DateTimeProvider.cs:15-23` catches `TimeZoneNotFoundException`, writes a `Console.WriteLine` warning (not through Serilog) and silently falls back to UTC. Every install snippet (`docker-compose.yml:21`, `environment-variables.mdx:12`, `README.md:160`) tells users to set `TZ`.
- Impact today is limited (the backend only consumes `IDateTimeProvider.UtcNow` – 11 call sites; `Now`/`ConvertToLocalTime` are unused), but Serilog timestamps are UTC and the RSVP e-mail (`GameNightService.cs:176`) prints the raw `StartDate` with no zone. Any future local-time feature will silently be UTC for everyone.
- **Fix:** `apk add --no-cache tzdata` (≈3 MB) in the runtime stage; log the fallback via `ILogger` at Warning; optionally fail fast when `TZ` is set but unknown. **Effort S · Confidence High**

### INF-04 · High · CI/Security · Deploy pipeline red since 2026-08-27 and the security gate does not gate
- `publish-container.yml:305-360` (`security-scan`, Trivy `exit-code: 1` on CRITICAL/HIGH) fails on every push: last 5 Deploy runs all `failure`. Cause: `CVE-2026-14456` in `libcrypto3 3.5.7-r0` (alpine 3.24.1, fixed in 3.5.8-r0) – i.e. the floating `aspnet:10.0-alpine` base is behind and the Dockerfile never runs `apk upgrade`.
- `build-linux-multiarch` (`publish-container.yml:218-303`) does **not** depend on `security-scan`, so `beta`/versioned tags are pushed regardless (Docker Hub `beta` = 2026-08-30, built by a failed run). The gate is purely cosmetic.
- **Fix:** `RUN apk upgrade --no-cache && apk add --no-cache …` in the runtime stage (`Dockerfile:74`); make the push job `needs: [version, test-and-analyze, security-scan]` or scan the multi-arch build before `push: true` (build with `load`/`--output type=oci`, scan, then push by digest); enable Renovate digest pinning for Docker (`renovate.json:155-157` currently `pinDigests: false`) so base-image bumps are visible PRs. **Effort S-M · Confidence High**

### INF-05 · High · CI/Docs · Docs deploy has been broken since 2026-08-27
- `docs/pnpm-lock.yaml:612` and `:619` both declare `'@napi-rs/wasm-runtime@1.2.3':` (merge-conflict residue from `c75212aa`, "Merge branch 'master' into renovate/all-minor-patch"). `docs.yml:53` `pnpm install --frozen-lockfile` fails with `ERR_PNPM_BROKEN_LOCKFILE … duplicated mapping key (619:3)`; last 3 runs `failure`, last successful deploy 2026-08-24. No docs content changed since, so the live site is not stale *yet*, but the next docs edit will not ship.
- **Fix:** `cd docs && pnpm install` (regenerate), commit; add `docs/**` + `pnpm --dir docs install --frozen-lockfile` to the PR `ci.yml` so lockfile breakage is caught on PRs, not on master. **Effort S · Confidence High**

### INF-06 · Medium · Security/Docker · Sentry auth token persisted in build-stage layers and the GHA cache
- `Dockerfile:9,25` `ARG SENTRY_AUTH_TOKEN` → `ENV SENTRY_AUTH_TOKEN=${SENTRY_AUTH_TOKEN}` in `frontend-build`. It is not in the final image (verified stage separation), but the ENV is recorded in that stage's layer config, which is exported by `cache-to: type=gha,mode=max` (`publish-container.yml:296-297`) and is readable by anyone who can read the Actions cache (any PR from a fork restores it read-only; repo collaborators fully).
- **Fix:** `RUN --mount=type=secret,id=sentry_token SENTRY_AUTH_TOKEN=$(cat /run/secrets/sentry_token) pnpm build` and pass `secrets: sentry_token=${{ secrets.SENTRY_AUTH_TOKEN }}` in `build-push-action`; drop the `ENV`. `VITE_SENTRY_DSN` can stay a build-arg (public value). **Effort S · Confidence High**

### INF-07 · Medium · Compose/Docs · `.env` values are silently ignored by the containers
- `.env.example:1-45` documents 30 variables, but compose interpolation is not container environment. `docker-compose.build.yml:21-33` forwards only `DB_*`, `JWT_SECRET`, `TZ`, `RAG_ENABLED`, `AI_BASE_URL`, `AI_CHAT_MODEL`; `docker-compose.yml` / `docker-compose.gpu.yml` hard-code everything. So `AUTH_ENABLED`, `ADMIN_PASSWORD`, `TRUSTED_PROXIES`, `CORS_ORIGINS`, `SWAGGER_ENABLED`, `AI_PROVIDER`, `AI_API_KEY`, `AI_EMBEDDING_*`, `SMTP_*`, `LOGLEVEL`, `STATISTICS_ENABLED` placed in `.env` never reach the app – yet `email.mdx:35-43` shows exactly that (`title=".env"`).
- `.env.example:41` `DATA_PATH=./data` is referenced nowhere. `docker-compose.build.yml:10` passes build-arg `TZ` that no `ARG` consumes (BuildKit warning).
- **Fix:** add `env_file: .env` to the app service in all three compose files (and keep `environment:` only for compose-internal wiring like `DB_HOST=db`), or an explicit passthrough list; delete `DATA_PATH` and the `TZ` build-arg; state clearly in `email.mdx`/`environment-variables.mdx` where the variables must be set. **Effort S · Confidence High**

### INF-08 · Medium · CI · PR SonarCloud analysis has no TypeScript coverage; 130 duplicated lines between two workflows
- `ci.yml:105` `sonar.javascript.lcov.reportPaths="coverage/lcov.info"` is resolved from the repo root; the file is `boardgametracker.client/coverage/lcov.info` (which `publish-container.yml:146` uses correctly). CI log of run 34245203636: `No LCOV files were found using coverage/lcov.info … No coverage information will be saved because all LCOV files cannot be found.` → the PR quality gate is blind to TS coverage; the master (push) analysis has it, so numbers jump between PR and main.
- `ci.yml:45-199` and `publish-container.yml:85-216` are near-identical copies (the only differences are this bug, the `sed` fix-ups and the PR comment steps) – classic drift.
- **Fix:** correct the path (and keep the `sed` absolute-path rewrite, which is what the second pipeline lacks); extract `test-and-analyze` into `.github/workflows/_test.yml` with `workflow_call` and inputs for "post PR comment". **Effort M · Confidence High**

### INF-09 · Medium · CI · Builds depend on a runner-preinstalled .NET 10 SDK
- `ci.yml:51-54`, `publish-container.yml:91-94`, `security.yml:26-29` install `dotnet-version: "8.x"` while every project is `net10.0` and the AppHost uses `Aspire.AppHost.Sdk/13.4.6`. It only works because `ubuntu-latest` currently ships SDK 10.0.400 (`Detected .NET Core SDK version '10.0.400'` in the CI log). No `global.json` exists.
- **Fix:** `dotnet-version: 10.0.x` + a `global.json` (`rollForward: latestFeature`) so CI, Docker (`Dockerfile:30` already uses `sdk:10.0-alpine`) and local devs agree. **Effort S · Confidence High**

### INF-10 · Medium · Build reproducibility · Floating NuGet versions, no lock file, csproj cruft
- `BoardGameTracker.Host.csproj:15-20` (`10.0.1*`, `10.0.*`), `Core.csproj:15-16,20`, `Common.csproj:15,17`, `Tests.csproj:18-19`. No `packages.lock.json`, no `Directory.Packages.props`. Consequences: Docker `dotnet restore` (`Dockerfile:44`) can resolve a different patch than CI or the developer; Trivy/SBOM results drift between runs; Renovate's EF/Identity grouping rules (`renovate.json:89-103`) never fire because Renovate skips floating ranges.
- `Host.csproj:37-38` references `BoardGameTracker.Core.csproj` twice; `Host.csproj:53-55` links `..\DockerFile` which does not exist on a case-sensitive FS (the file is `Dockerfile`).
- **Fix:** pin exact versions (let Renovate bump them), add `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` and `dotnet restore --locked-mode` in the Dockerfile; remove the duplicate reference and the `DockerFile` link. **Effort S · Confidence High**

### INF-11 · Medium · CI · Stale bot fails every day and is decorative even when it runs
- `stale.yml:56-58` adds label `owner-responded` **before** `stale.yml:69-84` "Ensure labels exist" creates it → `'owner-responded' not found` → exit 1 → `actions/stale` never executes. 12/12 runs since 2026-08-29 are `failure`.
- `stale.yml:100-106` exempts `bug` and `enhancement`; both issue templates auto-apply exactly those labels (`bug_report.md:5`, `feature_request.md:6`) and blank issues are disabled (`ISSUE_TEMPLATE/config.yml:1`), so no template-created issue can ever go stale.
- **Fix:** move "Ensure labels exist" first; decide whether stale should apply to bugs/enhancements (drop them from the exempt list or delete the workflow). **Effort S · Confidence High**

### INF-12 · Medium · CI/Security · Nightly DAST scan has been dead since at least 2026-08-25
- `security-full-scan.yml:75-80` `azure/login` fails with `AADSTS700213: No matching federated identity record found for presented assertion subject 'repo:mregni/BoardGameTracker:ref:refs/heads/master'` on every scheduled run (12/12 failures). The ZAP API scan (`:186-202`) therefore never runs; only Docker Scout and the informational pnpm audit complete. The `Delete Azure resource group` cleanup step also fails (nothing to delete), which is fine.
- **Fix:** in Entra, add a federated credential whose subject is `repo:mregni/BoardGameTracker:ref:refs/heads/master` (or bind the job to `environment: security-scan` and use the `environment:` subject form). Also `fail_action: "false"` (`:193`) means even a working ZAP run never fails the job – consider `fail_action: true` once the rules file is tuned, and pin `zaproxy:stable` (`:191`) to a digest. **Effort S (config) · Confidence High**

### INF-13 · Medium · CI/Security · "Security Scanning" is red on every PR
- `security.yml:84-87` `pnpm audit --audit-level=high --prod` fails with two HIGH browserslist advisories (GHSA-c83g-rgw3-j3cx, GHSA-73wf-gq98-2v4g) reached via `@sentry/vite-plugin` → `@babel/*`. `@sentry/vite-plugin` is a build-time plugin but lives in `dependencies` (`boardgametracker.client/package.json:33`), so `--prod` counts it. 12/12 recent runs `failure` (all Renovate PRs). "Frontend Dependency Scan" is not a required check (ruleset requires only `Backend Dependency Scan` and `Secret Detection`), so PRs merge with a red workflow and nobody sees a real regression.
- **Fix:** move `@sentry/vite-plugin` (and `@tanstack/*-devtools`) to `devDependencies`; add `pnpm.overrides: { browserslist: ">=4.28.7" }` until Sentry bumps; then make the job required. **Effort S · Confidence High**

### INF-14 · Medium · Docker/Repo · Build context and ignore files miss the runtime data directories
- `.dockerignore` excludes none of `docs/`, `postgres-data/`, `ollama/`, `manuals/`, `images/`, `local-packages/`, `BoardGameTracker.Tests/`, `BoardGameTracker.AppHost/`, `.github/` (7 MB incl. a 5 MB PSD), `infra/`, `TestResults`, `coverage`. `.dockerignore:26-28` only excludes `BoardGameTracker.Host/images/{cover,profile}`; `.dockerignore:16` `**/charts` is a Helm leftover (harmless only because matching is case-sensitive vs `Models/Charts`).
- `README.md:219-222` tells contributors to run `docker-compose -f docker-compose.build.yml up --build` from the repo root. After the first run, `./postgres-data` (uid 999, mode 700) and `./ollama` (root) exist in the build context; a non-root Docker user on Linux then gets `error checking context: can't stat …/postgres-data`, and everyone else uploads their whole database to BuildKit on every build.
- `git check-ignore`: only `logs/` is ignored; `postgres-data/`, `ollama/`, `manuals/`, `images/` are not → one `git add -A` away from committing a database or cover images.
- **Fix:** add `/postgres-data/ /ollama/ /manuals/ /images/` (or move compose defaults under `./data/…`) to both `.gitignore` and `.dockerignore`; also ignore `docs/`, tests, AppHost, `.github/`, `infra/`, `*.md` in `.dockerignore`. **Effort S · Confidence High**

### INF-15 · Medium · Docker/entrypoint · `chown -R` on every start, `set -e` aborts silently, PUID/PGID undocumented
- `entrypoint.sh:26` `chown -R "$PUID:$PGID" /app/images /app/logs /app/manuals` runs on every container start – O(files) over the whole cover/manual library (slow on RPi/SD/NAS), and with `set -e` (`:2`) any failure (NFS/CIFS mounts, read-only volumes, `root_squash`) kills the container with no explanatory message. No numeric validation of `PUID`/`PGID` (`addgroup -g abc` → exit). Quoting and the non-root branch (`:9-12`) are otherwise correct (shellcheck not available locally; reviewed manually). No env validation, no migrations, no model download happen here – those are in-app (`Program.cs:390`, `ModelProvisioningBackgroundService.cs`).
- `PUID`/`PGID` (`Dockerfile:91-92`, default 1654) are mentioned nowhere in README/docs.
- **Fix:** `find … ! -user "$PUID" -exec chown …` (only fix what differs) or a one-time marker file; `chown … || echo "WARN: could not chown …"`; validate `case $PUID in ''|*[!0-9]*) …`; document PUID/PGID next to the volumes. **Effort S · Confidence High**

### INF-16 · Medium · Docs/Runtime · Reverse-proxy page omits the two settings that matter behind a proxy
- `proxy.mdx` is Traefik labels only. It never mentions `TRUSTED_PROXIES` (`Program.cs:67-85`): without it forwarded headers are ignored, so (a) the login rate limiter partitions by `RemoteIpAddress` (`Program.cs:154-161`) = the proxy IP → **10 login attempts/min shared by the whole household**, (b) `IsHttps` is false → HSTS (`Program.cs:302-305`) never emitted, (c) audit logs show the proxy IP.
- Nor does any page mention the in-app **Public URL** setting (`AppConfig.PublicUrl`, `ConfigDefaults.cs:18`, default `http://localhost:5444`) used by `PublicUrlBuilder.cs:16-32` to build RSVP and password-reset links → e-mails link to `localhost` until changed (it can also be set as env `PUBLIC_URL`, see INF-17).
- `environment-variables.mdx:14,17` claim defaults `DB_HOST=db`, `DB_USER=dbuser`; code defaults are empty (`DbConnectionProvider.cs:8-9`).
- **Fix:** rewrite `proxy.mdx` as "Behind a reverse proxy": TRUSTED_PROXIES (with the Docker bridge CIDR example `172.16.0.0/12`), Public URL, HTTPS/HSTS, nginx + Caddy + Traefik snippets; fix the two defaults. **Effort S · Confidence High**

### INF-17 · Medium · Config/Docs · Every DB config key is env-overridable, but only the AI ones are documented
- `BoardGameTracker.Core/Configuration/ConfigRepository.cs:20-31` reads `Environment.GetEnvironmentVariable(key.ToUpperInvariant())` before the DB for **every** key in `Constants.cs:21-100`. So `PUBLIC_URL`, `CURRENCY`, `DATE_FORMAT`, `TIME_FORMAT`, `UI_LANGUAGE`, `SHELF_OF_SHAME_ENABLED/MONTHS`, `GAME_NIGHTS_ENABLED`, `RSVP_AUTHENTICATION_ENABLED`, `BGG_API_KEY`, `CHANGEDETECTION_BASE_URL/API_KEY`, `AI_TOP_K`, `UPDATE_TRACK`, `UPDATE_CHECK_ENABLED`, `UPDATE_CHECK_INTERVAL_HOURS` (and the internal `UPDATE_CHECK_LAST_RUN`, `UPDATE_AVAILABLE_VERSION`, …) all work as env vars – undocumented, and the same mechanism is how `AI_*` work (they are *not* dedicated reads; there is no `GetEnvironmentVariable("AI_…")` anywhere).
- Side effects: when a key is overridden by env, the Settings UI still lets users edit/save it (`SetConfigValueAsync` writes the DB, reads keep returning env) – silent no-op; an unparsable value (`AI_EMBEDDING_NUM_GPU=abc`) throws `ConfigMissingException` on every read. Also read but undocumented: `PORT` (`EnvironmentProvider.cs:11`, default 7178, only echoed by `GET /api/settings/environment`, `SettingsController.cs:59-70`), `ENVIRONMENT` (`EnvironmentExtensions.cs:7`), `Jwt__Issuer/Audience/AccessTokenExpiryMinutes/RefreshTokenExpiryDays` (`Program.cs:112-114`, `TokenService.cs:31,65,104`).
- **Fix:** document the override rule once ("any setting key upper-cased") with the key list; have `GET /api/settings` flag env-overridden keys so the UI renders them read-only; drop `PORT` or document it. **Effort S-M · Confidence High**

### INF-18 · Medium · Docker build · arm64 is built under QEMU; no dependency cache mounts; unused ARGs; no digest pins
- `Dockerfile:30` `FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine` has no `--platform=$BUILDPLATFORM`, so for `linux/arm64` the whole .NET SDK runs emulated (the Build Multi-Arch job took 13 m 20 s on 2026-08-30; `timeout-minutes: 60` at `publish-container.yml:222` hints at prior pain). `ARG BUILDPLATFORM/TARGETPLATFORM/TARGETOS` (`Dockerfile:2-4`) are declared and never used. The frontend stage is correctly pinned to amd64 (`Dockerfile:8`).
- Restore layering is right (csproj-only copy before restore, `Dockerfile:35-44`; `pnpm install --frozen-lockfile` before source copy, `:18-19`), but there is no `--mount=type=cache` for `~/.nuget/packages` or the pnpm store, so cache misses re-download everything.
- Base tags `node:22-alpine`, `sdk:10.0-alpine`, `aspnet:10.0-alpine`, `pgvector/pgvector:pg16`, `ollama/ollama:latest` are unpinned and `renovate.json:155-157` disables Docker digest pinning.
- **Fix:** `FROM --platform=$BUILDPLATFORM …sdk:10.0-alpine`, `ARG TARGETARCH`, `dotnet restore -a $TARGETARCH`, `dotnet publish -a $TARGETARCH --no-restore` (cross-compile; runtime stage stays native); add cache mounts; drop dead ARGs; set `pinDigests: true` for `dockerfile` and `docker-compose`. **Effort M · Confidence High**

### INF-19 · Medium · AppHost · Personal SMTP details committed; dev defaults contradict the docs
- `BoardGameTracker.AppHost/AppHost.cs:65-71` hard-codes the maintainer's real relay/username/from-domain (`mail.smtp2go.com`, `nobelenoedelMailer`, `noreply@nobelenoedel.be`) in a public repo. Not a secret (password comes from user-secrets, `:7`), but it is personal infrastructure and every contributor will accidentally use it.
- `AppHost.cs:54-57` defaults the dev stack to `AI_PROVIDER=openai` on Groq (`openai/gpt-oss-120b`) while `development.mdx:67-71` says the assistant "runs entirely locally – Aspire starts an Ollama resource". Without a `Parameters:groq-api-key` secret the chat step fails in dev, although Ollama is still started and `bge-m3` pulled. `AppHost.cs:60-61` set `MANUALS_PATH`/`OLLAMA_PATH`, which the backend never reads. Otherwise the AppHost is wired and usable (`launchSettings.json` profiles match the docs; Postgres/Ollama volumes persistent; `dotnet restore ./BoardGameTracker.sln` in CI includes it and passes).
- **Fix:** move SMTP/AI provider values to `appsettings.Development.json` with placeholders + user-secrets; default `AI_PROVIDER=ollama`; delete the two dead env lines. **Effort S · Confidence High**

### INF-20 · Medium · Docs · Feature coverage gaps and stale visuals
- `user-guide.mdx` is "Coming soon". No page for OIDC (advertised on `index.mdx:59-61`; providers are configured in-app via `OidcProviderService`, zero setup docs), badges/achievements, game nights, loans, shelf of shame, update checks (`UPDATE_TRACK` stable/beta is a real, undocumented feature), price tracking (this branch – not yet on master), backups, upgrades (INF-02).
- Screenshots: `.github/images/games.png` / `game-list.png` last changed 2024-08 (two years and RAG/game-nights/OIDC/loans ago); `.github/images/display.psd` is the largest blob in the repo (5.0 MB) and serves no runtime purpose. `docs/src/assets/hero.png` (1.2 MB, 2026-08) is fine.
- `bugs-features.mdx:12` links `?template=BLANK_ISSUE` while `ISSUE_TEMPLATE/config.yml:1` disables blank issues. `logging.mdx:13` `docker logs -f boardgametracker` assumes a container name compose does not produce (`<dir>-boardgametracker-1`).
- **Fix:** minimal user guide from the settings sidebar; an "Authentication (local + OIDC)" page; a "Backup & upgrade" page; refresh the four README screenshots; move the PSD out of the repo (or LFS); fix the two links. **Effort M-L · Confidence High**

### INF-21 · Low · Compose/RAG · Ollama service wiring
- `docker-compose.gpu.yml:48-59` / `docker-compose.build.yml:57-62`: `ollama/ollama:latest`, no `healthcheck`, the app has no `depends_on: ollama`. `ModelProvisioningBackgroundService.cs:21-22,57` retries 40 × 15 s (10 min) then **gives up until the next app restart** – an Ollama pull/boot slower than 10 min on a small box leaves RAG dead. No `deploy.resources.limits` on the Ollama container. `AI_CHAT_MODEL` default in both compose files is `llama3.2:3b` (`build.yml:33`, `gpu.yml:24`) vs `qwen3:4b` everywhere else (`ConfigDefaults.cs:28`, `rag.mdx:27`, `.env.example:24`).
- **Fix:** healthcheck (`ollama list`) + `depends_on: condition: service_healthy`; unbounded retry with exponential back-off; align the model default. **Effort S · Confidence High**

### INF-22 · Low · Docker/Runtime · Health check and startup robustness
- `Dockerfile:98-99` and all compose healthchecks hard-code `localhost:5444` while `ASPNETCORE_URLS` is an overridable ARG/env (`Dockerfile:70,88`, `docker-compose.build.yml:9,23`) – overriding the port makes the container permanently "unhealthy". `/api/health` (`Program.cs:62,324`) is `AddHealthChecks()` with no checks → "healthy" while the DB is unreachable. `RunDbMigrations` (`Program.cs:390,415-420`) has no retry, so `docker run` users (README) crash-loop until Postgres is up.
- **Fix:** `AddHealthChecks().AddNpgSql(...)`/`AddDbContextCheck<MainDbContext>()`; derive the port from `ASPNETCORE_URLS` in the health check (or drop the ARG); a short bounded retry around `Migrate()`. **Effort S · Confidence High**

### INF-23 · Low · CI · Fork PRs can never be green
- `ci.yml` Sonar steps are skipped without `SONAR_TOKEN` (forks) but `SonarCloud Code Analysis` is a **required** check (ruleset "Pr-review"); `marocchino/sticky-pull-request-comment` (`ci.yml:196`), `EnricoMi/publish-unit-test-result-action` (`:158`) and `actions/labeler` on `pull_request` (`pr.yml:29`) need `pull-requests: write`/`checks: write`, which fork tokens do not get → those steps fail. Only the owner (bypass actors on the ruleset) can merge, which matches reality today, but the README invites contributions.
- **Fix:** `if: github.event.pull_request.head.repo.full_name == github.repository` guards on the write steps, and make the Sonar check non-required or run labeler via `pull_request_target`. **Effort S · Confidence High**

### INF-24 · Low · Supply chain · No SBOM/provenance on the published image; inconsistent attestations
- `publish-container.yml:287-303` uses `build-push-action` defaults (min provenance, no `sbom: true`). The CycloneDX SBOM (`:362-374`) is generated from a separate amd64 `:scan` build and kept 90 days as an artifact – not attached to the image or release. Docker Hub `latest` shows two `unknown/unknown` attestation manifests, `beta` none (security-full-scan uses `provenance: false`, `:59`; Deploy does not) – confusing for users browsing tags.
- OCI labels are set via `metadata-action` (`:265-285`) – good; there are no index `annotations`.
- **Fix:** `provenance: mode=max`, `sbom: true`, `annotations: ${{ steps.meta.outputs.annotations }}`; upload `sbom.json` to the GitHub release. **Effort S · Confidence Medium**

### INF-25 · Low · Repo hygiene
- Missing: `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, `CODE_OF_CONDUCT.md`, `PULL_REQUEST_TEMPLATE.md`, `.editorconfig` (Biome/C# style is enforced only by CI). `.gitignore:654-656` `/*.md` + `!/README.md` + `!/CONTRIBUTING.md` means a future `SECURITY.md`/`CHANGELOG.md` would be silently untracked.
- `CODEOWNERS` fine (`* @mregni`); `.vscode/settings.json` tracked (fine); `.idea`, `*.DotSettings.user`, `OIDC_PLAN.md`, `local-packages/` correctly ignored. `local-packages/BoardGamer.BoardGameGeek.0.10.1-localfix.nupkg` is unused (`nuget.config:4-5` clears sources to nuget.org; csproj pins 0.10.0). `LICENSE` MIT © 2023 – fine, no per-file headers needed.
- Leftovers: remote `dev` branch (216 commits behind, last 2026-04-19) still protected by ruleset "Protect defauls" (typo); `release-please--branches--dev`; unused `SEMGREP_APP_TOKEN` secret; `codeql.yml:18` swift/macos conditional that can never match; stale dynamic "CodeQL" default-setup workflow entry (last run 2026-03-30, state `not-configured`) – harmless; `.github/codeql/codeql-config.yml:7-10` globally suppresses `cs/log-forging` and `cs/storing-sensitive-information` (accept, but document why).
- Local only: this clone's `.git` has 18 orphan `.pack` files without `.idx` (`size-pack: 3.30 GiB`) → `git gc --prune=now` / delete the orphan packs.
- **Effort S each · Confidence High**

### INF-26 · Low · i18n/Crowdin · Contributor guidance and fallback drift
- `welcome.yml:44-45` tells first-time contributors to edit "all three locale files (en-US.json, nl-NL.json, nl-BE.json)"; the actual layout is per-namespace dirs `public/locales/{base,en-US,es-ES,nl-BE,nl-NL}/*.json` and Crowdin (`crowdin.yml:2-3`) owns everything except `base`. `es-ES` is shipped in `wwwroot` but not in `supportedLngs` (`i18n.ts:46`). `en-US/error.json` already differs from `base/error.json` (9 lines) while production loads `/locales/{{lng}}/` with `fallbackLng: "en-US"` (`i18n.ts:35-47`) → keys added to `base` render as raw keys until Crowdin syncs `en-US`.
- **Fix:** message → "edit only `locales/base/*.json`"; `fallbackLng: ["en-US", "base"]` or serve `base` as `en-US`; either add `es-ES` to `supportedLngs` or exclude it from the build. **Effort S · Confidence High**

### INF-27 · Low · Nits (Dockerfile / workflows / docs)
- `Dockerfile:1` starts with a UTF-8 BOM (`ef bb bf`) – BuildKit tolerates it, hadolint/older parsers do not. 4 `.pdb` files ship in the runtime image (`/app` 63.8 MB, `wwwroot` 24.9 MB, ≈85 MB compressed per arch – acceptable; `DebugType=none` for Release saves a few MB). Globalization-invariant mode is the right call (saves ICU); see INF-28.
- `publish-container.yml:24-25` `cancel-in-progress: false` still cancels *queued* runs (3 cancelled on 2026-08-27) → beta numbers skip (0.2.314 → 0.2.317 → 0.2.333) – harmless. `Dockerfile:57` strips `-beta` before `/p:Version`, so `InformationalVersion` (`Directory.Build.props:7`) loses the pre-release tag; pass `/p:InformationalVersion=${VERSION}` separately. Release notes (`:394-426`) are raw commit subjects incl. Renovate noise – `generate_release_notes: true` or git-cliff.
- `pr.yml:6` includes `edited` → every Renovate PR triggers two runs in the same concurrency group and one is cancelled (all the "cancelled" PR Quality rows) – drop `edited` or key the group on `run_id`.
- `harden-runner` is `egress-policy: audit` in all 9 workflows – informational only; switch the deterministic ones (`pr.yml`, `stale.yml`, `welcome.yml`, `docs.yml`) to `block`. `pnpm/action-setup@b906aff` still targets Node 20 (deprecation warning in every run) – bump.
- `README.md:119` `docker-compose` (v1) vs `docker compose` elsewhere. arm/v7 images stopped in Jan 2026 (Docker Hub history) – state "amd64/arm64 only" in the docs for 32-bit Pi users.
- Positive: all 34 distinct actions across 9 workflows are SHA-pinned with version comments (verified by grep); every job declares least-privilege `permissions`; `welcome.yml` uses `pull_request_target` safely (no checkout); required-check names in the ruleset match job names exactly (`Test and SonarCloud Analysis`, `Backend Dependency Scan`, `Secret Detection`, `Analyze (actions|csharp|javascript-typescript)`); `entrypoint.sh` LF-forced via `.gitattributes`; secret scanning + push protection enabled on the repo.

### INF-28 · Info · Globalization · `InvariantGlobalization=true` audit – no culture bug found
- `BoardGameTracker.Host.csproj:7`. Parsing: `ChangeDetectionSnapshotParser.cs:9,39-76` normalises `,`→`.` and parses with `NumberStyles.Number` + `InvariantCulture` – correct and deterministic; `UpdateService.cs:53` invariant; `TypeConverter.cs:15` int only; BGG decimals are parsed inside `BoardGamer.BoardGameGeek` where invariant culture matches BGG's `.` decimals. Formatting: `Price.cs:19` `ToString("C")` would render `¤` under invariant culture but is unused; `GameNightService.cs:176` formats the RSVP e-mail date in invariant English (`dd MMMM yyyy 'at' HH:mm`) and in UTC – an i18n gap, not a globalization bug. Frontend does all locale formatting client-side. Keep invariant mode; add tzdata (INF-03).

---

## Env var cross-check table

Legend – Compose: `Y` = passed in `docker-compose.yml`; `B` = only in `docker-compose.build.yml`; `G` = only in `docker-compose.gpu.yml`; `–` = none. Docs: `EV` = `environment-variables.mdx`, `R` = README, `.ex` = `.env.example`.

| Variable | Read where | Documented where | Compose |
|---|---|---|---|
| `DB_HOST` | `DbConnectionProvider.cs:8` | EV:14 (wrong default `db`), R:152, .ex | Y/B/G |
| `DB_PORT` | `DbConnectionProvider.cs:12`, `Program.cs:363` | EV:15, R:153, .ex | Y/B/G |
| `DB_USER` | `DbConnectionProvider.cs:9` | EV:17 (wrong default `dbuser`), R:154, .ex | Y/B/G |
| `DB_PASSWORD` | `DbConnectionProvider.cs:10` | EV:18, R:155, .ex | Y/B/G |
| `DB_NAME` | `DbConnectionProvider.cs:11` | EV:16, R:156, .ex | Y/B/G |
| `JWT_SECRET` | `EnvironmentProvider.cs:31`, `TokenService.cs:26`, `Program.cs:112` | EV:13, docker.mdx (too short), quick-start; **missing in README** | Y/B/G |
| `AUTH_ENABLED` | `EnvironmentProvider.cs:25` | EV:21, .ex:9 | – |
| `ADMIN_PASSWORD` | `EnvironmentProvider.cs:36` | EV:22, .ex:13 | – |
| `TRUSTED_PROXIES` | `EnvironmentProvider.cs:38`, `Program.cs:73` | EV:23, .ex:15 (not in proxy.mdx) | – |
| `CORS_ORIGINS` | `EnvironmentProvider.cs:40`, `Program.cs:186` | EV:24, .ex:16 | – |
| `SWAGGER_ENABLED` | `EnvironmentProvider.cs:43`, `Program.cs:283,328` | EV:25, .ex:17 | – |
| `LOGLEVEL` | `LogLevelExtensions.cs:9` | EV:19, logging.mdx:6 (not in .ex) | – |
| `STATISTICS_ENABLED` | `EnvironmentProvider.cs:16`, `WebHostBuilderExtensions.cs:21`, `Program.cs:338,360` | EV:20, logging.mdx:18; README names it `STATISTICS` (wrong) | – |
| `TZ` | `DateTimeProvider.cs:11-12`, `Program.cs:362` | EV:12, R:160, .ex:7 | Y/B/G (**no-op: no tzdata**) |
| `RAG_ENABLED` | `EnvironmentProvider.cs:19`, `ServiceCollectionExtensions.cs:87` | EV:51, rag.mdx, .ex:19 | B/G |
| `AI_PROVIDER` | generic `ConfigRepository.cs:22` (`ai_provider`) | EV:52, rag.mdx, .ex:22 | – |
| `AI_BASE_URL` | generic (`ai_base_url`) | EV:53, .ex:23 | B/G |
| `AI_CHAT_MODEL` | generic (`ai_chat_model`) | EV:54, .ex:24 (`qwen3:4b`) | B/G (default `llama3.2:3b` – mismatch) |
| `AI_API_KEY` | generic (`ai_api_key`) | EV:55, .ex:25 | – |
| `AI_EMBEDDING_BASE_URL` | generic | EV:56, .ex:27 | – |
| `AI_EMBEDDING_NUM_GPU` | generic | EV:57, .ex:30 | – |
| `AI_TOP_K` | generic (`ai_top_k`) | **undocumented** | – |
| `SMTP_HOST/PORT/USERNAME/PASSWORD/USE_SSL/FROM_ADDRESS/FROM_NAME` | `EnvironmentProvider.cs:52-67` | EV:33-39, email.mdx, .ex:32-38 | – (email.mdx shows `.env`, which compose ignores) |
| `PUBLIC_URL` | generic (`public_url`, `PublicUrlBuilder.cs:30`) | **undocumented** (in-app setting only) | – |
| `CURRENCY`, `DATE_FORMAT`, `TIME_FORMAT`, `UI_LANGUAGE` | generic | README:158-161 lists three as dedicated vars (they are not) | – |
| `SHELF_OF_SHAME_ENABLED/MONTHS`, `GAME_NIGHTS_ENABLED`, `RSVP_AUTHENTICATION_ENABLED` | generic | undocumented | – |
| `BGG_API_KEY`, `CHANGEDETECTION_BASE_URL`, `CHANGEDETECTION_API_KEY` | generic | undocumented | – |
| `UPDATE_TRACK`, `UPDATE_CHECK_ENABLED`, `UPDATE_CHECK_INTERVAL_HOURS` (+ internal `UPDATE_CHECK_ERROR/LAST_RUN`, `UPDATE_AVAILABLE*`) | generic | undocumented | – |
| `PORT` | `EnvironmentProvider.cs:11` (→ `SettingsController.cs:68` only; default 7178) | undocumented | – |
| `ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` | `EnvironmentExtensions.cs:6-7`, `Program.cs:358` | undocumented | B (`ASPNETCORE_ENVIRONMENT`), Dockerfile:86 |
| `ASPNETCORE_URLS` | Kestrel | undocumented | Dockerfile:88, B:23 |
| `ASPNETCORE_HTTP_PORTS` | `Program.cs:361` (log only, never set) | – | – |
| `Jwt__Secret/Issuer/Audience/AccessTokenExpiryMinutes/RefreshTokenExpiryDays` | `Program.cs:112-114`, `TokenService.cs:31,65,104` | undocumented (appsettings.json:9-14) | – |
| `PUID`, `PGID` | `entrypoint.sh:4-5` (Dockerfile:91-92 default 1654) | **undocumented** | – |
| `STATISTICS` | **not read** | README:157 | – |
| `DATA_PATH` | **not used anywhere** | .ex:41 | – |
| `IMAGE_PATH`, `LOG_PATH`, `DB_PATH`, `MANUALS_PATH`, `OLLAMA_PATH` | compose interpolation only (B:16-18,45,62); `AppHost.cs:60-61` also sets `MANUALS_PATH`/`OLLAMA_PATH` as app env – never read | .ex:40-45 | B |
| `SENTRY_AUTH_TOKEN`, `VITE_SENTRY_DSN` | build-args (`Dockerfile:9-10,25-26`, `sentry.ts:10`) | undocumented (CI only) | – |
| `DOTNET_EnableDiagnostics`, `COREPACK_ENABLE_DOWNLOAD_PROMPT` | Dockerfile:87,14 | – | – |

---

## CI run health (from `gh`, 2026-09-09)

| Workflow | Recent result | Root cause |
|---|---|---|
| CI (PR) | ✅ all green | – (but no TS coverage to Sonar, INF-08; runs on runner-preinstalled SDK 10, INF-09) |
| CodeQL Advanced | ✅ green | – |
| PR Quality | ✅ (each PR shows one `cancelled` twin) | `edited`+`synchronize` in one concurrency group (INF-27) |
| Welcome New Contributors | ✅ | – |
| **Security Scanning (PR)** | ❌ 12/12 since ≥2026-09-02 | `Frontend Dependency Scan`: 2 HIGH browserslist advisories via `@sentry/vite-plugin` (INF-13); not a required check |
| **Deploy (publish-container)** | ❌ 5/5 since 2026-08-27 (3 more `cancelled` = superseded queue) | `Security Scan` Trivy HIGH `CVE-2026-14456` libcrypto3; images still pushed (INF-04) |
| **Deploy Documentation** | ❌ 3/3 since 2026-08-27 (last success 08-24) | broken `docs/pnpm-lock.yaml` duplicate key (INF-05) |
| **Stale Issues & PRs** | ❌ 12/12 daily | label used before it is created (INF-11) |
| **Security Full Scan (Nightly)** | ❌ 12/12 since ≥2026-08-25 | Azure OIDC federated-credential subject mismatch; ZAP never runs (INF-12) |
| Dependabot Updates / CodeQL (dynamic) | inactive placeholders | – |

Release state: `latest` = v0.2.181 (2026-05-21, net8.0); `beta` = 0.2.344-beta (2026-08-30, net10.0); 163 unreleased commits on master; images are amd64 + arm64 (arm/v7 dropped Jan 2026). Rulesets: `Pr-review` (0 approvals, owner bypass, 7 required checks – names consistent), `Protect defauls` (also protects the dead `dev` branch). Repo: auto-merge on, secret scanning + push protection on, Dependabot security updates off (Renovate covers it).

---

## Quick wins (≤ 1 h each, in order of payoff)

1. `RUN apk upgrade --no-cache && apk add --no-cache curl su-exec poppler-utils tzdata` in `Dockerfile:74` → clears the Trivy HIGH, fixes `TZ`, un-reds Deploy (INF-03, INF-04).
2. Regenerate `docs/pnpm-lock.yaml` → docs deploy green (INF-05).
3. Swap the two steps in `stale.yml` (INF-11).
4. Fix `ci.yml:105` lcov path (INF-08).
5. `dotnet-version: 10.0.x` in three workflows + `global.json` (INF-09).
6. Move `@sentry/vite-plugin` to devDependencies (INF-13).
7. Add `env_file: .env` to the app service in the three compose files; delete `DATA_PATH` and the `TZ` build-arg (INF-07).
8. README: add `JWT_SECRET`, switch `postgres:16` → `pgvector/pgvector:pg16`, fix badge, delete the stale env table (INF-01); same for `docker.mdx`.
9. `.gitignore`/`.dockerignore`: `/postgres-data/ /ollama/ /manuals/ /images/ docs/ .github/ BoardGameTracker.Tests/ BoardGameTracker.AppHost/` (INF-14).
10. `RUN --mount=type=secret` for the Sentry token (INF-06).
11. Fix the Entra federated-credential subject (INF-12) – config only.
12. Add a federated "Upgrading" note + pgvector pre-check before the next stable release (INF-02).

---

## Overall assessment

1. The build/CI skeleton is above average for a solo project: clean multi-stage Dockerfile with correct restore layering, PUID/PGID drop, healthchecks, 34/34 SHA-pinned actions, least-privilege permissions, Trivy + Scout + CodeQL + gitleaks + ZAP, multi-arch, Renovate with sensible grouping.
2. But five of nine workflows have been red for two weeks (Deploy, Docs, Stale, Nightly DAST, PR Security) and nobody is blocked by it – the gates that exist do not gate, so the signal has been lost; fixing that is mostly one-line changes.
3. The shipped image quietly ignores `TZ` (no tzdata) and the documented install paths (README, `docker.mdx`) no longer start the app at all (missing/short `JWT_SECRET`, wrong Postgres image); the next stable release will additionally crash-loop every existing `postgres:16` install with no upgrade note.
4. Configuration documentation is out of sync with the code in both directions: `.env` values that compose never forwards, a README env table that is half fiction, and a powerful generic env-override mechanism (`PUBLIC_URL`, `UPDATE_TRACK`, …) that nobody documents.
5. Priorities: (a) INF-01/02/03/04/05 before cutting the next stable release, (b) INF-06/07/08/09 for CI trustworthiness, (c) the docs/hygiene items can ride along with the release.
