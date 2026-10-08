# Pursuit

[![CI](https://github.com/adheebabdulla007/pursuit/actions/workflows/ci.yml/badge.svg)](https://github.com/adheebabdulla007/pursuit/actions/workflows/ci.yml)

Pursuit is a multi-tenant job board built with ASP.NET Core 10 and React 19. Employers can post jobs, job seekers can search and apply with a resume, and administrators can manage accounts.

I built the project to practise the parts of backend work that become difficult after the first CRUD screen: tenant boundaries, session revocation, concurrent refresh requests, browser security, caching, messaging, and repeatable tests.

## Try it locally

You need Docker Desktop or Docker Engine with Docker Compose. Confirm that the Docker engine is running before starting the stack:

```text
docker info
```

Create the local environment file.

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

macOS or Linux:

```bash
cp .env.example .env
```

Then start the complete stack:

```text
docker compose up --build -d --wait
```

Open [http://localhost:5173](http://localhost:5173). The local administrator account is documented in [the demo guide](docs/demo-guide.md).

The `migrate` container applies the EF Core bundle before the API starts. A migration failure leaves the API stopped. The first start downloads several container images and creates an empty database. Follow the [five-minute product flow](docs/demo-guide.md#five-minute-product-flow) to create an employer, publish a job, and apply as a job seeker.

Stop the stack with:

```powershell
docker compose down
```

Use `docker compose down -v` when you also want to delete the local SQL Server volume.

If Compose reports that it cannot connect to the Docker API or daemon, start Docker Desktop or the Docker service and rerun `docker info` before trying again.

### Run the API in Visual Studio and the client in Vite

The Development API uses local SQL Server Express and expects Redis, RabbitMQ, and Azurite on ports 6379, 5672, and 10000. From the repository root, start those services before launching the API:

```text
docker compose up -d --wait redis rabbitmq azurite
```

Update the SQL Express database before starting the API:

```powershell
dotnet tool restore
$env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=PursuitDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet dotnet-ef database update --project src/Pursuit.Infrastructure --startup-project src/Pursuit.API
Remove-Item Env:ConnectionStrings__DefaultConnection
```

Run the `http` profile of `Pursuit.API` in Visual Studio, or use `dotnet run --project src/Pursuit.API`. In `client`, run `npm run dev`. The client environment file must point `VITE_API_BASE_URL` to `http://localhost:5146`. Keep the three dependency containers running while applying to jobs; résumé upload requires Azurite, and job search uses Redis.

## What works

- Employer and job-seeker registration with role-specific validation
- Job creation, editing, deletion, search, and offset pagination
- Resume upload to Azure Blob Storage or Azurite
- Application submission with a database uniqueness constraint
- Administrator statistics, user paging, and account activation controls
- JWT access tokens plus rotating refresh tokens stored in HttpOnly cookies
- Refresh-token replay detection and account-wide session revocation
- CSRF protection for cookie-authenticated browser mutations
- Tenant-aware query filters and service-level ownership checks
- Redis-backed job search caching with cache invalidation
- RabbitMQ application-submitted events and a background consumer
- Production startup configuration checks and opt-in administrator bootstrap
- A versioned EF Core migration bundle that runs before API deployment

## Architecture

```mermaid
flowchart LR
    browser["React client"] -->|HTTP and cookies| api["ASP.NET Core API"]
    migration["One-time migration job"] --> sql["SQL Server"]
    api --> sql["SQL Server"]
    api --> redis["Redis"]
    api --> blob["Azure Blob Storage or Azurite"]
    api -->|Publish and consume events| rabbitmq["RabbitMQ"]
```

The API follows a four-project structure:

- `Pursuit.Domain`: entities and enums
- `Pursuit.Application`: use cases, DTOs, validation, and interfaces
- `Pursuit.Infrastructure`: EF Core, Redis, RabbitMQ, identity, and blob storage
- `Pursuit.API`: controllers, authentication, middleware, and composition root

Read [the architecture notes](docs/architecture.md) for request flows, trust boundaries, design decisions, and current failure modes.

## Security work

The repository includes tested fixes for several easy-to-miss authentication problems:

- Public registration accepts only `Employer` and `JobSeeker`; numeric enum values and `Admin` are rejected.
- A refresh token is rotated inside a SQL transaction with row-level locking.
- Reuse of a rotated refresh token revokes every refresh token for that account.
- Access tokens carry a security version. Deactivation and password-sensitive account changes invalidate existing sessions.
- Browser mutations require an antiforgery token and an allowed origin.
- Production startup rejects missing secrets, weak signing keys, unencrypted SQL connections, RabbitMQ guest credentials, and emulator blob storage.

The implementation notes live in [`docs/`](docs/).

## Verification

The GitHub Actions workflow runs four jobs on every push and pull request:

| Job | Checks |
| --- | --- |
| Backend | Release build and 81 SQL Server, Redis, RabbitMQ, API, and configuration tests |
| Migration bundle | Linux bundle build, empty-database migration, idempotent rerun, and retained artifact |
| Frontend | ESLint, 83 Vitest component tests, and a production build |
| Browser | Playwright employer-to-job-seeker journey across Chromium, Firefox, and WebKit |

Run the same build and test gates locally:

```powershell
dotnet restore Pursuit.slnx
dotnet build Pursuit.slnx --configuration Release --no-restore
dotnet test Pursuit.slnx --configuration Release --no-build

Set-Location client
npm ci
npm run lint
npm test
npm run build
```

The backend test suite uses Testcontainers, so Docker must be running.

## Main technology choices

| Area | Choice |
| --- | --- |
| API | ASP.NET Core 10, C# 14 |
| Data | EF Core 10, SQL Server |
| Client | React 19, TypeScript 6, Vite 8, Tailwind CSS 4 |
| Cache | Redis |
| Messaging | RabbitMQ |
| Files | Azure Blob Storage, Azurite for local work |
| Tests | xUnit, Testcontainers, Vitest, Testing Library, Playwright |
| Delivery | Docker Compose and GitHub Actions |

## Repository map

```text
Pursuit/
├── src/                         API and backend projects
├── client/                      React client and browser tests
├── tests/Pursuit.IntegrationTests/
├── docs/                        Design and security notes
├── .github/workflows/ci.yml
├── docker-compose.yml           Complete local stack
└── docker-compose.e2e.yml       Browser-test dependencies
```

## Current limits

These are the next engineering tasks, recorded here so the repository does not claim more than it proves:

1. Application rows and RabbitMQ messages are written separately. A transactional outbox is planned to prevent lost notifications when the broker is unavailable.
2. `/health` is a liveness endpoint. Dependency readiness checks still need to cover SQL Server, Redis, RabbitMQ, and Blob Storage.
3. API page sizes, search text, uploads, and request bodies need server-side bounds before public exposure.
4. Authentication and upload endpoints need rate limits backed by load-test results.
5. Job search uses SQL `Contains` and offset pagination. Larger data sets will need better indexes, keyset pagination, and a dedicated search strategy.

The order and reasoning are in [the portfolio roadmap](docs/portfolio-roadmap.md).

## Interview walkthrough

[The interview guide](docs/interview-guide.md) contains a five-minute code tour, the decisions I would discuss, and direct answers to likely follow-up questions.

## License

This project is available under the [MIT License](LICENSE).
