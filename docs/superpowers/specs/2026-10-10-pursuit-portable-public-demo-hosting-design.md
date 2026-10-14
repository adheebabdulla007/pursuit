# Pursuit portable public demo hosting design

## Purpose

Publish Pursuit as a zero-cost portfolio demonstration that a reviewer can
explore through the complete Employer, Job Seeker and Admin journeys. The
deployment is reproducible and portable, not a commercial production system or
an uptime-guaranteed service.

The public environment contains synthetic data only. It must never accept or
retain a visitor's real resume, personal information or production workload.

## Constraints

- No Azure subscription and no payment card.
- Preserve the current ASP.NET Core 10, React, Redis and RabbitMQ architecture.
- New application features must remain host-portable.
- No provider SDK may enter the Application or Domain projects.
- A free-tier failure must degrade a non-authoritative feature or stop the
  release; it must not silently corrupt application state.
- No public deployment occurs until hosted authentication, authorization,
  tenant isolation and complete role journeys pass.

## Corrected hosting decision

- Vercel serves the static React client and forwards `/api/*` to the API.
- Render runs the existing Linux ASP.NET Core container as a free web service.
- Neon supplies PostgreSQL over certificate-validated TLS.
- Upstash supplies Redis over TLS and remains optional for correctness.
- CloudAMQP supplies RabbitMQ over TLS after the transactional outbox tranche.

MonsterASP.NET is rejected. Its free service has no production commitment or
backup guarantee, and its documented SQL Server connection modes conflict with
Pursuit's production requirement to encrypt the database connection and verify
the server certificate.

No free provider is treated as permanent. Long-term resilience comes from
standard protocols, deterministic provisioning, tested migrations and a
documented restore path.

## Database portability boundary

PostgreSQL becomes Pursuit's only relational provider. Maintaining SQL Server
and PostgreSQL simultaneously would double migration, query and integration-test
risk without helping the public demo.

The domain model, application interfaces and API contracts remain unchanged.
Provider-specific configuration stays in Infrastructure and deployment code.
Local development and integration tests use PostgreSQL, so CI exercises the same
database engine as the hosted demo.

Production PostgreSQL connections must use `SSL Mode=VerifyFull`, which requires
encryption and validates both the certificate chain and hostname. Development
containers may explicitly use `SSL Mode=Disable`; that exception must never pass
Production startup validation. Both paths explicitly use
`GSS Encryption Mode=Disable` because Pursuit uses password authentication and
must not prefer GSS over the required TLS transport.

Schema changes continue to use the reviewed `Pursuit.Migrations` artifact executed
before the API revision is promoted. The API never migrates its database during
startup.

## Messaging correctness boundary

SQL is authoritative. Applying for a job must commit the application and an
outbox record in one database transaction. A dispatcher publishes pending
outbox messages to RabbitMQ and records successful delivery. Broker downtime
therefore delays notification processing without making an application request
ambiguous or losing the event.

The free Render process may sleep, so the design does not promise immediate
background delivery while the site is idle. Pending messages resume when the API
wakes. RabbitMQ messages and Redis entries never become the source of truth.

## Public demo boundary

- Registration is disabled in hosted configuration and enforced by the API.
- Fixed synthetic Job Seeker, Employer and Admin accounts are seeded.
- Demo mode rejects multipart visitor uploads at the API boundary.
- Applying uses a packaged synthetic resume stored outside the public asset tree.
- A deterministic reset restores the scenario without depending on visitor data.
- Shared-account collisions, duplicate applications and changed job state return
  explicit UI messages rather than generic failures.
- Exact production and staging origins are allowlisted; random preview origins
  are excluded.
- Authentication cookies remain `HttpOnly`, `Secure`, `SameSite=Strict` and
  host-only through the Vercel route.

## Release order

1. Build and test the reviewed commit.
2. Export the synthetic hosted database before a schema change.
3. Run `Pursuit.Migrations` with a migration-only identity and a `VerifyFull` connection.
4. Deploy a private Render candidate and wait for readiness.
5. Run API smoke checks directly against Render.
6. Deploy one fixed Vercel staging candidate.
7. Run authentication, CSRF, role, tenant, application and cold-start journeys.
8. Promote the API revision, then the Vercel client.
9. Run the public smoke journey and record the deployed commit.

Automatic provider deployment from an unchecked push is disabled. A failed
gate leaves the previous public revision in place.

## Acceptance gates

The hosting choice is accepted only when all of these are observed against the
hosted candidate:

1. Render and Neon accounts can be created without a payment card.
2. The API starts within Render's memory and startup limits after sleeping.
3. PostgreSQL migrations run twice successfully and Production requires
   certificate-validated TLS.
4. Redis failure becomes an immediate cache miss without breaking searches.
5. RabbitMQ outage leaves a committed application plus a pending outbox record;
   recovery publishes it once without duplicating notifications.
6. Vercel preserves cookies, Origin, CSRF headers, status codes and non-cacheable
   API responses.
7. The complete Employer to Job Seeker to Employer journey passes in Chromium,
   Firefox and WebKit at desktop and mobile sizes.
8. Direct cross-tenant access, foreign origins, visitor registration and visitor
   file uploads are rejected.
9. No real personal data, provider credential or connection string appears in
   source, logs, artifacts or browser payloads.
10. The README states the cold-start and free-tier limitations without calling
    the environment production.

Failure of any security, database, authentication, tenant or end-to-end gate
blocks publication. Provider availability limits are documented, not hidden.

## Explicit non-goals

- Commercial production hosting or an uptime guarantee.
- Real applicant data or visitor file storage.
- Multi-region availability.
- Keeping two relational database providers.
- Bypassing TLS validation to satisfy a free provider.
