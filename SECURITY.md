# Security Policy

## Supported versions

Only the latest stable release (`uping/boardgametracker:latest`) and the current beta (`:beta`) receive fixes. Older tags are not patched; upgrade instead (see the [upgrading guide](https://mregni.github.io/BoardGameTracker/getting-started/upgrading/)).

## Reporting a vulnerability

Please do not open a public issue for security problems.

Use [GitHub private vulnerability reporting](https://github.com/mregni/BoardGameTracker/security/advisories/new) and include:

- the version or image tag you are running,
- how the application is exposed (local network, reverse proxy, `AUTH_ENABLED` setting),
- steps to reproduce, and the impact you expect.

You should get a first reply within a week. Fixes ship as a new release and are credited in the release notes unless you prefer to stay anonymous.

## Hardening checklist for self-hosters

- Set a random `JWT_SECRET` of at least 32 characters and change the default `admin` password after the first login.
- Keep `AUTH_ENABLED=true` unless the instance is only reachable from a trusted network.
- Behind a reverse proxy, set `TRUSTED_PROXIES` so the login rate limit and HTTPS detection see the real client.
- Keep the image up to date; every release is scanned with Trivy and ships an SBOM.
