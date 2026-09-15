# Contributing to BoardGameTracker

Thanks for helping out. This page covers the things that make a pull request easy to review.

## Before you start

- Open an issue or a discussion first for anything larger than a bug fix, so the design can be agreed on before you write code.
- Fork the repository and create a branch from `master` (`feature/<topic>` or `fix/<topic>`).

## Development setup

Requirements: .NET SDK 10 (`global.json`), Node 22 (`.nvmrc`), pnpm (installed through corepack), Docker.

```bash
# Database with the pgvector extension
docker run -d --name bgt-db -e POSTGRES_USER=dev -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=boardgametracker -p 5432:5432 pgvector/pgvector:pg16

# Backend (from the repository root)
DB_HOST=localhost DB_USER=dev DB_PASSWORD=dev DB_NAME=boardgametracker JWT_SECRET=$(openssl rand -base64 48) dotnet run --project BoardGameTracker.Host

# Frontend
cd boardgametracker.client && pnpm install && pnpm dev
```

The [development page](https://mregni.github.io/BoardGameTracker/extra/development/) in the docs describes the full setup, including the Aspire AppHost.

## Checks that run on every pull request

Run them locally before pushing:

```bash
dotnet restore --locked-mode && dotnet build --no-restore && dotnet test --no-build
cd boardgametracker.client && pnpm typecheck && pnpm lint && pnpm test:run
```

`dotnet test` runs two projects: `BoardGameTracker.Tests` (unit tests, no infrastructure) and `BoardGameTracker.IntegrationTests`, which starts a `pgvector/pgvector:pg16` container through Testcontainers and boots the real application against it — Docker must be running. Run `dotnet test BoardGameTracker.Tests` alone when you only need the fast suite.

- NuGet packages are pinned and locked (`packages.lock.json`). After changing a `<PackageReference>` run `dotnet restore` (without `--locked-mode`) and commit the updated lock files.
- The frontend keeps a generated copy of the API contract: `boardgametracker.client/openapi/swagger.json` (exported by the host with `pnpm export:openapi`, which runs `BoardGameTracker.Host --openapi <file>`) and `src/models/api.generated.ts` (`pnpm generate:api`). Run both after changing a controller, command or DTO and commit the result — CI fails when either file is stale, and `src/models/api.contract.test.ts` fails to typecheck when a hand-written model drifts from the schema.
- Frontend code is formatted and linted by Biome with LF line endings; `pnpm lint:fix` fixes most findings.
- Every translation key must exist in every folder under `boardgametracker.client/public/locales/` (`base`, `en-US`, `es-ES`, `nl-BE`, `nl-NL`). The `localeSync` test fails when a locale drifts. Non-English texts are maintained on Crowdin; a plain English copy in the other locales is fine for a pull request.
- Backend changes come with xUnit tests (Moq + FluentAssertions, `VerifyNoOtherCalls()` at the end of each test). Test files mirror the source folders (`BoardGameTracker.Tests/Games/`, `Badges/BadgeEvaluators/`, `Sessions/Specifications/`, …); shared helpers such as `SessionBuilder`, `TestBadges` and `ShouldIncludeExactly` live in `BoardGameTracker.Tests/Support/`.

## Conventions

- Commit messages and PR titles follow conventional commits (`feat:`, `fix:`, `docs:`, `chore:`, ...); the release notes are generated from them.
- No explanatory comments in code; make the names say it.
- Backend: queries are Ardalis specifications, repositories never save, services call `IUnitOfWork.SaveChangesAsync()`; commands (`Create...Command`) carry DataAnnotations; entities change state through `Update...()` methods.
- Frontend: routes live under `src/routes/<area>` with `-components`, `-hooks`, `-modals`, `-utils` folders; data goes through the typed query factories in `src/services/queries`; mutations invalidate through `useQueryInvalidator`.

## Pull request

- Keep the PR focused; unrelated refactors go in their own PR.
- Fill in the pull request template; screenshots help for UI changes.
- CI must be green. Pull requests from forks skip the steps that need write access to the repository (coverage comment, labels); that is expected.
