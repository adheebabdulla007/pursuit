# Interview guide

## A five-minute code tour

### First minute: product and shape

Pursuit is a multi-tenant job board. Employers create jobs, job seekers apply with resumes, and administrators manage accounts. The React client talks to one ASP.NET Core API backed by SQL Server. Redis caches job searches, RabbitMQ carries application events, and Blob Storage holds resumes.

Show the diagram in [`architecture.md`](architecture.md), then explain why a modular monolith fits a one-developer project better than several independently deployed services.

### Second minute: tenant isolation

Open `AppDbContext.cs`, `CurrentUserService.cs`, and one tenant-sensitive application service.

Explain that tenant identity comes from a validated JWT claim. EF Core query filters protect ordinary reads, and services check ownership before writes. Point out the named repository methods that deliberately bypass filters for authentication and administrator work.

### Third minute: session security

Open `AuthService.cs`, `RefreshTokenRepository.cs`, and `TokenService.cs`.

Explain:

- Access tokens expire within 15 minutes.
- Refresh tokens are random, hashed at rest, and rotated.
- SQL row locks serialize concurrent rotation.
- Reuse of an old refresh token revokes the user's sessions.
- A security-version claim makes account deactivation effective before access-token expiry.

### Fourth minute: browser and startup security

Open `BrowserCsrfMiddleware.cs` and `StartupConfigurationValidator.cs`.

Explain why SameSite cookies alone are not the whole CSRF boundary, how allowed origins are checked, and why production settings fail before the application touches the database.

### Fifth minute: tests and unfinished work

Open `.github/workflows/ci.yml` and one integration test. Describe the real SQL Server, Redis, and RabbitMQ containers used by the backend suite and the Playwright browser journey.

Finish with the most important known gap: database writes and RabbitMQ publication need a transactional outbox. State the failure case and the planned fix clearly.

## Questions I should be ready to answer

### Why SQL Server?

Users, tenants, jobs, applications, and refresh tokens have clear relationships and uniqueness rules. Transactions and database constraints matter for duplicate applications and token rotation. A relational database fits those requirements directly.

### Why Redis?

Job search results are read often and can be recomputed from SQL. That makes them safe cache entries. If Redis fails, requests fall back to SQL and the application remains correct.

### Why RabbitMQ?

Application notifications do not need to block the main request forever. A broker lets a worker process that work separately. The current direct-publish path exposes a consistency problem, which is why the next implementation is a SQL outbox.

### Why keep one deployable API?

The codebase has one owner and tightly related data. One process gives simpler transactions, tests, local setup, and releases. Project boundaries keep the code organized. I would split a service only after independent ownership or load made the extra network boundary worthwhile.

### How is tenant isolation enforced?

Validated identity claims set the current tenant. EF query filters constrain ordinary reads, service methods check ownership, and entities carry tenant IDs. Authentication and administrator operations use explicit filter-bypass repository methods because they cross tenant boundaries by design.

### What happens if two users submit the same application request?

The service checks first for a useful error, and SQL Server has a unique `(JobId, ApplicantId)` index as the final concurrency guard. One insert can succeed.

### What happens if two refresh requests arrive together?

Both requests try to lock the same user row inside a transaction. One rotates the token first. The second sees the old token as revoked, treats reuse as suspicious, and revokes the account's refresh sessions.

### What would you change before a public production release?

I would add the transactional outbox, move migrations to a deployment job, add dependency readiness checks, bound resource-heavy inputs, add rate limits, and test the container under representative load.

### Which design would you reconsider with more traffic?

Offset pagination and SQL `Contains` are the first data-access candidates. I would measure query plans and response times, add supporting indexes, move deep pages to keyset pagination, and adopt full-text search when search quality or data volume requires it.

## Project stories for behavioral rounds

### Security boundary correction

Public registration originally accepted any parseable enum value, including numeric values and `Admin`. I traced the request from JSON binding through validation and persistence, added one shared role policy at the service boundary, wrote negative API tests, and had a separate review look for bypasses.

### Concurrency correction

Refresh-token rotation originally allowed two requests to race. I moved the operation into a SQL transaction, locked by user, and tested concurrent requests against a real SQL Server container. This taught me to keep a database constraint or lock behind application-level checks.

### Production configuration correction

Tracked base settings contained local credentials that a Production environment could inherit. I moved local defaults into the Development file, added early validation, disabled administrator bootstrap by default, serialized bootstrap across replicas, and tested unsafe production settings.

For a STAR answer, add the real context in which you built the project, the time you spent, what you personally decided, and what feedback changed your approach.
