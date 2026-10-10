# Pursuit PostgreSQL Portability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Pursuit's SQL Server-only persistence path with a single PostgreSQL path that is exercised locally and in CI and can connect to Neon with certificate-validated TLS.

**Architecture:** Keep the domain, application services and HTTP API provider-neutral. Configure Npgsql only in Infrastructure and design-time composition, regenerate a PostgreSQL migration history, and move integration and Compose environments to PostgreSQL so hosted and tested database semantics match.

**Tech Stack:** .NET 10.0.401, EF Core 10.0.8, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, PostgreSQL 17, Testcontainers.PostgreSql, xUnit, Docker Compose, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-10-10-pursuit-portable-public-demo-hosting-design.md`

## Global Constraints

- PostgreSQL is the only relational provider after this tranche.
- Application and Domain projects must not reference Npgsql or hosting providers.
- Production database connections require `SSL Mode=VerifyFull` and certificate hostname validation.
- Development may use `SSL Mode=Disable` only outside Production.
- The API must not migrate its database at startup.
- Existing tenant filters, uniqueness rules, pagination and API contracts must remain unchanged.
- No provider credentials or hosted connection strings enter tracked files or test output.

## Review Focus

- A Production connection that omits `SSL Mode` must fail startup rather than inherit Npgsql's permissive default.
- `SSL Mode=Require` must fail Production because it encrypts without authenticating the server.
- Case-insensitive email and slug uniqueness must behave the same under PostgreSQL as under the existing application contract.
- Migration bundles must be idempotent against both an empty and current PostgreSQL database.
- Tenant-filtered queries must not leak records through provider-specific translation differences.

---

### Task 1: PostgreSQL runtime and production validation

**Files:**
- Modify: `src/Pursuit.Infrastructure/Pursuit.Infrastructure.csproj`
- Modify: `src/Pursuit.Infrastructure/DependencyInjection.cs`
- Modify: `src/Pursuit.API/Configuration/PursuitDesignTimeDbContextFactory.cs`
- Modify: `src/Pursuit.API/Configuration/StartupConfigurationValidator.cs`
- Modify: `tests/Pursuit.IntegrationTests/Configuration/StartupConfigurationValidatorTests.cs`
- Modify: `tests/Pursuit.IntegrationTests/Configuration/PursuitDesignTimeDbContextFactoryTests.cs`

**Interfaces:**
- Produces: `AddInfrastructure` and `PursuitDesignTimeDbContextFactory` configured through `UseNpgsql(string?)`.
- Produces: Production startup rejection unless `NpgsqlConnectionStringBuilder.SslMode == SslMode.VerifyFull` and `TrustServerCertificate == false`.
- Consumes: `ConnectionStrings:DefaultConnection` with standard Npgsql keys.

- [ ] **Step 1: Change validator and design-time tests to require PostgreSQL and verified TLS**

Add tests named `Validate_rejects_postgresql_production_connection_without_verified_tls`, `Validate_accepts_postgresql_production_connection_with_verified_tls`, and `CreateDbContext_WithConnectionEnvironmentVariable_UsesNpgsqlProvider`. Assert explicit error text and provider name `Npgsql.EntityFrameworkCore.PostgreSQL`.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter "FullyQualifiedName~StartupConfigurationValidatorTests|FullyQualifiedName~PursuitDesignTimeDbContextFactoryTests" --no-restore`

Expected: FAIL because SQL Server remains configured and the validator accepts or parses the wrong connection semantics.

- [ ] **Step 3: Replace the SQL Server package and composition with Npgsql 10.0.3**

Use `NpgsqlConnectionStringBuilder` for validation. Reject every Production mode except `VerifyFull`, and reject `TrustServerCertificate=true` independently.

- [ ] **Step 4: Restore and run the focused tests**

Run: `dotnet restore Pursuit.slnx && dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter "FullyQualifiedName~StartupConfigurationValidatorTests|FullyQualifiedName~PursuitDesignTimeDbContextFactoryTests" --no-restore`

Expected: PASS with zero failed tests.

- [ ] **Step 5: Commit**

Commit: `refactor: switch database composition to PostgreSQL`

### Task 2: PostgreSQL schema and integration infrastructure

**Files:**
- Replace: `src/Pursuit.Infrastructure/Persistence/Migrations/*`
- Modify: `src/Pursuit.Infrastructure/Persistence/Configurations/JobConfiguration.cs`
- Modify: `tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj`
- Modify: `tests/Pursuit.IntegrationTests/CustomWebApplicationFactory.cs`
- Modify: `tests/Pursuit.IntegrationTests/Infrastructure/DatabaseSchemaProvisioner.cs`
- Modify: `tests/Pursuit.IntegrationTests/Auth/AuthFlowTests.cs`
- Modify: `src/Pursuit.Application/Services/AuthService.cs`
- Modify: relevant integration tests that assert SQL Server connection syntax

