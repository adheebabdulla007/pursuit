# Pursuit hosting compatibility gate implementation plan

> **Superseded on 2026-10-10.** The SQL Server/MonsterASP.NET direction in this
> document was rejected before public deployment. The active design is
> `docs/superpowers/specs/2026-10-10-pursuit-portable-public-demo-hosting-design.md`,
> the active implementation plan is
> `docs/superpowers/plans/2026-10-10-pursuit-postgresql-portability.md`, and current
> verified evidence is in `docs/hosting/postgresql-portability-report.md`.
> The remaining content is retained as historical decision context only and must
> not be executed as the current hosting plan.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove or reject the Vercel plus MonsterASP.NET hosting choice against Pursuit's real SQL Server, cookie, CSRF, Redis, RabbitMQ, IIS, memory and browser behavior before building the public demo layer.

**Architecture:** Run the work on an unmerged `codex/hosting-compatibility-probe` branch. Start with provider-only probes, then add the smallest configuration adapters needed to exercise the real application. Deploy a private staging candidate, route a fixed Vercel staging origin to it, and record pass or fail evidence for every gate. Mergeable code is limited to provider-neutral configuration and test tooling; failed hosting experiments stay off `main`.

**Tech Stack:** .NET 10.0.401, ASP.NET Core/IIS, EF Core 10.0.8, SQL Server, StackExchange.Redis 3.0.7, RabbitMQ.Client 7.2.1, React 19, Vite 8, Vercel external rewrites, Playwright 1.63, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-10-09-pursuit-zero-cost-public-demo-hosting-design.md`

## Global constraints

- Spend remains exactly $0; do not add a payment method or enable automatic paid upgrades.
- Use synthetic accounts and files only. Do not place real applicant data in any hosted service.
- Keep SQL Server. Do not start a PostgreSQL migration in this tranche.
- Preserve the Linux image, local Docker Compose, Azurite support and current local environment variables.
- Keep `HttpOnly`, `Secure`, `Path=/` and `SameSite=Strict` cookies. CSRF checks remain active.
- Use one fixed Vercel staging origin. Random preview origins are not trusted origins.
- Store secrets only in GitHub or provider secret stores. Logs and committed files may contain key names, never values.
- The probe branch is rejected if required SQL syntax, persistent Data Protection, proxy cookies, CSRF, durable RabbitMQ delivery, the complete browser journey, or the 220 MB memory target fails.
- No public launch, public demo accounts, nightly reset or README live link belongs in this plan. Those start only after this gate passes.

## Review focus

- A connection URI containing escaped credentials or a non-root RabbitMQ virtual host must parse correctly and must never be printed.
- Redis or RabbitMQ unavailability must have bounded behavior; Redis returns an immediate cache miss and RabbitMQ reports a failed dependency without hanging startup.
- A Vercel rewrite must preserve multiple `Set-Cookie` headers, cookie paths, `Origin`, multipart bodies, 401/403 responses and CSRF headers without caching API responses.
- IIS recycle and redeploy must retain Data Protection keys, while a missing or unwritable key directory must stop production startup.
- The hosted flow must detect shared or dirty staging data and create isolated synthetic records rather than relying on test order.

---

### Task 1: Establish the probe boundary and evidence ledger

**Files:**
- Create: `docs/deployment/hosting-compatibility-runbook.md`
- Create: `docs/deployment/hosting-compatibility-results.md`
- Modify: `.gitignore`

**Interfaces:**
- Produces: one operator checklist with gate IDs `HOST-01` through `HOST-12`, and one evidence table with `Pending`, `Pass`, `Fail` or `Blocked` status.

- [ ] **Step 1: Create and verify the isolated branch**

Confirm `main` has no uncommitted changes, create `codex/hosting-compatibility-probe` from the reviewed plan commit, and record the starting SHA. Do not use a destructive reset and do not copy local secrets into the worktree.

- [ ] **Step 2: Write the runbook**

For each compatibility check in the spec, record its command or browser action, expected result, failure threshold, evidence location and cleanup action. Include provider account creation as an explicit human checkpoint because account verification and secret retrieval cannot be automated safely.

- [ ] **Step 3: Write the results template**

Create one row per gate with columns `Gate`, `Commit`, `Environment`, `Observed result`, `Evidence`, `Status`, `Decision`. State that a required failure selects the MonsterASP.NET single-origin fallback probe; a fallback failure stops this hosting tranche.

- [ ] **Step 4: Protect local provider state**

Ignore `.vercel/`, Web Deploy publish profiles, provider export files, probe output containing environment details, and local IIS publish artifacts. Verify tracked examples use placeholders only.

- [ ] **Step 5: Validate the documentation boundary**

Run `git diff --check` and `git grep -n -I -E "(amqps|rediss|Server=|Password=|VERCEL_TOKEN|publishUrl)" -- docs/deployment .gitignore`. Review every match and remove any credential value.

- [ ] **Step 6: Commit**

Commit: `docs: define hosting compatibility gate`

---

### Task 2: Build provider connectivity probes

**Files:**
- Create: `tools/Pursuit.HostingProbe/Pursuit.HostingProbe.csproj`
- Create: `tools/Pursuit.HostingProbe/Program.cs`
- Create: `tools/Pursuit.HostingProbe/ProbeOptions.cs`
- Create: `tools/Pursuit.HostingProbe/ProbeReport.cs`
- Create: `tools/Pursuit.HostingProbe/SqlServerProbe.cs`
- Create: `tools/Pursuit.HostingProbe/RedisProbe.cs`
- Create: `tools/Pursuit.HostingProbe/RabbitMqProbe.cs`
- Create: `tests/Pursuit.HostingProbeTests/Pursuit.HostingProbeTests.csproj`
- Create: `tests/Pursuit.HostingProbeTests/ProbeReportTests.cs`
- Create: `tests/Pursuit.HostingProbeTests/RabbitMqProbeTests.cs`
- Modify: `Pursuit.slnx`

**Interfaces:**
- Consumes: `HOST_SQL_CONNECTION`, `HOST_REDIS_CONNECTION`, and `HOST_RABBITMQ_URI` from the process environment.
- Produces: `ProbeReport` with provider name, duration, sanitized endpoint host, checks, and exit status; process exit code `0` only when every selected check passes.

- [ ] **Step 1: Write failing report-redaction tests**

Test that SQL passwords, Redis passwords, RabbitMQ user info, query strings and original URIs never appear in console JSON or exception summaries. Test that a failed check produces exit code `1` and names the failed operation.

- [ ] **Step 2: Write failing RabbitMQ URI tests**

Cover `amqps://`, percent-encoded credentials, port `5671`, a non-root virtual host and a missing TLS scheme. Require TLS and reject `amqp://` for a hosted probe.

