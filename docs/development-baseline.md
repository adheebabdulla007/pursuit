# Development baseline

Recorded on 19 September 2026 at commit `befe097` on `main`.

## Pinned toolchain

- .NET SDK `10.0.401`, with roll-forward limited to the latest patch in the same feature band.
- Node.js `22.16.0`.
- npm `10.9.2`, declared in `client/package.json` and verified against `client/package-lock.json`.
- Docker Desktop engine `29.7.2` for the local verification run.

## Verified local checks

| Check | Result |
|---|---|
| `dotnet build Pursuit.slnx --configuration Release --no-restore` | Passed with two NU1903 warnings for the same transitive `Microsoft.OpenApi` 2.0.0 advisory |
| `dotnet test Pursuit.slnx --configuration Release --no-build` | 11/11 passed against Testcontainers SQL Server, Redis, and RabbitMQ |
| Clean frontend install from `package-lock.json` | Passed; npm reported 0 vulnerabilities |
| `npm run lint` | Passed |
| `npm test` | 47/47 passed across 11 files |
| `npm run build` | Passed |
| `npx playwright test` | 3/3 passed: Chromium, Firefox, and WebKit |

The normal user-level npm shim on the verification machine is broken. The clean-install verification used Corepack with the pinned npm version. This is a workstation issue; GitHub Actions installs Node and npm independently.

## CI gates

`.github/workflows/ci.yml` runs on pushes and pull requests targeting `main`, and can also be started manually. It provides three independent gates:

1. Backend restore, Release build, and Docker-backed integration tests.
2. Frontend clean install, lint, component tests, and production build.
3. The existing employer-to-applicant browser journey in Chromium, Firefox, and WebKit, using isolated SQL Server, Redis, RabbitMQ, and Azurite containers.

The workflow has read-only repository permissions, bounded job timeouts, cancellation for superseded runs, synthetic E2E credentials, an `always()` cleanup step, and a seven-day Playwright report on failure or success.

## Known baseline findings

- `Microsoft.OpenApi` 2.0.0 produces NU1903 high-severity advisory warnings through the API and test projects. Dependency remediation remains part of SEC-2.
- EF Core reports that the required `RefreshToken.User` relationship targets an entity with a global query filter. This needs deliberate review during the tenant/session hardening milestones; it is not silently suppressed here.
- The tracked base configuration was missing `JwtSettings:RefreshTokenExpiryInDays`. The E2E run exposed it, and the non-secret seven-day default is now present in `appsettings.json`.
- The first CI workflow does not yet run a dedicated dependency audit, secret scanner, container-image build, load test, deployment, rollback, or restore drill. Those remain explicit later gates rather than being represented as completed.
