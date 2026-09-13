# BoardGameTracker dependency audit (2026-09-09)

Scope: `BoardGameTracker.sln` (6 .NET 10 projects), `boardgametracker.client` (pnpm 10.34.5, Vite 8), `docs/` (pnpm, Astro 7). Branch `feature/236-change-detection`. Read-only; only `dotnet restore`/`build` (obj/bin) and read-only pnpm commands were run. Raw outputs live in the scratchpad (`nuget-report.txt`, `nuget-report2.txt`, `pnpm-report.txt`, `build-full.txt`, `build-host.txt`).

Local toolchain used: .NET SDK 10.0.112, Node v22.21.1, pnpm 10.34.5.

---

## TL;DR: the four things that are actually broken right now

1. **`docs/pnpm-lock.yaml` on `master` is corrupt** (5 duplicated YAML keys from merge `c75212aa`, 2026-08-24). Every pnpm command in `docs/` dies with `ERR_PNPM_BROKEN_LOCKFILE`, so `docs.yml` (`pnpm install --frozen-lockfile`) cannot deploy. Fix: `cd docs && pnpm install` to regenerate, commit.
2. **Frontend audit gates are red**: `pnpm audit --prod` = 2 high (browserslist) → `security.yml` (`--audit-level=high --prod`) fails; `pnpm audit` = 4 high / 3 moderate → `security-full-scan.yml` (`--audit-level=moderate`) fails. All are transitive, all already satisfiable by declared ranges; a lockfile refresh + `vitest` 4.1.11 clears every one. The prod-side hits exist only because `@sentry/vite-plugin` (build-time) sits in `dependencies`.
3. **CI installs .NET SDK `8.x` for a `net10.0` solution** (`ci.yml`, `publish-container.yml`, `security.yml`) and Node `20.x` (EOL 2026-04-30) while the Dockerfile builds on `node:22-alpine`. It only works because `ubuntu-latest` happens to preinstall SDK 10. No `global.json`, no `.nvmrc`, no `engines`.
4. **`security.yml`'s NuGet vulnerability check has a blind spot**: `dotnet list BoardGameTracker.sln package --vulnerable` aborts enumeration at the `.esproj` and silently never reports `BoardGameTracker.Tests` or `BoardGameTracker.AppHost` (the sln order is Host, Api, Common, Core, *esproj*, Tests, AppHost). The grep-based verdict still passes because the first four projects print "has no vulnerable packages". Per-project runs are clean today, but the gate is not testing what it thinks it is.

---

## (a) Vulnerabilities

### NuGet — none (all 6 projects, `--include-transitive`)

```
### VULNERABLE (transitive)   (sln-level: only 4 projects reported, see TL;DR #4)
The given project `BoardGameTracker.Host`   has no vulnerable packages given the current sources.
The given project `BoardGameTracker.Api`    has no vulnerable packages given the current sources.
The given project `BoardGameTracker.Common` has no vulnerable packages given the current sources.
The given project `BoardGameTracker.Core`   has no vulnerable packages given the current sources.
The project `...\boardgametracker.client.esproj` uses package.config for NuGet packages, while the command works only with package reference projects.
### BoardGameTracker.Tests   VULNERABLE transitive -> has no vulnerable packages
### BoardGameTracker.AppHost VULNERABLE transitive -> has no vulnerable packages
### DEPRECATED -> none in any project
```

### npm — `boardgametracker.client` (7 findings, all transitive except vitest)

| Package | Installed | Advisory | Severity | Reached via | Fix |
|---|---|---|---|---|---|
| `browserslist` | 4.28.2 | GHSA-c83g-rgw3-j3cx (unbounded cache growth / OOM) | high | `@sentry/vite-plugin>@sentry/bundler-plugins>@babel/core>@babel/helper-compilation-targets` (**prod-classified**), `@tanstack/router-plugin>@babel/core>…` (dev) | ≥4.28.7. Declared range is `^4.24.0` → `pnpm update browserslist` (no override needed) |
| `browserslist` | 4.28.2 | GHSA-73wf-gq98-2v4g (prototype write via custom stats) | high | same | same |
| `baseline-browser-mapping` | 2.10.31 | GHSA-w5vr-8v7q-w6rv (DoS on invalid input) | moderate | `…>browserslist>baseline-browser-mapping` | ≥2.11.0. Declared range `^2.10.12` → `pnpm update baseline-browser-mapping` |
| `js-yaml` | 4.3.0 | GHSA-5p4m-2wfm-xmqj (CVE-2026-59870, quadratic CPU in `!!omap`) | high | `vite-plugin-svgr>@svgr/core>cosmiconfig>js-yaml` (dev) | ≥4.3.1. The existing override `js-yaml@>=4.0.0 <4.3.0 → ^4.3.0` is stale; `cosmiconfig` declares `^4.1.0` so `pnpm update js-yaml` suffices |
| `js-yaml` | 4.3.0 | GHSA-2883-xcg3-v3hh (`maxTotalMergeKeys` CPU) | high | same | ≥4.3.2 |
| `vitest` | 4.1.10 | GHSA-82fw-gwwq-j7x9 (path traversal via `@vitest/mocker` redirect mock) | moderate | direct devDependency | 4.1.11 (patch) |
| `@vitest/mocker` | 4.1.10 | same advisory | moderate | `vitest>@vitest/mocker` | comes with vitest 4.1.11 |

