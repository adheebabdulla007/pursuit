# Pursuit Deployment Migration Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove database migration from API startup and provide one explicit migration step that works locally, in browser tests, and in a production container deployment.

**Architecture:** Pin `dotnet-ef`, build one EF Core migration bundle into the API image, and run it as a one-time Compose/deployment job before starting the API. Integration tests migrate their disposable database explicitly. CI proves the bundle works against an empty database and remains safe when rerun.

**Tech Stack:** .NET 10.0.401, EF Core/`dotnet-ef` 10.0.8, SQL Server 2022, Docker Compose, xUnit/Testcontainers, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-10-08-pursuit-deployment-migration-boundary-design.md`

## Global Constraints

- Do not change domain behavior, HTTP contracts, authentication, or schema.
- The API must not call EF schema-mutation methods.
- Do not introduce provider-specific deployment infrastructure before a host is selected.
- Local and E2E startup remain one-command operations.
- Keep only controls required to launch safely; defer outbox, readiness, rate limits, monitoring, and backups to their own milestones.

## Review Focus

- Missing migration configuration fails without exposing credentials.
- Empty database migration succeeds before API startup.
- Rerunning the bundle against a current database succeeds without changes.
- Failed migration prevents API startup.
- Existing backend, frontend, and browser workflows remain green.

---

### Task 1: Create the reproducible migration bundle boundary

**Files:**
- Create: `.config/dotnet-tools.json`
- Create: `src/Pursuit.API/Configuration/PursuitDesignTimeDbContextFactory.cs`
- Create: `tests/Pursuit.IntegrationTests/Configuration/PursuitDesignTimeDbContextFactoryTests.cs`
- Modify: `Dockerfile`

**Interfaces:**
- Produces: `PursuitDesignTimeDbContextFactory.CreateDbContext(string[] args)` and `/app/migrations/efbundle` in the API runtime image.

- [ ] **Step 1: Write failing factory tests**

Test that a supplied `ConnectionStrings__DefaultConnection` creates a SQL Server `AppDbContext`. Test that a missing value throws exactly `ConnectionStrings:DefaultConnection is required to create migration artifacts.` Restore environment variables in `finally` blocks and serialize these tests.

- [ ] **Step 2: Confirm the tests fail because the factory is missing**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter FullyQualifiedName~PursuitDesignTimeDbContextFactoryTests`

- [ ] **Step 3: Implement the minimum factory and tool manifest**

Pin `dotnet-ef` 10.0.8. Build `AppDbContext` directly with SQL Server options and `SystemDbContextScope`; do not construct the host or external dependencies.

- [ ] **Step 4: Verify factory and bundle creation**

Run `dotnet tool restore`, the focused tests, and `dotnet dotnet-ef migrations bundle` into ignored `artifacts/migrations/`.

- [ ] **Step 5: Build the Linux bundle into the Docker image**

Restore the local tool in the build stage, create a framework-dependent `linux-x64` bundle using a build-stage-only dummy connection, copy it to `/app/migrations/efbundle`, and keep the normal API entry point unchanged.

- [ ] **Step 6: Build the image and inspect both executables**

Run `docker build`, confirm the image still starts `Pursuit.API.dll`, and confirm `/app/migrations/efbundle` exists and is executable.

- [ ] **Step 7: Commit**

Commit: `build: add deployable migration bundle`

---

### Task 2: Remove automatic migration and preserve test provisioning

**Files:**
- Modify: `src/Pursuit.API/Program.cs`
- Create: `tests/Pursuit.IntegrationTests/Infrastructure/DatabaseSchemaProvisioner.cs`
- Modify: `tests/Pursuit.IntegrationTests/CustomWebApplicationFactory.cs`
- Create: `tests/Pursuit.IntegrationTests/Configuration/ApiMigrationBoundaryTests.cs`

**Interfaces:**
- Produces: `DatabaseSchemaProvisioner.MigrateAsync(string connectionString, CancellationToken cancellationToken = default)` for disposable test databases only.

- [ ] **Step 1: Write a failing startup-boundary test**

Start the API against an empty disposable SQL database with administrator bootstrap disabled and the RabbitMQ consumer replaced in test services. Assert `/health` returns 200 and SQL still has no `__EFMigrationsHistory` table.