- [ ] **Step 3: Verify the tests fail because the probe types are absent**

Run: `dotnet test tests/Pursuit.HostingProbeTests/Pursuit.HostingProbeTests.csproj --configuration Release`

Expected: FAIL at compile time for missing probe types.

- [ ] **Step 4: Implement the probe shell and redacted report**

Parse `--sql`, `--redis`, `--rabbit` and `--all`. Fail fast when the selected environment variable is missing. Emit one JSON document and never serialize raw configuration or exception connection strings.

- [ ] **Step 5: Implement the SQL Server checks**

Open an encrypted connection, query the server version, acquire and release `sys.sp_getapplock`, execute `UPDLOCK` plus `HOLDLOCK` inside a rolled-back transaction, and run a bounded recursive CTE. Use temporary objects or rollback so the probe leaves no application data.

- [ ] **Step 6: Implement the Redis checks**

Use `ConfigurationOptions.Parse`, require SSL, set a namespaced key with a 60-second expiry, read it, and delete it. Cap connect and command timeouts at 5 seconds.

- [ ] **Step 7: Implement the RabbitMQ checks**

Set `ConnectionFactory.Uri` from the TLS URI. Declare a uniquely named durable queue, publish a persistent message, close the first connection, reconnect, consume and acknowledge the same message, then delete the probe queue in `finally`.

- [ ] **Step 8: Run local tests and disposable dependency checks**

Run the new test project, then point each probe at local Docker dependencies using test-only environment variables. Expected: all selected checks pass and output contains no credentials.

- [ ] **Step 9: Run the hosted preliminary gate**

Create free MonsterASP.NET SQL Server, Upstash and CloudAMQP resources without adding a card. Run the probes from an approved operator machine and GitHub Actions. Record sanitized output and quotas under `HOST-03`, `HOST-04`, `HOST-09` and `HOST-10`. Stop here if SQL syntax or either TLS service fails.