Raw (trimmed):

```
### CLIENT pnpm audit --prod
3 vulnerabilities found   Severity: 1 moderate | 2 high
### CLIENT pnpm audit
7 vulnerabilities found   Severity: 3 moderate | 4 high
```

Practical impact: all of these are build/test-time tools (Babel, SVGR config loader, Vitest). None ship to the browser bundle. Exposure is CI/dev machines only, but the gates fail, and a red security gate that everyone learns to ignore is worse than none.

### npm — `docs/`

Cannot be audited: lockfile is broken (see (e)/(g)). Registry check of the four direct deps: `astro` 7.2.6 → 7.3.2, `@astrojs/starlight` 0.41.8 → 0.42.0, `sharp` 0.35.3 → 0.35.4, `@fontsource/chakra-petch` 5.3.0 (current).

---

## (b) Outdated majors worth doing (with risk notes)

### NuGet (`dotnet list package --outdated`, per project)

```
Host:    Refit 10.2.0 -> 15.2.0 | Refit.HttpClientFactory 10.2.0 -> 15.2.0 | Serilog.AspNetCore 9.0.0 -> 10.0.0 | Serilog.Extensions.Hosting 9.0.0 -> 10.0.0
Common:  Sentry.AspNetCore 6.9.0 -> 6.10.0 | Serilog.AspNetCore 9.0.0 -> 10.0.0
Core:    Microsoft.Extensions.AI 10.9.0 -> 10.10.0 | Microsoft.Extensions.AI.OpenAI 10.9.0 -> 10.10.0 | Refit 10.2.0 -> 15.2.0 | SixLabors.ImageSharp 3.1.12 -> 4.1.1
Tests:   coverlet.collector 8.0.1 -> 10.0.1 | Microsoft.NET.Test.Sdk 17.14.1 -> 18.10.0 | xunit.runner.visualstudio 3.1.5 -> 4.0.0 | xunit.v3 3.2.2 -> 4.0.0
AppHost: Aspire.Hosting.JavaScript 13.4.6 -> 13.5.3 | Aspire.Hosting.PostgreSQL 13.4.6 -> 13.5.3 | CommunityToolkit.Aspire.Hosting.Ollama 13.4.0 -> 13.5.0
Api:     no packages
```

| Upgrade | Why / risk |
|---|---|
| **Serilog.AspNetCore 9 → 10** (+ drop `Serilog.Extensions.Hosting`, it is transitive) | Low risk; aligns with .NET 10. Used in `Common/Extensions/WebHostBuilderExtensions.cs`, `Host/Program.cs` (`UseSerilog`, `WriteTo.File`). |
| **Refit 10.2 → 15.2** | Medium: Refit ≥8 relies on its source generator; API surface here is tiny (`Core/DockerHub/IDockerHubApi.cs`, `AddRefitClient<IDockerHubApi>()` in Program.cs:265). Check the generator's Roslyn floor against SDK 10.0.112 (see CS9057 story in (f) — OllamaSharp already trips this). |
| **SixLabors.ImageSharp 3.1 → 4.1** | Medium: API breaks in v4 (`Image.Load` overloads, `Configuration` → `DecoderOptions`, metadata). Three call sites (`Core/Disk/DiskProvider.cs`, `Core/Images/ImageService.cs`). Licence unchanged for OSS (Six Labors Split License). |
| **xunit.v3 3.2 → 4.0, xunit.runner.visualstudio 3.1 → 4.0, Microsoft.NET.Test.Sdk 17 → 18, coverlet.collector 8 → 10** | Do as one bundle (renovate already groups "Backend test packages"). xunit.v3 4.0 pushes harder toward Microsoft.Testing.Platform (`xunit.v3.core.mtp-v1` is already in the transitive graph). `ci.yml` uses `dotnet test --collect "XPlat Code Coverage"` (VSTest/coverlet mode) — that path still works with `Microsoft.NET.Test.Sdk`, but if you flip to MTP you must switch coverage to `Microsoft.Testing.Extensions.CodeCoverage` and change the `dotnet test` invocation. |
| **Aspire 13.4.6 → 13.5.3** | Low; also bump the `<Project Sdk="Aspire.AppHost.Sdk/13.4.6">` attribute in `BoardGameTracker.AppHost.csproj` in the same PR (verify renovate proposes it; if it does not, add a regex/custom manager). |
| Microsoft.Extensions.AI 10.9 → 10.10, Sentry 6.9 → 6.10 | Minor, automerge candidates. |