- [ ] **Step 2: Confirm current API startup fails the boundary**

Run the focused test and confirm it detects the automatically created schema.

- [ ] **Step 3: Provision integration-test schema explicitly**

Implement the test-only provisioner with `AppDbContext`, SQL Server options, `SystemDbContextScope`, and `Database.MigrateAsync`. Invoke it after the test SQL container becomes healthy.

- [ ] **Step 4: Remove migration from `Program.cs`**

Delete the startup migration scope and unused imports. Keep administrator bootstrap unchanged.

- [ ] **Step 5: Verify focused and complete backend behavior**

Run the boundary test and the complete Release backend suite.

- [ ] **Step 6: Commit**

Commit: `refactor: separate migrations from api startup`

---

### Task 3: Add one-time migration jobs for local and E2E startup

**Files:**
- Modify: `docker-compose.yml`
- Modify: `docker-compose.e2e.yml`
- Create: `tests/Pursuit.IntegrationTests/Deployment/ComposeMigrationBoundaryTests.cs`

**Interfaces:**
- Produces: Compose services `migrate` and `migrate-e2e`, each executing `/app/migrations/efbundle` before dependent application startup.

- [ ] **Step 1: Write failing configuration tests**

Assert both Compose files contain the migration service, SQL health dependency, bundle command, no published migration port, and `service_completed_successfully` for the local API dependency.

- [ ] **Step 2: Add the two migration services**

Use the API image, override only the command, pass the environment-specific SQL connection, and wait for SQL health. Make local API startup depend on successful migration. Ensure E2E infrastructure does not report ready until `migrate-e2e` succeeds.

- [ ] **Step 3: Verify successful and failed paths**

Run both `docker compose config` commands. Start the E2E stack from empty state and run the migration service twice. Override its connection with an invalid endpoint and confirm non-zero exit and blocked dependent startup.

- [ ] **Step 4: Run the focused configuration tests**

Expected: all pass.

- [ ] **Step 5: Commit**

Commit: `build: run migrations before application startup`

---

### Task 4: Verify the migration artifact in CI

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: `migration-bundle` CI job and `pursuit-migration-bundle-linux-x64` artifact retained for seven days.

- [ ] **Step 1: Add the isolated CI job**

Use SQL Server 2022 with a CI-only password. Restore the pinned tool, build the bundle, run it once against the empty CI database, rerun it against the current database, and require both commands to exit 0. Upload only the bundle with seven-day retention. Do not reference production secrets or mutate a production environment.

- [ ] **Step 2: Validate workflow syntax and diff**

Review the YAML, run any already-installed workflow validator, and run `git diff --check`.

- [ ] **Step 3: Commit**

Commit: `ci: verify migration bundle`

---

### Task 5: Document and verify the launch-critical result

**Files:**
- Modify: `README.md`
- Modify: `docs/production-configuration.md`
- Modify: `docs/architecture.md`
- Modify: `docs/portfolio-roadmap.md`

**Interfaces:**
- Produces: exact local and production migration commands, permission boundary, rollout order, and rollback rule.

- [ ] **Step 1: Update operator documentation**

Document local tool restoration, Compose migration behavior, manual bundle execution, separate production migration/API database identities, deployment order, failure behavior, and the rule against automatic destructive down-migrations. Remove every claim that the API migrates during startup.

- [ ] **Step 2: Run full verification**

Run Release restore, build, complete backend tests, frontend unit tests, lint, production build, E2E infrastructure from empty state, the complete Playwright matrix with one worker, E2E cleanup without deleting persistent user volumes, and `git diff --check`.

- [ ] **Step 3: Reevaluate the completed decision**

Confirm replicas cannot migrate independently, migration/API permissions can be separated, failures block rollout, no host-specific coupling was introduced, local startup remains practical, and no unrelated production-gap work entered the diff.

- [ ] **Step 4: Commit, push, and verify hosted CI**

Commit: `docs: document deployment migration workflow`. Review the complete commit series, push, and track backend, frontend, E2E, and migration-bundle jobs for the exact SHA. Do not call the milestone complete until all jobs succeed.
