# Portfolio roadmap

This roadmap orders work by user impact, production risk, and what the change teaches.

## Completed foundation

- Reproducible backend, frontend, and browser CI
- Tenant isolation tests for employer and job-seeker application reads
- Refresh-token rotation, replay detection, logout revocation, and concurrent-request locking
- Account and tenant revocation for access and refresh sessions
- CSRF protection for browser mutations
- Public registration role allowlist
- Production configuration checks and opt-in administrator bootstrap
- One-command Docker Compose stack for the API, client, SQL Server, Redis, RabbitMQ, and Azurite

## 1. Product experience and UI review

Complete the visible employer and job-seeker workflows, then redesign the interface as one coherent system. The design review must cover information hierarchy, navigation, empty and error states, responsive behavior, accessibility, and visual consistency.

Do not publish screenshots or promote a public demo until this phase passes a browser review at desktop and mobile sizes.

Evidence required:

- Employers can manage their jobs and review applications from the client.
- Job seekers can view their applications and current status.
- The complete workflow is covered by Playwright.
- Keyboard navigation, form labels, focus states, loading states, and error states are verified.
- Screens selected for the portfolio represent real seeded data and the final visual system.

## 2. Public demo and onboarding

Deploy a resettable demo environment with synthetic data, HTTPS, restricted demo accounts, and no real resumes. Add the live URL to the GitHub repository only after the product-experience review is complete.

Evidence required:

- A new reviewer can open the product without installing local dependencies.
- Demo data resets on a documented schedule.
- Public accounts cannot damage the shared environment or access another user's data.
- The deployed revision and health status are visible.

## 3. Transactional outbox

Write the application and an outbox record in one SQL transaction. A worker publishes pending records with broker confirms, retries, and an idempotent consumer.

Evidence required:

- Broker outage does not make a saved application look failed to the user.
- Worker restart publishes pending records.
- Duplicate delivery produces one logical notification.
- Concurrent submissions still create one application.

## 4. Deployment migration boundary

Build an EF Core migration bundle in CI, inspect migrations before release, and run the bundle as a one-time deployment job. Remove schema permissions from the normal API identity and remove `MigrateAsync` from API startup.

Evidence required:

- Empty database reaches the latest schema.
- An up-to-date database is unchanged.
- A failed migration stops deployment before API rollout.
- Rollback procedure is written and rehearsed.

## 5. Readiness and shutdown

Keep a lightweight liveness endpoint and add readiness checks for SQL Server, Redis, RabbitMQ, and Blob Storage. Fix the RabbitMQ channel shutdown path so normal host disposal does not produce a fatal log.

Evidence required:

- A failed required dependency removes the instance from readiness.
- Redis policy matches the application's cache-fallback behavior.
- Shutdown completes without an unhandled exception.

## 6. Resource controls

Set server-side limits for pages, search text, JSON bodies, multipart requests, file size, and accepted file content. Apply stricter rate limits to registration, login, refresh, and upload routes.

Evidence required:

- Boundary and over-limit tests return stable 4xx responses.
- Limits are configurable and documented.
- A small load test records latency and rejection behavior.

## 7. Query growth

Add deterministic ordering and measured indexes, paginate application lists, and replace deep offset pages with keyset pagination. Review full-text search when SQL `Contains` becomes a measured bottleneck.

Evidence required:

- Query plans use the intended indexes.
- Paging remains stable when new rows arrive.
- Before-and-after measurements use a documented data set.

## 8. Notification and audit completion

Store notification delivery status and add audit records for administrator actions. Each feature should include authorization tests and a complete browser flow.