### npm (`pnpm outdated`, majors)

```
@tanstack/react-table        8.21.3   -> 9.2.4
@testing-library/jest-dom    6.9.1    -> 7.0.1
@types/node                  20.19.43 -> 22.20.2
@types/react / react-dom     18.3.x   -> 19.3.0
react / react-dom            18.3.1   -> 19.3.0
vitest, @vitest/ui, @vitest/coverage-v8   4.1.10 -> 5.0.0
jsdom                        27.4.0   -> 30.0.1
typescript                   5.9.3    -> 7.0.2
```

| Upgrade | Readiness evidence | Risk |
|---|---|---|
| **React 18.3 → 19.3** | Every direct React lib declares a `^19` peer (`@radix-ui/themes` 3.3.0, `react-aria-components` 1.20, `@tanstack/react-form/query/router/table`, `react-error-boundary` 6, `@sentry/react` 10, `@testing-library/react` 16, `sonner` 2, `@nivo/bar` 0.99, `zustand` 5). Source scan: `forwardRef` 0, component `.defaultProps =` 0, `ReactDOM.render` 0, `react-dom/test-utils` 0, `findDOMNode` 0, `useRef()` without arg 0, `createRoot` already used (4). The 322 `defaultProps` grep hits are all test fixtures named `const defaultProps = {…}`, not React statics. | Low-medium. Main work is `@types/react` 19 strictness (e.g. `JSX` namespace → `React.JSX`, ref typing) and the `act` warnings in 74 test call sites. Bump `@types/react`/`@types/react-dom` in the same PR. |
| **@tanstack/react-table 8 → 9** | Small surface: 2 `useReactTable`, 3 `getCoreRowModel/getSortedRowModel/getFilteredRowModel`, 3 `flexRender`, 5 `ColumnDef<` | Low-medium; v9 changes are mostly typing/feature-import based. |
| **vitest 4 → 5 (+ jsdom 30, jest-dom 7)** | Vite 8 already present (vitest 5 requires it). Setup is minimal (`src/test/setup.ts` imports `@testing-library/jest-dom/vitest`). `vitest-sonar-reporter` 3.0.0 — check its vitest 5 peer before bumping. | Medium; do after the vitest 4.1.11 patch. |
| **TypeScript 5.9 → 7.0 (native)** | Project only runs `tsc --noEmit`; linting/formatting is Biome, bundling is Rolldown. Nothing depends on the JS `typescript` API except `cosmiconfig`'s optional peer. | Low-medium; wait until `@tanstack/router-plugin` and `vite-plugin-svgr` confirm TS7 compatibility. |
| **@types/node 20 → 22** | Runtime is already Node 22 (Dockerfile, docs.yml, local). Vite 8 engines: `^20.19.0 \|\| >=22.12.0`. | Trivial; do it together with the Node standardisation in (d). |
| `@biomejs/biome` 2.4.15 → 2.5.12 (minor) | `biome.json` `$schema` is pinned to `2.4.1` — update the schema URL in the same PR. | Low. |

---

## (c) Unused / redundant packages (with grep evidence)

Grep scope: all `*.cs` excluding `bin/obj/Migrations`; all `src/**/*.{ts,tsx}` plus configs for the client.

### NuGet — remove