- [ ] **Step 10: Commit**

Commit: `test: add hosted dependency compatibility probes`

---

### Task 3: Add provider-neutral TLS configuration adapters

**Files:**
- Create: `src/Pursuit.Infrastructure/Messaging/RabbitMqConnectionFactoryBuilder.cs`
- Modify: `src/Pursuit.Infrastructure/DependencyInjection.cs`
- Modify: `src/Pursuit.API/Configuration/StartupConfigurationValidator.cs`
- Modify: `src/Pursuit.API/appsettings.json`
- Create: `tests/Pursuit.IntegrationTests/Configuration/RabbitMqConfigurationTests.cs`
- Modify: `tests/Pursuit.IntegrationTests/Configuration/StartupConfigurationValidatorTests.cs`

**Interfaces:**
- Consumes: optional `RabbitMqSettings:Uri`; existing `Host`, `Port`, `Username`, `Password` remain the local fallback.
- Produces: `RabbitMqConnectionFactoryBuilder.Build(IConfiguration) -> ConnectionFactory`.

- [ ] **Step 1: Write failing configuration tests**

Test that an `amqps://` URI sets TLS, host, port, credentials and virtual host. Test that local split settings still create the existing non-TLS factory. Test that production rejects `amqp://`, conflicting URI and split settings, missing credentials, and guest credentials.