**Interfaces:**
- Consumes: Task 1 `UseNpgsql` composition.
- Produces: one PostgreSQL migration history and a `PostgreSqlContainer`-backed integration factory.

- [ ] **Step 1: Convert the integration harness to PostgreSQL and add schema-behaviour tests**

Use `PostgreSqlContainer` but retain the existing test collection lifetime. Add a
migration test that creates the schema twice without error and an authentication
test proving registration and login treat email casing consistently without
creating a second account.

- [ ] **Step 2: Run the focused migration tests and verify RED**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter "FullyQualifiedName~Migration|FullyQualifiedName~DesignTime" --no-restore`

Expected: FAIL because the migration history still targets SQL Server and email
lookup is case-sensitive under PostgreSQL.

- [ ] **Step 3: Replace Testcontainers SQL Server with PostgreSQL and generate a clean migration history**

Use `postgres:17-alpine`. Remove provider-specific SQL Server type declarations
from live model configuration. Generate the migration from the current model; do
not hand-translate historical migrations. Normalize registration and login email
with `Trim().ToLowerInvariant()` before repository access so behaviour does not
depend on database collation.

- [ ] **Step 4: Run the complete backend test suite**

Run: `dotnet test Pursuit.slnx --configuration Release --no-restore`

Expected: PASS with zero failed tests.

- [ ] **Step 5: Commit**

Commit: `refactor: migrate schema and tests to PostgreSQL`

### Task 3: Local, container and CI migration parity

**Files:**
- Modify: `docker-compose.yml`
- Modify: `docker-compose.e2e.yml`
- Modify: `Dockerfile`
- Modify: `.github/workflows/ci.yml`
- Modify: `tests/Pursuit.IntegrationTests/Deployment/ComposeMigrationBoundaryTests.cs`
- Modify: `README.md`
- Modify: `docs/production-configuration.md`

**Interfaces:**
- Consumes: Task 2 PostgreSQL migrations and connection format.
- Produces: PostgreSQL local/E2E services, Linux migration bundle, CI migration execution and operator documentation.

- [ ] **Step 1: Change deployment-boundary tests to require PostgreSQL**

Assert PostgreSQL images, health checks, connection keys, migration dependency and the absence of SQL Server images and connection strings.

- [ ] **Step 2: Run deployment-boundary tests and verify RED**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter "FullyQualifiedName~ComposeMigrationBoundaryTests" --no-restore`

Expected: FAIL because Compose and CI still provision SQL Server.

- [ ] **Step 3: Convert Compose, Docker migration bundling and CI to PostgreSQL 17**

Keep migrations outside API startup. CI runs the bundle twice against an empty PostgreSQL service and uploads only the reviewed bundle artifact.

- [ ] **Step 4: Update operator documentation**

Document local connection strings, `VerifyFull` in Production, separate migration/API roles, rollout order and the removal of SQL Server prerequisites.

- [ ] **Step 5: Run repository verification**

Run: `dotnet test Pursuit.slnx --configuration Release --no-restore && npm --prefix client run lint && npm --prefix client test -- --run && npm --prefix client run build && git diff --check`

Expected: every command exits 0.

- [ ] **Step 6: Commit**

Commit: `build: align local and CI databases with PostgreSQL`

### Task 4: Portability evidence and readiness decision

**Files:**
- Create: `docs/hosting/postgresql-portability-report.md`
- Modify: `docs/superpowers/plans/2026-10-09-pursuit-hosting-compatibility-gate.md`

**Interfaces:**
- Consumes: Task 3 verified container, migration and CI commands.
- Produces: sanitized evidence table and explicit `Ready for private provider probe` or `Rejected` decision.

- [ ] **Step 1: Run migration idempotence from an empty local PostgreSQL state**

Run the reviewed migration container twice, then start the API and execute the existing health check. Record command, commit and sanitized result only.

- [ ] **Step 2: Run the complete local browser journey**

Run the existing Playwright matrix with one worker against the PostgreSQL-backed E2E environment.

Expected: Employer, Job Seeker and Admin journeys pass in Chromium, Firefox and WebKit.

- [ ] **Step 3: Write the evidence report and update the superseded gate**

Record database provider, exact commit, migration idempotence, backend suite, frontend checks, browser matrix and unresolved hosted-provider gates. Never mark an unexecuted hosted check as passed.

- [ ] **Step 4: Commit**

Commit: `docs: record PostgreSQL portability evidence`
