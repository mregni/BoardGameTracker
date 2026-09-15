## What

<!-- One or two sentences on what changes and why. Link the issue: Closes #123 -->

## How to test

<!-- Steps a reviewer can follow, or the tests you added -->

## Checklist

- [ ] PR title follows conventional commits (`feat:`, `fix:`, `docs:`, `chore:`, ...)
- [ ] Backend changes have xUnit tests; frontend changes have Vitest tests where it makes sense
- [ ] New or changed UI text exists in every folder under `boardgametracker.client/public/locales/`
- [ ] Changed a `<PackageReference>`? `packages.lock.json` files are updated (`dotnet restore`)
- [ ] User-facing behaviour or configuration changed? The docs under `docs/` are updated