| Package (project) | Evidence | Note |
|---|---|---|
| **Newtonsoft.Json 13.0.4** (Core, Tests) | `using Newtonsoft\|JsonConvert\.\|Newtonsoft\.` → **0 files** in Api/Common/Core/Host and **0** in Tests. `System.Text.Json` is what's used (4 files: `Api/Infrastructure/DateTimeConverter.cs`, `Common/Entities/Expansion.cs`, `Core/Auth/OidcService.cs`, …). Added in `b37c9871` ("wip", 2024-06-30). | Nothing else needs it: `BoardGamer.BoardGameGeek` 0.10.x is a dependency-free netstandard2.0 package (its nuspec has an empty `<group targetFramework=".NETStandard2.0" />`). Removing both refs removes it from the graph entirely. |
| **HtmlAgilityPack 1.13.0** (Core) | `HtmlAgilityPack\|HtmlDocument\|HtmlWeb` → **0 files**. Same "wip" commit. | Dead. |
| **Microsoft.AspNetCore.OpenApi 10.0.1\*** (Host) | `AddOpenApi(\|MapOpenApi(` → **0 files**. Swashbuckle is the OpenAPI stack in use: `Program.cs:230 AddSwaggerGen`, `:330 UseSwagger`, `:331 UseSwaggerUI`. Swashbuckle.AspNetCore 10.2.3 brings `Microsoft.OpenApi 2.12.0` itself. Present since `3a0ff363` (2024-02). | Two OpenAPI stacks referenced, one used. |
| **Microsoft.VisualStudio.Azure.Containers.Tools.Targets 1.23.0** (Host) | Only affects VS "Docker F5" tooling. The image is built from the hand-written root `Dockerfile` by `publish-container.yml`. Related VS remnants in the csproj: `<DockerDefaultTargetOS>`, `<Content Include="..\DockerFile">` (**case mismatch**: the file is `Dockerfile`; on Linux that item resolves to nothing), `<Content Include="..\.dockerignore">`. | Keep only if someone actually debugs via VS container tools. |
| **Serilog.Extensions.Hosting 9.0.0** (Host) | Transitive of `Serilog.AspNetCore` (`--include-transitive` lists it under Core/Tests as transitive 9.0.0). | Redundant direct ref. |
| **Refit 10.2.0** (Host) | `Refit.HttpClientFactory` depends on `Refit`. Host source only calls `AddRefitClient<…>()` (`Program.cs:265`); the `[Get]` interface lives in Core. | Redundant direct ref (keep Core's). |
| **Microsoft.EntityFrameworkCore 10.0.\*** (Host) | Transitive via Core (which also gets it via Common). `Microsoft.EntityFrameworkCore.Design` in Host needs EF, but the transitive one satisfies it. | Harmless; disappears naturally under CPM. |

### NuGet — used, but worth a note

| Package | Evidence | Note |
|---|---|---|
| **System.IdentityModel.Tokens.Jwt 8.22.0** (Core) | Used: `Core/Auth/TokenService.cs:53 new JwtSecurityToken(`, `:60 new JwtSecurityTokenHandler().WriteToken`. | This direct 8.22.0 pin lifts `Microsoft.IdentityModel.{Tokens,JsonWebTokens,Logging,Abstractions}` to 8.22.0 while `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12` keeps `Microsoft.IdentityModel.Protocols(.OpenIdConnect)` at **8.19.2** → a mixed IdentityModel set (`Host` transitive listing). Microsoft's recommended API is `JsonWebTokenHandler` in `Microsoft.IdentityModel.JsonWebTokens` (already transitive). Migrating `TokenService` to it lets you drop the direct ref and the skew. |
| **Microsoft.AspNetCore.SpaServices.Extensions** (Host) | Used: `Program.cs:271 AddSpaStaticFiles`, `:368 UseSpaStaticFiles`, `:372 UseSpa`. | Works, but this is the legacy SPA package; `UseStaticFiles` + `MapFallbackToFile("index.html")` covers the production path (the dev proxy is `SpaProxy`). Optional simplification. |
| **Microsoft.AspNetCore.SpaProxy** (Host) | Only via MSBuild props (`SpaProxyLaunchCommand=pnpm dev`, `SpaProxyServerUrl`); no runtime API usage. | Dev-time only; condition it on `'$(Configuration)' == 'Debug'` so it is not restored/published into the container. |
| **OllamaSharp 5.4.30** (Core) | Used in `Core/Rag/AiClientFactory.cs` (`OllamaApiClient`). Its source generator is **not** used (`[OllamaTool` → 0 hits). | The package's analyzer is built for Roslyn 5.6 and is silently disabled by SDK 10.0.112 (Roslyn 5.0) → `CS9057` on every compile (see (f)). `ExcludeAssets="analyzers"` removes the noise. |
| **Microsoft.EntityFrameworkCore.InMemory** (Tests) | `UseInMemoryDatabase` in 8 test files (Auth/OIDC/UserAdmin services). No Testcontainers/Sqlite (`UseSqlite\|Testcontainers` → 0). | Fine for unit tests; see (d) if you want relational fidelity. |
| **FluentAssertions 8.10.0** (Tests) | 124 files. | v8 is under the Xceed Community License: free for non-commercial and open-source use; the repo is MIT (`LICENSE`), so compliant. If you ever need a clean Apache-2 alternative, `AwesomeAssertions` is the drop-in fork. |
| **BoardGamer.BoardGameGeek 0.10.0** (Common, Core, Tests) | 14 files. | See `local-packages` in (e). |
| Pgvector 0.3.2 (Common) + Pgvector.EntityFrameworkCore 0.3.0 (Core), MailKit, PdfPig, Microsoft.Extensions.AI(.OpenAI), Sentry, ImageSharp, Ardalis.*, Identity.* | All have real usages (7, 4, 1, 4/1, 2, 3, 29/43, 1/7 files). | Keep. |

### npm — remove / move

| Package | Evidence | Action |
|---|---|---|
| **`@radix-ui/react-popover ^1.1.15`** | `grep -rn react-popover` (excluding node_modules/lockfile) → only `package.json:27`. **0 imports.** And it creates a second copy: direct `1.1.23` next to `@radix-ui/themes>radix-ui>@radix-ui/react-popover 1.1.15` (`pnpm why` → "Found 2 versions"). | Remove. |
| **`@rollup/rollup-linux-x64-gnu ^4.12.0`** (optionalDependencies) | Added 2024-02-27 (`91c1422e "fix"`) — the classic npm optional-deps bug workaround (npm/cli#4828) from the Rollup 4 native-binary era, when this was an npm project. The repo moved to pnpm on 2026-05-19 (`d327b4b4`), which handles optional deps correctly, and **Vite 8 bundles with Rolldown: `pnpm why rollup` → empty**; the only `rollup` strings in the lockfile are `@rollup/pluginutils`'s optional peer and this orphan. | Remove. It is pure download weight in the Docker frontend stage. |
| **`@sentry/vite-plugin ^5.1.1`** in `dependencies` | Only imported by `vite.config.ts`. It is what drags `@babel/core → browserslist` into the **prod** audit path. | Move to `devDependencies`. `pnpm audit --prod` becomes clean immediately. |
| `@radix-ui/react-select` | Used (`BgtSelect.tsx`, `BgtSimpleSelect.tsx`) but two copies in the bundle: direct `2.3.7` vs themes' `2.2.6`. | Either import `Select` from the `radix-ui` umbrella that `@radix-ui/themes` already ships, or pin the direct dep to the version themes uses. |
| `react-aria-components` + `@internationalized/date` | Used by exactly one file: `src/components/BgtForm/BgtDatePicker.tsx`. | Not unused, but a large second UI toolkit for one date picker while Radix is the primary kit. Consolidation candidate, not a bug. |
| `@tanstack/react-query-devtools`, `@tanstack/react-router-devtools` in `dependencies` | 1 import each. | Fine if tree-shaken behind `import.meta.env.DEV`; otherwise devDependencies. |
| `class-variance-authority` (49 imports), `react-loading-icons` (9), `zustand` (`src/hooks/useAuth.ts`), `react-error-boundary` (4 files), `@nivo/bar` (1), `@nivo/pie` (2), `sonner` (4), `axios` (1), i18next trio | All used. | Keep. |
| `boardgametracker.client.esproj` | `<JavaScriptTestFramework>Jest</JavaScriptTestFramework>` while the project uses Vitest; SDK is `Microsoft.VisualStudio.JavaScript.Sdk/0.5.271090-alpha` (a 2023 pre-release; 1.0.x is stable). Renovate's nuget manager does not parse `.esproj`. | Cosmetic, but it is also what breaks `dotnet list package` enumeration (TL;DR #4). |

**Used-but-not-declared:** none. Every bare import specifier found in `src/**` (`react`, `react-i18next`, `vitest`, `@tanstack/*`, `class-variance-authority`, `date-fns`, `zod`, `@radix-ui/themes`, `@testing-library/*`, `react-loading-icons`, `sonner`, `react-error-boundary`, `i18next*`, `@radix-ui/react-{switch,tooltip,select,checkbox}`, `@nivo/{pie,bar}`, `react-aria-components`, `axios`, `@sentry/react`, `@internationalized/date`, `zustand`, `react-dom`) maps to a declared dependency; `vite/client` and `vite-plugin-svgr/client` type references are covered by devDependencies.

---

## (d) Missing packages / tooling, with justification

### .NET

| Gap | Justification | Concrete change |
|---|---|---|
| **`global.json`** | Local SDK is 10.0.112; Docker uses `sdk:10.0-alpine` (floating patch); CI installs **8.x** and relies on the runner image's preinstalled 10. OllamaSharp's analyzer already shows Roslyn-version sensitivity (CS9057). | Add `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }` and switch the three `setup-dotnet` steps to `global-json-file: global.json`. |
| **`Directory.Packages.props` (CPM)** | Same version string duplicated across csproj files: `Microsoft.EntityFrameworkCore` (Common, Core, Host), `BoardGamer.BoardGameGeek` (Common, Core, Tests), `Refit` (Core, Host), `Serilog.AspNetCore` (Common, Host), `Serilog.Sinks.Console` (Common, Host), `Ardalis.Specification` (Core, Tests), `Newtonsoft.Json` (Core, Tests), `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (Core, Tests). Host also lists `..\BoardGameTracker.Core\BoardGameTracker.Core.csproj` **twice**. Renovate supports CPM natively. | `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` + one `Directory.Packages.props`; delete the duplicate ProjectReference. |
| **Analyzers / warnings policy in `Directory.Build.props`** | `Directory.Build.props` only sets version numbers. No `TreatWarningsAsErrors`, no `AnalysisLevel`/`AnalysisMode`, no `EnforceCodeStyleInBuild`, no `.editorconfig` (the only style file is `BoardGameTracker.sln.DotSettings.user`, which is per-user and not shared). The codebase has only **2** real nullable warnings (see (f)), so turning the ratchet on is cheap now and expensive later. | Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisLevel>latest-recommended</AnalysisLevel>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`, a root `.editorconfig`; fix `Common/Entities/Config.cs:14` and `Core/Email/MailKitSmtpSender.cs:22`. Also add `<ImplicitUsings>enable</ImplicitUsings>` to Tests (the only project without it). |
| **NuGet lockfile (`packages.lock.json`)** | Floating versions (`10.0.*`, `10.0.1*`) + no lock = the container build restores whatever patch is newest *at build time*, which is not what PR CI tested. `find packages.lock.json` → none; `RestorePackagesWithLockFile` → not set. | Either pin exact versions (preferred, renovate does the patch bumps) or `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` + `dotnet restore --locked-mode` in CI/Dockerfile. |
| **`Microsoft.Extensions.Http.Resilience`** | 5 `AddHttpClient` registrations (`Program.cs:173-176, :255`; Ollama, changedetection.io, BGG, DockerHub, images) and **0** resilience handlers (`AddStandardResilienceHandler\|Polly\|AddResilienceHandler` → 0 files). Project memory notes BGG import "fails first, works second" and a 30 s frontend timeout — textbook retry/timeout territory. | `services.AddHttpClient(...).AddStandardResilienceHandler()` on the external-API clients. |
| **`Microsoft.IdentityModel.JsonWebTokens`** (already transitive) | Replaces `System.IdentityModel.Tokens.Jwt`'s `JwtSecurityTokenHandler` (Microsoft's recommended path) and removes the 8.19.2/8.22.0 IdentityModel skew. | Rewrite `TokenService` with `JsonWebTokenHandler.CreateToken(SecurityTokenDescriptor)`; drop the direct `System.IdentityModel.Tokens.Jwt` ref. |
| Optional: `Microsoft.EntityFrameworkCore.Sqlite` (in-memory) or `Testcontainers.PostgreSql` | 8 test files use `UseInMemoryDatabase`, which ignores relational constraints/`GroupBy` translation; the repo pattern deliberately pushes aggregates into repository methods, so those queries are untested against a real provider. | Judgment call; the CI already runs a Postgres service? (No — `ci.yml` has no services block, so Testcontainers would need Docker on the runner; Sqlite in-memory is the cheap step.) |

### Frontend

| Gap | Justification | Concrete change |
|---|---|---|
| **Node version pinning** | CI `node-version: "20.x"` (`ci.yml`, `publish-container.yml`, `security.yml`, `security-full-scan.yml`) vs `docs.yml` 22 vs Dockerfile `node:22-alpine` vs local 22.21.1 vs `@types/node ^20`. Node 20 is EOL since 2026-04-30. No `.nvmrc`, no `engines`. | Add `.nvmrc` (`22`), `"engines": { "node": ">=22.12", "pnpm": ">=10" }` in both `package.json`s, `node-version-file: .nvmrc` in workflows, `@types/node` → 22. |
| **`.npmrc`** | None in either pnpm project. `packageManager: pnpm@10.34.5` is set (good; corepack honours it). | Consider `engine-strict=true` once `engines` exists; otherwise fine. |
| **Lockfiles committed?** | Yes, both tracked (`git ls-files`). But `docs/pnpm-lock.yaml` is corrupt (see (e)). | Regenerate docs lock. |
| Optional: `knip` | Would have caught `@radix-ui/react-popover`, the rollup orphan and the esproj Jest setting automatically. | devDependency + `pnpm knip` in CI. |

---

## (e) Renovate / versioning coherence

`renovate.json`: `config:recommended`, `:dependencyDashboard`, `:semanticCommits`, `group:allNonMajor`, `helpers:pinGitHubActionDigests`, Monday schedule, patch + devDependency-minor automerge (`platformAutomerge`), majors labelled and not automerged, security-sensitive auth packages never automerged, `dockerfile.pinDigests: false`. No `dependabot.yml` (good, single bot).

What is coherent:

- **GitHub Actions**: every `uses:` in all 9 workflows is SHA-pinned with a version comment (grep for unpinned → none); renovate is actively bumping them (`#226 codeql digest`, `#228 checkout v7`, `#230/#231/#232`). Coherent with `helpers:pinGitHubActionDigests`.
- **Floating NuGet ranges (`10.0.*`, `10.0.1*`) are renovate-managed, not renovate-blind.** Evidence: `16605def chore(deps): update dependency microsoft.aspnetcore.identity.entityframeworkcore to 10.0.1* (#221)`, `5bdd88da … to 8.0.2* (#183)`, `bcb30ae3 update ef core packages to 9.0.18 (#195)`. Renovate rewrites the wildcard itself. The `EF Core packages` / `ASP.NET Core Identity` groups therefore do fire.
- **`[0.1.16]` PdfPig** exact range was also produced by renovate (`caa572e9`), from the user's original `[0.1.15]` (`a1c074f8 "Update to .NET 10"`). Handled; the exact-range syntax just forbids NuGet from unifying upward, which is harmless here.

What is not coherent / worth changing:

1. **Floating + no lockfile = untested patch drift.** `10.0.1*` means "any 10.0.10–10.0.19" and `10.0.*` "any 10.0.x": Docker/CI restore silently moves to a newer patch than the one the last renovate PR tested, and when 10.0.20 ships the `10.0.1*` range goes stale until renovate rewrites it. With renovate present, floating buys nothing that pinned + grouped patch automerge does not already give, minus reproducibility. **Recommendation: pin exact versions (or add `packages.lock.json` + `--locked-mode`).**
2. **No `lockFileMaintenance`.** All 7 npm advisories are transitive and already satisfiable by declared ranges; renovate's `vulnerabilityAlerts` only targets direct deps (and requires GitHub Dependabot alerts enabled on the repo). The `pnpm.overrides` block is a manual stand-in for what `lockFileMaintenance: { enabled: true, schedule: ["before 6am on Monday"] }` would do automatically.
3. **`pnpm.overrides` is mostly dead weight** (declared ranges checked in `node_modules/.pnpm/*/package.json`):

   | Override | Requester's declared range | Verdict |
   |---|---|---|
   | `form-data@>=4.0.0 <4.0.6 → ^4.0.6` | `axios@1.19.0` → `form-data ^4.0.6` | no-op, remove |
   | `brace-expansion@>=3.0.0 <5.0.7 → ^5.0.7` | `minimatch@10.2.6` → `^5.0.8` | no-op, remove |
   | `@babel/core@<7.29.6 → ^7.29.6` | all 10 requesters → `^7.29.6` | no-op, remove |
   | `ws@>=8.0.0 <8.21.0 → ^8.21.0` | `jsdom@27.4.0` → `ws ^8.18.3` | technically binding but a fresh resolve already yields 8.21.1; remove |
   | `js-yaml@>=4.0.0 <4.3.0 → ^4.3.0` | `cosmiconfig@8.3.6` → `^4.1.0` | binding **and stale** (resolved 4.3.0 is vulnerable, needs ≥4.3.2). Replace with a lockfile refresh; no override needed |

   Net: the whole `overrides` block can go after `pnpm update browserslist baseline-browser-mapping js-yaml ws` refreshes the lock.
4. **Docker/compose**: renovate's docker managers are on by default. `Dockerfile` → `node:22-alpine`, `sdk:10.0-alpine`, `aspnet:10.0-alpine` (renovate will propose `node:24-alpine` as a major; fine). `docker-compose*.yml` → `pgvector/pgvector:pg16` will get **pg17/pg18 major PRs** — a Postgres major is a data migration, not a dependency bump; add a rule (`matchDatasources: ["docker"], matchPackageNames: ["pgvector/pgvector"], enabled: false` or `dependencyDashboardApproval: true`). `ollama/ollama:latest` and `uping/boardgametracker:latest` are untrackable by design. No renovate commit has ever touched Dockerfile/compose (git log → empty), consistent with nothing being bumpable yet.
5. **`Aspire.AppHost.Sdk/13.4.6`** lives in the `Sdk=` attribute. Verify on the dependency dashboard that renovate lists it; if not, add a custom manager, or the AppHost will drift from the `Aspire.Hosting.*` packages it must match.
6. **`.esproj`** is outside renovate's nuget `fileMatch`, so the alpha `Microsoft.VisualStudio.JavaScript.Sdk` will never be proposed.
7. **Overlapping group rules** (`group:allNonMajor` then TanStack/Radix/Nivo/Vite/i18next/test groups) are fine — later rules win — but note that "TanStack packages" excludes majors while "Vite"/"i18next" groups include them, which is presumably intentional.

### `local-packages/` and `nuget.config`

- `nuget.config`: `<clear/>` + nuget.org only. The `local-bgg-fix` source was removed on 2026-08-07 (`ee5a5958`, "breaks CI restore with NU1301").
- `local-packages/BoardGamer.BoardGameGeek.0.10.1-localfix.nupkg` (44 KB, dated 2026-07-22) is **gitignored** (`.gitignore:649`), referenced by **nothing** (`grep -ri localfix|local-bgg|0.10.1` across the repo → 0), and every csproj resolves `0.10.0` from nuget.org. Its nuspec: upstream `Cobster/BoardGamer.BoardGameGeek` at commit `4115ad11…`, netstandard2.0, no dependencies, a locally rebuilt DLL with an unrecorded patch.
- Verdict: **stale orphan** — delete the folder. If the local patch fixed a real 0.10.0 bug, that fix is either now worked around in app code (`Core/Games/BggImportService.cs`, `GameFactory.cs`) or lost; upstream last published 0.10.0 and the repo is essentially unmaintained, so the durable options are (a) open a PR upstream, or (b) vendor the source into `BoardGameTracker.Core` (it is a small XML API client) and drop the package. Keeping a phantom `-localfix` nupkg on one developer's disk is the worst of both.

### `docs/pnpm-lock.yaml`

Corrupt on `master` (contained by `origin/master`), introduced by merge `c75212aa "Merge branch 'master' into renovate/all-minor-patch"` (both parents' lockfiles were valid: 0 duplicate keys each). Duplicated keys: `packages:` → `@napi-rs/wasm-runtime@1.2.3`, `es-module-lexer@2.3.2`, `postcss@8.5.26`; `snapshots:` → `@bruits/satteri-win32-arm64-msvc@0.10.5`, `find-process@2.1.1`.

```
 ERR_PNPM_BROKEN_LOCKFILE  The lockfile at "...\docs\pnpm-lock.yaml" is broken: duplicated mapping key (619:3)
```

`docs.yml` runs `pnpm install --frozen-lockfile` → the Pages deploy has been failing (or will on the next `docs/**` push) since 2026-08-24. The client lockfile is fine (0 duplicates).

---

## (f) Build warning summary

Full non-incremental rebuild (`dotnet build --no-incremental`), Api/Common/Core/Tests in place, Host/AppHost into a scratch `OutDir` because the user's running JetBrains debugger locks `Host/bin` (that lock produced 146×MSB3061 + 60×MSB3026 + 6 MSB3027/3021 errors in the first pass — environmental, not code).

| Code | Count (unique sites) | Where | Meaning |
|---|---|---|---|
| **CS8618** | 1 | `BoardGameTracker.Common/Entities/Config.cs(14,19)` — `public string Value { get; set; }` non-nullable, not initialised | Nullable |
| **CS8604** | 1 | `BoardGameTracker.Core/Email/MailKitSmtpSender.cs(22,54)` — `password` may be null in `AuthenticateAsync(username, password, …)` | Nullable |
| **CS9057** | 1 (repeated per compile: 8 in sln pass, 6 in Host pass) | `OllamaSharp 5.4.30` analyzer `OllamaSharp.SourceGenerators.dll` references compiler **5.6.0.0**, SDK 10.0.112 runs **5.0.0.0** → generator disabled | Package/SDK mismatch, no functional impact (`[OllamaTool]` unused) |

Total real code warnings: **2** (nullable). Errors: 0. That is low enough to enable `TreatWarningsAsErrors` today.

---

## (g) Quick wins (ordered by value ÷ effort)

1. **Regenerate `docs/pnpm-lock.yaml`** (`cd docs && pnpm install`, commit). Unblocks the docs deploy. 2 minutes.
2. **Frontend security gates green**: move `@sentry/vite-plugin` to `devDependencies`; `pnpm update browserslist baseline-browser-mapping js-yaml ws vitest @vitest/ui @vitest/coverage-v8` (patch-level; vitest → 4.1.11); delete the `pnpm.overrides` block and the `@rollup/rollup-linux-x64-gnu` optional dep; remove `@radix-ui/react-popover`. Re-run `pnpm audit` → 0.
3. **NuGet dead refs**: drop `Newtonsoft.Json` (Core, Tests), `HtmlAgilityPack`, `Microsoft.AspNetCore.OpenApi`, `Serilog.Extensions.Hosting`, Host's duplicate `Refit` and duplicate Core `ProjectReference`; decide on `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (+ fix/remove the `..\DockerFile` content item). Add `ExcludeAssets="analyzers"` on `OllamaSharp` to silence CS9057.
4. **Delete `local-packages/`** (orphaned `-localfix` nupkg).
5. **Toolchain pinning**: `global.json` (10.0.1xx), `.nvmrc` = 22, `engines` in both `package.json`, CI `global-json-file` / `node-version-file`; `@types/node` → 22; README line 189 still says ".NET 8.0".
6. **Fix the sln-level vulnerability check** in `security.yml`: enumerate csproj files (`for p in */*.csproj; do dotnet list "$p" package --vulnerable --include-transitive; done`) or use `dotnet restore` + `NuGetAudit` (`<NuGetAuditMode>all</NuGetAuditMode>` + `<TreatWarningsAsErrors>` turns NU1901-NU1904 into build failures, which also covers Docker builds). Today Tests and AppHost are silently skipped.
7. **Ratchet quality in `Directory.Build.props`**: `TreatWarningsAsErrors`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild`, root `.editorconfig`; fix the 2 nullable sites. Then CPM (`Directory.Packages.props`) to collapse the 8 duplicated version strings.
8. **Pin the floating NuGet versions** (or add `packages.lock.json` + locked restore) so the container ships what CI tested; renovate keeps handling patches either way.
9. **Renovate**: enable `lockFileMaintenance`; add a rule to gate `pgvector/pgvector` majors; verify `Aspire.AppHost.Sdk` shows on the dashboard.
10. **Majors, in this order**: Serilog 10 (trivial) → xunit/test SDK bundle → React 19 + `@types/react` 19 → TanStack Table 9 → vitest 5/jsdom 30/jest-dom 7 → Refit 15 → ImageSharp 4 → TypeScript 7 (last; wait for plugin ecosystem).