- [ ] **Step 2: Run the focused tests and confirm failure**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter "FullyQualifiedName~RabbitMqConfigurationTests|FullyQualifiedName~StartupConfigurationValidatorTests"`

Expected: FAIL because URI configuration is unsupported.

- [ ] **Step 3: Implement the builder and validation rules**

Prefer the single URI when supplied. Keep the current four split settings when no URI exists in local development and tests. Do not log the URI, username or password. Production requires the TLS URI mode.

- [ ] **Step 4: Verify focused and complete backend behavior**

Run the focused tests, Release build, complete integration suite, and local E2E journey. Confirm Docker Compose needs no setting change.

- [ ] **Step 5: Re-run the CloudAMQP probe through application composition**

Start the API with the hosted URI and an isolated staging database. Confirm the consumer connects, a submitted event enters a durable queue, survives an API recycle, and is consumed after restart. Record `HOST-10` evidence.

- [ ] **Step 6: Commit**

Commit: `feat: support tls rabbitmq configuration`

---

### Task 4: Produce Windows/IIS deployment artifacts

**Files:**
- Create: `.github/workflows/hosting-probe.yml`
- Create: `tests/Pursuit.IntegrationTests/Deployment/HostingProbeWorkflowTests.cs`
- Modify: `docs/deployment/hosting-compatibility-runbook.md`

**Interfaces:**
- Produces: `pursuit-api-win-x64` framework-dependent publish artifact and `pursuit-migration-bundle-win-x64`; accepts hosted secrets only in manually dispatched probe jobs.

- [ ] **Step 1: Write a failing workflow contract test**

Require `workflow_dispatch`, pinned setup actions, backend restore, `dotnet publish --no-self-contained`, a `win-x64` migration bundle, 7-day artifact retention, no pull-request secret access, and no automatic production deployment.

- [ ] **Step 2: Confirm the workflow contract test fails**

Run the focused test and verify it fails because `.github/workflows/hosting-probe.yml` is absent.

- [ ] **Step 3: Add artifact-only jobs**

Build the Windows migration bundle and framework-dependent IIS publish output from the exact commit. Do not run hosted migrations or deployment by default. Put secret-using jobs behind explicit `workflow_dispatch` inputs and GitHub environment approval.

- [ ] **Step 4: Verify artifacts locally**

Build both artifacts on Windows. Confirm the API artifact contains `Pursuit.API.dll`, generated `web.config`, dependency assemblies and no `appsettings.Production.json`, `.env`, publish profile or secret value.

- [ ] **Step 5: Verify the workflow and hosted artifact build**

Run the workflow contract test and `git diff --check`, push the probe branch, manually dispatch artifact build only, and record the exact run URL and artifact hashes.

- [ ] **Step 6: Commit**

Commit: `ci: build windows hosting probe artifacts`

---

### Task 5: Deploy the private MonsterASP.NET staging candidate

**Files:**
- Create: `src/Pursuit.Infrastructure/Storage/ProbeFileStorageService.cs`
- Create: `tests/Pursuit.IntegrationTests/Storage/ProbeFileStorageServiceTests.cs`
- Modify: `src/Pursuit.Infrastructure/DependencyInjection.cs`
- Modify: `src/Pursuit.API/Configuration/StartupConfigurationValidator.cs`
- Modify: `tests/Pursuit.IntegrationTests/Configuration/StartupConfigurationValidatorTests.cs`
- Modify: `docs/deployment/hosting-compatibility-runbook.md`

**Interfaces:**
- Consumes: `HostingProbe:Enabled=true` and existing absolute `DataProtection:KeyRingPath`; `HostingProbe:PrivateStoragePath` is an existing absolute directory.
- Produces: a staging-only `IBlobStorageService` that stores synthetic probe uploads outside the web root under opaque identifiers. Resume download remains outside this private probe and is implemented by the packaged demo-storage plan after host acceptance.

- [ ] **Step 1: Write failing storage-boundary tests**

Test absolute existing-directory validation, generated opaque identifiers, containment against traversal, 5 MB enforcement, allowed PDF/DOCX content types, write completion, and deletion during test cleanup. Require `GetDownloadUrlAsync` to return a stable unavailable error in probe mode. Test that production cannot enable probe storage unless `HostingProbe:Enabled=true`.

- [ ] **Step 2: Confirm focused tests fail**

Run the storage and startup-validator tests. Expected: FAIL because probe storage is absent.

- [ ] **Step 3: Implement staging-only private file storage**

Register it only for the compatibility branch setting. Keep Azure Blob as the default. Never expose a filesystem path, static-file route or directory listing. The hosted gate uploads the synthetic test resume but does not request a download. Mark the adapter for removal or replacement by packaged demo storage in the next plan.

- [ ] **Step 4: Publish and migrate staging in controlled order**

Create the provider directories first, configure Data Protection and private probe storage, run the Windows migration bundle, then Web Deploy the reviewed API artifact. Remove administrator bootstrap credentials after the first successful start.

- [ ] **Step 5: Verify IIS and SQL gates**

Exercise liveness, registration, login, refresh rotation, rotated-token replay, logout and administrator bootstrap. Recycle the application pool and redeploy the same artifact; confirm old auth cookies remain decryptable and Data Protection keys persist. Record `HOST-01`, `HOST-03`, `HOST-05` and `HOST-06`.

- [ ] **Step 6: Verify memory**

Measure worker-process memory at startup, after the complete role journey and after recycle. `HOST-02` passes only when the observed peak is below 220 MB. A peak at or above 220 MB rejects the hybrid host.

- [ ] **Step 7: Commit**

Commit: `test: add private iis storage probe`

---

### Task 6: Prove the Vercel proxy security boundary

**Files:**
- Create: `client/vercel.json`
- Create: `client/playwright.hosted.config.ts`
- Create: `client/tests/hosted-proxy.spec.ts`
- Create: `client/src/config/env.test.ts`
- Modify: `client/src/config/env.ts`
- Modify: `.github/workflows/hosting-probe.yml`
- Modify: `docs/deployment/hosting-compatibility-runbook.md`

**Interfaces:**
- Consumes: build-time `VITE_API_BASE_URL` defaulting to an empty string, fixed `HOSTED_BASE_URL`, and a fixed external API destination supplied to the staging project.
- Produces: a static SPA with same-origin `/api/*` requests and hosted Playwright tests that start no local web servers.

- [ ] **Step 1: Write failing API-base tests**

Test that an absent production value resolves to `""`, an explicit local value remains unchanged, and the browser API path becomes `/api/...` rather than `undefined/api/...`.

- [ ] **Step 2: Write the hosted proxy test cases**

Use unique synthetic accounts. Assert multiple auth cookies exist with expected Secure, HttpOnly, SameSite and Path attributes; refresh rotates cookies; logout clears them; missing or foreign Origin plus missing CSRF returns 403; multipart application submission reaches the API; upstream 401/403 status and response headers survive the rewrite; API responses have no Vercel cache hit.

- [ ] **Step 3: Confirm client tests fail for the missing defaults and hosted config**

Run `npm test -- --run src/config/env.test.ts` and validate the hosted Playwright config loads. Expected: FAIL before implementation.

- [ ] **Step 4: Add relative API configuration and Vercel routes**

Put the external `/api/:path*` rewrite before the SPA fallback. Do not enable external rewrite caching. Give hashed assets immutable caching and force `index.html` revalidation. Disable Git-triggered production deployment so the probe uses an explicit staging deployment.

- [ ] **Step 5: Deploy the fixed staging project**

Build the reviewed commit, deploy to one stable Vercel staging project, and add only that exact HTTPS origin to backend origin configuration. Keep the MonsterASP.NET URL available for direct health checks.

- [ ] **Step 6: Run proxy tests on Chromium and WebKit**

Run the hosted suite against the fixed staging URL with one worker. Inspect response headers and browser cookie storage. Record `HOST-07` and `HOST-08`; either failure rejects the hybrid host.

- [ ] **Step 7: Commit**

Commit: `test: verify hosted proxy boundary`

---

### Task 7: Run the complete hosted journey and issue the gate decision

**Files:**
- Modify: `client/playwright.hosted.config.ts`
- Modify: `client/tests/core-user-journey.spec.ts`
- Modify: `client/tests/accessibility.spec.ts`
- Modify: `.github/workflows/hosting-probe.yml`
- Modify: `docs/deployment/hosting-compatibility-results.md`
- Modify: `docs/superpowers/specs/2026-10-09-pursuit-zero-cost-public-demo-hosting-design.md`

**Interfaces:**
- Consumes: fixed Vercel staging URL, private MonsterASP.NET staging API, synthetic probe identities and the exact deployed commit SHA.
- Produces: one evidence-backed `Accept hybrid`, `Probe single-origin fallback`, or `Reject free hosting tranche` decision.

- [ ] **Step 1: Make existing journeys target-selectable**

Keep local Playwright behavior unchanged. Let hosted configuration reuse the core role journey without local `webServer` processes and with unique test data. Skip administrator UI in public hosted runs; verify administrator bootstrap through direct private staging checks only.

- [ ] **Step 2: Add dirty-state and timeout coverage**

Test a pre-existing email, duplicate application, changed job status, cold backend response and dependency timeout. Each case must end with a bounded visible result and no indefinite spinner.

- [ ] **Step 3: Run the full gate from the reviewed commit**

Run backend Release build and tests, frontend lint/tests/build, local Playwright matrix, Windows bundle idempotence, hosted Chromium/WebKit journeys, accessibility checks and `git diff --check`. Capture the exact commit SHA, GitHub run URLs, Vercel deployment URL, backend revision and sanitized provider evidence. Record the complete Employer to Job Seeker to Employer result as `HOST-11` and the cold-start result as `HOST-12`.

- [ ] **Step 4: Recheck failure thresholds**

Review every `HOST-01` through `HOST-12` result twice against the spec. Required gates must have direct evidence. Documentation claims or provider marketing do not count as a pass.

- [ ] **Step 5: Record one decision**

If every required gate passes, record `Accept hybrid` and identify the probe-only storage adapter that the demo-storage plan must replace. If proxy or cookie gates fail, deploy the compiled client from MonsterASP.NET and repeat the security and browser gates under one origin. If the fallback or another required gate fails, record `Reject free hosting tranche` and stop without merging provider-specific work.

- [ ] **Step 6: Amend the design sequencing**

Document the verified provider behavior, measured memory, any single-identity database exception, and the selected route. Remove assumptions disproved by the gate. Do not weaken the original security or data-handling requirements to manufacture a pass.

- [ ] **Step 7: Commit and request review**

Commit: `docs: record hosting compatibility decision`. Review the complete branch diff and evidence before deciding whether any commit is eligible to merge. Do not push to `main`, publish a public URL or start the demo-mode implementation without explicit approval of the gate result.

---

## Follow-on plans after acceptance

Create these only after `Accept hybrid` or a passing single-origin fallback:

1. Demo identity, deterministic seed, registration lockout and packaged synthetic resume.
2. Readiness, revision metadata, rate limits, startup UI and shared-state handling.
3. Maintenance executable, nightly reset, pre-migration export and recovery rehearsal.
4. Controlled backend migration/deploy, staged Vercel release, public smoke checks and documentation publication.
