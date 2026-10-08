# Pursuit deployment migration boundary design

Date: 2026-10-08

## Objective

Move schema migration out of API startup and into an explicit, one-time deployment step without breaking local Compose, integration tests, or the existing application startup contract. The deployed API identity must need data-access permissions only; a separate migration identity owns schema-change permissions.

This is the first production-gap milestone. Transactional messaging, dependency readiness, rate limits, production hosting, observability, and backups remain separate milestones.

## Current state

- `Program.cs` calls `Database.MigrateAsync()` before administrator bootstrap and before accepting traffic.
- The API process therefore needs schema permissions and its availability is coupled to migration duration and failure.
- Local Compose starts the API after SQL Server becomes healthy, but has no one-time migration service.
- Integration tests rely on API startup to create the Testcontainers database schema.
- CI builds and tests the application but does not create or validate a deployable migration artifact.
- The API Docker image contains only the published application.

## Decision review

### Option 1: keep migrations in API startup

This is the smallest change but is rejected. Multiple replicas can contend during rollout, the runtime identity retains excessive permissions, migration failure is reported as an application-startup failure, and schema rollout cannot be inspected or stopped independently.

### Option 2: run `dotnet ef database update` during deployment

This separates migration timing but is rejected for production. It requires the SDK, EF tool, source tree, and project evaluation in the deployment environment. That increases the deployment surface and makes the executable being reviewed different from the executable being run.

### Option 3: build an EF Core migration bundle and run it as a one-time container job

This is selected. The bundle is versioned with the application image, can run without the SDK or source tree, exits non-zero on failure, and can use a privileged migration connection independently from the API connection. It is portable across container platforms and preserves a fast path to deployment.

## Architecture

### Design-time context creation

Add an `IDesignTimeDbContextFactory<AppDbContext>` in the API composition project. It will:

- read only `ConnectionStrings__DefaultConnection`;
- create SQL Server `DbContextOptions` directly;
- supply a non-user design-time `IDbContextScope` whose tenant and user identifiers are null;
- avoid constructing the web host or connecting to Redis, RabbitMQ, or Blob Storage;
- fail with a configuration-key-only message when the connection string is absent.

Pin `dotnet-ef` in a repository-local tool manifest to the same EF Core feature and patch version used by the projects. This makes local and CI bundle generation reproducible.

### Container artifact

The Docker build stage will restore the local tool and create a Linux migration bundle from the existing Infrastructure migrations, using the API project as the startup project. The runtime image will contain both:

- `Pursuit.API.dll`, used by the normal API container command;
- `/app/migrations/efbundle`, invoked only by a migration job.

The API entry point remains the application. Merely starting or scaling an API container will not execute schema changes.

### Runtime startup

Remove `Database.MigrateAsync()` from `Program.cs`. Administrator bootstrap remains after application construction because it operates on an already-migrated schema. If the schema is missing or outdated, startup may fail when bootstrap or requests access it; deployment orchestration must therefore require successful migration completion before starting the new API revision.

### Local Compose

Add a `migrate` service that:

- uses the same image/build as the API;
- waits for SQL Server health;
- runs `/app/migrations/efbundle` with the local SQL connection;
- exits successfully when the database is already current;
- prevents API startup when migration exits non-zero via `service_completed_successfully`.

Local Compose may continue using the same SQL credential for API and migration because it is a developer environment. Documentation must state that production uses two different identities.

### Integration and browser tests

Removing startup migration would otherwise break tests that create a fresh SQL container. Test infrastructure will explicitly migrate its disposable database after SQL startup and before the test server is created. This is test provisioning, not application startup behavior.

The browser E2E Compose stack will receive its own one-time migration service, and Playwright's API web server will start only after the E2E infrastructure command reports successful migration.

### CI

CI will add a migration-boundary job or steps that:

1. restore the pinned local tool;
2. build the migration bundle;
3. upload the bundle as a short-retention workflow artifact;
4. run the bundle against a new SQL Server database;
5. verify all expected migrations are recorded;
6. run the same bundle again and verify it is idempotent;
7. verify the API can start against the migrated database without running migrations itself.

The CI workflow will not contain production credentials or mutate a production database. Provider-specific deployment will later run the reviewed image as a one-time job using protected production secrets.

### Production database identities

Production uses two connection strings:

- migration identity: schema creation and alteration permissions, used only by the one-time job;
- API identity: only the data permissions required by normal application queries and commands.

Exact SQL grants depend on the managed SQL provider and will be finalized with the hosting design. The repository will document the permission boundary without embedding credentials.

## Failure and rollback behavior

- Bundle failure exits non-zero and blocks the API rollout.
- An up-to-date database produces no schema changes and exits successfully.
- The previous API revision remains the rollback target while the database migration must be backward compatible with that revision.
- Automatic down-migrations are not used in production because they may destroy data.
- Each schema-changing release must document its application rollback compatibility and, when needed, a forward-fix migration.
- Database backup and restore are a later hosting/operations milestone, but deployment documentation will identify them as prerequisites before destructive migrations.

## Verification

Required automated evidence:

- bundle creation succeeds from a clean checkout;
- bundle migrates an empty SQL Server database to the latest migration;
- rerunning the bundle changes nothing and succeeds;
- the API no longer invokes migration at startup;
- integration tests explicitly provision schema and all existing backend tests pass;
- local Compose starts `sqlserver -> migrate -> api` in order;
- E2E infrastructure provisions schema before browser tests;
- frontend unit, lint, build, and complete Playwright matrix remain green;
- `git diff --check` is clean.

Required manual evidence before a live deployment:

- migration job uses a protected migration connection;
- API uses a lower-privilege connection;
- failed migration prevents revision promotion;
- rollback compatibility is recorded for the release.

## Documentation updates

Update:

- `README.md` with local migration commands, Compose behavior, artifact generation, and production rollout order;
- `docs/production-configuration.md` with separate migration/API identities and removal of startup migration;
- `docs/architecture.md` with the implemented deployment boundary;
- `docs/portfolio-roadmap.md` only after verification proves the milestone complete.

## Non-goals

- Choosing or provisioning the production cloud provider.
- Running migrations automatically against a live database from CI.
- Transactional outbox implementation.
- Dependency readiness checks.
- Rate limiting, upload hardening, monitoring, or backup automation.
- Kubernetes-specific jobs.

## Acceptance criteria

The milestone is complete only when the migration executable is reproducible, the API contains no automatic migration call, clean and current databases are both verified, local and E2E workflows remain one-command operations, all existing suites pass, documentation matches the behavior, the change is committed and pushed, and hosted CI succeeds.
