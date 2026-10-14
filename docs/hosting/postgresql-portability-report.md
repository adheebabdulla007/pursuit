# PostgreSQL portability evidence

**Decision:** Ready for private provider probes. Not ready for public launch.
The migration-runner security fix and local PostgreSQL portability gates pass;
hosted-provider and public-demo gates remain open.

**Candidate:** the security-fix commit containing this report, based on
`ce1d3d9`. Record and verify its exact pushed SHA before provider probes.

**Verification date:** 2026-10-14

This report covers the provider-neutral PostgreSQL cutover and the remediation of
Codex Security occurrence `occ_4b5eebd24daf1e3daa0cc18b`. It does not claim that
Neon, Render, Vercel, Upstash, CloudAMQP, hosted TLS, cold-start behaviour,
provider quotas, or public-demo data controls have passed. Those checks require
private provider accounts and sanitized hosted evidence.

## Current security-fix evidence

| Gate | Observed result | Status |
| --- | --- | --- |
| Shared PostgreSQL policy | Rejects omitted TLS, `Disable`, `Prefer`, `Require`, `VerifyCA`, certificate trust bypass, GSS preference and malformed strings; accepts `VerifyFull` with `GSS Encryption Mode=Disable` and no bypass | Pass |
| Production runner | `Testing` plus loopback plus `SSL Mode=Disable` exited 1 with the fixed `VerifyFull` error | Pass |
| Command-line override | Adding `--connection` did not replace `ConnectionStrings__DefaultConnection`; the production runner still rejected the environment value | Pass |
| Error redaction | Marker passwords were absent from plaintext rejection, malformed-connection and migration-failure output | Pass |
| Local-test runner | `Testing` plus loopback plus `Disable` crossed the policy boundary and reached the migration delegate; an external hostname was rejected before migration | Pass |
| Clean failure behaviour | Expected validation and provider failures return exit code 1 with sanitized messages instead of unhandled exceptions | Pass |
| Focused configuration/deployment tests | 44 passed, 0 failed | Pass |
| Release solution build | 0 warnings, 0 errors | Pass |
| Frontend | ESLint passed; 83 Vitest tests passed; production build passed | Pass |
| Complete backend suite | 116 passed, 0 failed with Docker-backed PostgreSQL, Redis and RabbitMQ | Pass |
| Docker image and embedded runner | Clean production image built; `/app/migrations/Pursuit.Migrations` exists and is executable | Pass |
| Production container boundary | Plaintext Production connection exited 1 with the fixed `VerifyFull` error and did not expose the marker password | Pass |
| Empty/current container database | Fresh PostgreSQL migration completed, then the idempotent rerun completed | Pass |
| API health | Rebuilt Compose API returned HTTP 200 | Pass |
| Linux GSS behaviour | Npgsql 10 missing-library warning reproduced, then disappeared after all password-authenticated connections explicitly disabled GSS preference | Pass |
| Browser journey | Chromium, Firefox, WebKit and mobile Chromium: 25 passed, 3 configured skips, 0 failed | Pass |

The production and local-test Windows artifacts produced different SHA-256
digests, confirming that the local plaintext exception changes the artifact. The
production artifact is the only one eligible for retention or deployment.

## Migration boundary

- `Pursuit.Migrations` reads only `ConnectionStrings__DefaultConnection`; there
  is no supported command-line connection input.
- The production build has no plaintext exception compiled into it.
- Local Compose and CI compile a separate artifact. It permits plaintext only
  when the environment is `Development` or `Testing`, SSL mode is explicitly
  `Disable`, GSS encryption is explicitly `Disable`, and the host is loopback or
  a repository Compose PostgreSQL name.
- Production requires `GSS Encryption Mode=Disable`; Pursuit uses PostgreSQL
  password authentication and must keep `VerifyFull` TLS as the selected
  transport instead of allowing Npgsql 10 to prefer GSS.
- The runner validates before constructing `DbContextOptions` or calling
  `MigrateAsync`.
- Validation and migration failures emit fixed, credential-free messages and
  return non-zero.
- The API does not migrate the database during startup. A one-shot migration job
  must complete before API startup.
- `dotnet ef database update` is not a deployment boundary. EF applies its
  `--connection` override after design-time context creation, so deployments must
  use the reviewed runner.

## Browser rerun note

The first full browser run had one WebKit-only timeout after the job-title field
was transiently empty. The same create-job path passed in the separate boundary
journey, passed three consecutive focused WebKit reruns, and the complete matrix
then passed. No deterministic product defect was reproduced; treat recurrence as
a test-stability investigation, not as a waived failure.

## Remaining gates

1. Commit the reviewed patch, push only `codex/portable-demo-hosting`, and verify
   hosted CI for that exact commit.
2. Create free provider accounts without a payment method. Keep credentials only
   in provider secret stores or process environment.
3. Verify Neon `VerifyFull`, Render proxy and key persistence, Upstash TLS,
   CloudAMQP TLS/durable delivery, cold start, quotas and complete hosted role
   journeys.
4. Complete the transactional outbox before RabbitMQ can be treated as reliable
   public-demo infrastructure.
