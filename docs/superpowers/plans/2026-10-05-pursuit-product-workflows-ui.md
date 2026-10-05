# Pursuit Product Workflows and UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a Glassdoor-inspired but visually independent Pursuit experience that proves the complete employer-to-job-seeker hiring workflow across accessible desktop and mobile layouts.

**Architecture:** Extend the existing modular monolith with bounded, tenant-aware paginated query contracts, then build feature-focused React pages on TanStack Query and nested URL-backed routes. Shared UI primitives and one application shell provide the Career Desk visual system; desktop and mobile reuse data/content components while layouts recompose responsively.

**Tech Stack:** ASP.NET Core 10, C# 14, EF Core, SQL Server, React 19, TypeScript 6, React Router 7, TanStack Query 5, Tailwind CSS 4, Radix Dialog/AlertDialog, Lucide React, Vitest, Testing Library, Playwright, Axe.

**Spec:** `docs/superpowers/specs/2026-10-05-pursuit-ui-product-workflows-design.md`

## Global Constraints

- Keep the application a modular monolith; do not add a new global client-state library.
- Preserve cookie authentication, CSRF behavior, tenant/applicant query filters, GUID identifiers, explicit enums, and the existing API error envelope.
- `page` must be at least 1; `pageSize` must be 1–50; default page size is 10; invalid values return HTTP 400.
- Stable collection ordering is `CreatedAt` descending, then `Id` descending.
- Use `#17212B` ink, `#A33A2B` action, `#842E23` hover, `#F6F1E8` canvas, `#FFFCF7` surface, `#5D625F` muted text, and `#D7CEC1` border.
- Replace Inter with self-hosted Manrope Variable; do not load third-party fonts at runtime.
- Do not add reviews, ratings, saved jobs, AI matching, analytics, status audit claims, screenshots, mockups, seed data, or public deployment work.
- Do not expose hard deletion as the normal job-management workflow; close/reopen through `isActive`.
- Meet WCAG 2.2 AA for the supported flows; status and selection never rely on color alone.
- Do not call the phase complete until local verification, push, hosted CI, full desktop browser coverage, and mobile Chromium coverage pass.

## Review Focus

- Invalid or excessive pagination input returns a safe 400 response instead of causing negative skips or unbounded queries (Tasks 1–3).
- A direct application-detail URL cannot reveal another tenant's candidate or resume data (Task 3).
- A failed close/reopen or status mutation leaves the last server-confirmed state visible and offers recovery (Tasks 6–8).
- A direct job or application deep link remains useful when the selected record is not present on the currently loaded list page (Tasks 5 and 7).
- Mobile navigation, filters, and confirmations keep keyboard focus contained and restore focus after closing (Tasks 4, 9, and 10).

---

### Task 1: Pagination Policy and Stable Public Job Search

**Files:**
- Create: `src/Pursuit.Application/Common/PaginationPolicy.cs`
- Modify: `src/Pursuit.Application/Services/JobService.cs`
- Modify: `src/Pursuit.Infrastructure/Persistence/Repositories/JobRepository.cs`
- Test: `tests/Pursuit.IntegrationTests/Jobs/JobSearchPaginationTests.cs`

**Interfaces:**
- Produces: `PaginationPolicy.EnsureValid(int page, int pageSize): void`, with public constants `DefaultPageSize = 10` and `MaxPageSize = 50`.
- Preserves: `IJobService.SearchAsync(...) -> Task<PagedResult<JobDto>>` and the public `/api/jobs` response shape.

- [ ] **Step 1: Write failing public-search integration tests**

Add tests asserting `page=0`, `pageSize=0`, and `pageSize=51` return 400; two jobs with equal `CreatedAt` are ordered by `CreatedAt DESC, Id DESC`; page 2 does not repeat a page-1 item; and closed jobs are excluded from public search/count while remaining retrievable by direct ID.

- [ ] **Step 2: Run the focused tests and confirm failure**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter FullyQualifiedName~JobSearchPaginationTests`

Expected: FAIL because invalid values are accepted and repository ordering is currently oldest-first without an ID tie-breaker.

- [ ] **Step 3: Implement the shared policy and stable ordering**

Call `PaginationPolicy.EnsureValid` from `JobService.SearchAsync`. Filter public search and count to `IsActive`, then apply `OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id)` before `Skip`/`Take`. Do not add the active filter to direct `GetByIdAsync`.

- [ ] **Step 4: Re-run focused tests**

Run the Step 2 command.

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add src/Pursuit.Application/Common/PaginationPolicy.cs src/Pursuit.Application/Services/JobService.cs src/Pursuit.Infrastructure/Persistence/Repositories/JobRepository.cs tests/Pursuit.IntegrationTests/Jobs/JobSearchPaginationTests.cs
git commit -m "feat(api): bound and stabilize job pagination"
```

### Task 2: Paginated Employer Jobs API

**Files:**
- Modify: `src/Pursuit.Application/Interfaces/IJobRepository.cs`
- Modify: `src/Pursuit.Application/Interfaces/IJobService.cs`
- Modify: `src/Pursuit.Application/Services/JobService.cs`
- Modify: `src/Pursuit.Infrastructure/Persistence/Repositories/JobRepository.cs`
- Modify: `src/Pursuit.API/Controllers/JobsController.cs`
- Test: `tests/Pursuit.IntegrationTests/Jobs/EmployerJobsTests.cs`

**Interfaces:**
- Produces: `IJobRepository.GetByTenantAsync(Guid tenantId, bool? isActive, int page, int pageSize, CancellationToken) -> Task<IReadOnlyList<Job>>`.
- Produces: `IJobRepository.CountByTenantAsync(Guid tenantId, bool? isActive, CancellationToken) -> Task<int>`.
- Produces: `IJobService.GetByTenantAsync(bool? isActive, int page, int pageSize, CancellationToken) -> Task<PagedResult<JobDto>>`.
- Produces: employer-only `GET /api/jobs/mine?isActive={bool?}&page={int}&pageSize={int}`.

- [ ] **Step 1: Write failing employer-job integration tests**

Cover tenant isolation, Open/Closed filtering, newest-first stable paging, anonymous 401, job-seeker 403, and invalid pagination 400. Assert the response is `PagedResult<JobDto>` with correct `totalCount`, `page`, and `pageSize`.

- [ ] **Step 2: Run the focused tests and confirm failure**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter FullyQualifiedName~EmployerJobsTests`

Expected: FAIL with 404 for `/api/jobs/mine`.

- [ ] **Step 3: Implement repository, service, and controller contracts**

Filter by the current tenant and optional `isActive`, apply the Task 1 ordering, validate pagination in the service, and map the result through the existing `JobDto` mapper. Declare the literal `mine` route before the GUID route for clarity.

- [ ] **Step 4: Re-run focused tests**

Run the Step 2 command.

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add src/Pursuit.Application/Interfaces/IJobRepository.cs src/Pursuit.Application/Interfaces/IJobService.cs src/Pursuit.Application/Services/JobService.cs src/Pursuit.Infrastructure/Persistence/Repositories/JobRepository.cs src/Pursuit.API/Controllers/JobsController.cs tests/Pursuit.IntegrationTests/Jobs/EmployerJobsTests.cs
git commit -m "feat(api): expose paginated employer jobs"
```

### Task 3: Paginated Application Queries and Direct Employer Detail

**Files:**
- Modify: `src/Pursuit.Application/DTOs/ApplicationDto.cs`
- Modify: `src/Pursuit.Application/Interfaces/IApplicationRepository.cs`
- Modify: `src/Pursuit.Application/Interfaces/IApplicationService.cs`
- Modify: `src/Pursuit.Application/Services/ApplicationService.cs`
- Modify: `src/Pursuit.Infrastructure/Persistence/Repositories/ApplicationRepository.cs`
- Modify: `src/Pursuit.API/Controllers/ApplicationsController.cs`
- Modify: `tests/Pursuit.IntegrationTests/TenantIsolation/ApplicationTenantIsolationTests.cs`
- Modify: `tests/Pursuit.IntegrationTests/TenantIsolation/ApplicationJobseekerIsolationTests.cs`

**Interfaces:**
- Produces: `ApplicationDto.CompanyName: string`.
- Removes: serialized `ApplicationDto.ResumeUrl`.
- Produces: paged repository methods `GetByJobAsync(Guid, int, int, CancellationToken)` / `CountByJobAsync(Guid, CancellationToken)` and `GetByApplicantAsync(Guid, int, int, CancellationToken)` / `CountByApplicantAsync(Guid, CancellationToken)`.
- Produces: `IApplicationService.GetMyApplicationsAsync(int page, int pageSize, CancellationToken) -> Task<PagedResult<ApplicationDto>>`.
- Produces: `IApplicationService.GetByJobAsync(Guid jobId, int page, int pageSize, CancellationToken) -> Task<PagedResult<ApplicationDto>>`.
- Produces: `IApplicationService.GetResumeStorageUrlAsync(Guid applicationId, CancellationToken) -> Task<string>` for controller-to-storage use only.
- Produces: employer-only `GET /api/applications/{id}` using the existing `GetByIdAsync` service method.

- [ ] **Step 1: Update integration tests to the paged contract and add failing cases**

Assert both list endpoints return paged envelopes, include `companyName`, omit `resumeUrl`, use stable newest-first ordering, reject invalid bounds, and retain applicant/tenant isolation. Add a direct-detail test proving the owner succeeds and a different tenant receives the established non-disclosing 404 response. Resolve `IApplicationService` from the test scope and verify `GetResumeStorageUrlAsync` returns the seeded private storage URL; Task 10 verifies the full temporary-download endpoint against Azurite.

- [ ] **Step 2: Run the tenant-isolation tests and confirm failure**

Run: `dotnet test tests/Pursuit.IntegrationTests/Pursuit.IntegrationTests.csproj --filter "FullyQualifiedName~ApplicationTenantIsolationTests|FullyQualifiedName~ApplicationJobseekerIsolationTests"`

Expected: FAIL because endpoints return arrays, omit company name, and lack direct detail.

- [ ] **Step 3: Implement paged queries, company mapping, and detail endpoint**

Include `Application.Job.Tenant` in mapped queries, apply `CreatedAt DESC, Id DESC`, validate through `PaginationPolicy`, and retain global query filters. Remove `ResumeUrl` from the serialized DTO, expose it only through `GetResumeStorageUrlAsync`, and make the controller use that internal value to generate the authorized temporary download URL.

- [ ] **Step 4: Re-run focused tests**

Run the Step 2 command.

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add src/Pursuit.Application/DTOs/ApplicationDto.cs src/Pursuit.Application/Interfaces/IApplicationRepository.cs src/Pursuit.Application/Interfaces/IApplicationService.cs src/Pursuit.Application/Services/ApplicationService.cs src/Pursuit.Infrastructure/Persistence/Repositories/ApplicationRepository.cs src/Pursuit.API/Controllers/ApplicationsController.cs tests/Pursuit.IntegrationTests/TenantIsolation/ApplicationTenantIsolationTests.cs tests/Pursuit.IntegrationTests/TenantIsolation/ApplicationJobseekerIsolationTests.cs
git commit -m "feat(api): paginate hiring application queries"
```

### Task 4: Career Desk Tokens and Accessible UI Primitives

**Files:**
- Modify: `client/package.json`
- Modify: `client/package-lock.json`
- Modify: `client/src/main.tsx`
- Modify: `client/src/index.css`
- Modify: `client/src/components/ui/Button.tsx`
- Modify: `client/src/components/ui/Input.tsx`
- Modify: `client/src/components/ui/Card.tsx`
- Modify: `client/src/components/ui/Button.test.tsx`
- Modify: `client/src/components/ui/Input.test.tsx`
- Modify: `client/src/components/ui/Card.test.tsx`
- Create: `client/src/components/ui/Alert.tsx`
- Create: `client/src/components/ui/EmptyState.tsx`
- Create: `client/src/components/ui/Skeleton.tsx`
- Create: `client/src/components/ui/StatusBadge.tsx`
- Create: `client/src/components/ui/Pagination.tsx`
- Create: `client/src/components/ui/ConfirmationDialog.tsx`
- Test: `client/src/components/ui/FeedbackPrimitives.test.tsx`
- Test: `client/src/components/ui/ConfirmationDialog.test.tsx`

**Interfaces:**
- Produces: `Alert({ variant: 'info'|'success'|'warning'|'danger', title?: string, children })`.
- Produces: `StatusBadge({ status: string })` with text plus status token.
- Produces: `Pagination({ page: number, totalPages: number, onPageChange(page): void })`.
- Produces: `ConfirmationDialog({ trigger, title, description, confirmLabel, variant, pending, onConfirm })`.
- Produces: layout-preserving `Skeleton` and action-oriented `EmptyState`.

- [ ] **Step 1: Write failing primitive tests**

Test accessible labels/roles, disabled pagination edges, live-region alerts, textual statuses, Escape close, focus containment, confirm pending state, and focus restoration to the trigger.

- [ ] **Step 2: Run focused tests and confirm failure**

Run: `npm test -- src/components/ui/FeedbackPrimitives.test.tsx src/components/ui/ConfirmationDialog.test.tsx`

Expected: FAIL because the components do not exist.

- [ ] **Step 3: Install and implement the approved design foundation**

Replace `@fontsource/inter` with `@fontsource-variable/manrope`; add `@radix-ui/react-dialog`, `@radix-ui/react-alert-dialog`, and `lucide-react`. Define the exact spec tokens, focus rings, reduced-motion rules, type scale, spacing, and base canvas. Keep primitives presentation-only and composable.

- [ ] **Step 4: Run primitive and existing component tests**

Run: `npm test -- src/components/ui`

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add client/package.json client/package-lock.json client/src/main.tsx client/src/index.css client/src/components/ui
git commit -m "feat(ui): establish Career Desk design system"
```

### Task 5: Public Job Explorer and Responsive Job Detail

**Files:**
- Modify: `client/src/api/jobs.ts`
- Modify: `client/src/types/job.ts`
- Modify: `client/src/App.tsx`
- Modify: `client/src/pages/JobsPage.tsx`
- Modify: `client/src/pages/JobDetailPage.tsx`
- Modify: `client/src/components/JobCard.tsx`
- Create: `client/src/components/jobs/JobSearchBar.tsx`
- Create: `client/src/components/jobs/JobFilters.tsx`
- Create: `client/src/components/jobs/JobResultsList.tsx`
- Create: `client/src/components/jobs/JobResultItem.tsx`
- Create: `client/src/components/jobs/JobDetailContent.tsx`
- Modify: `client/src/pages/JobsPage.test.tsx`
- Modify: `client/src/pages/JobDetailPage.test.tsx`
- Modify: `client/src/components/JobCard.test.tsx`

**Interfaces:**
- Consumes: Task 4 feedback, pagination, and dialog primitives.
- Produces: URL-backed `keyword`, `location`, `jobType`, `page`, and selected `id` behavior.
- Produces: shared `JobDetailContent({ job, applicationState })` used in desktop pane and focused mobile detail.

- [ ] **Step 1: Write failing explorer tests**

Test filter chips and removal, Back/Forward-safe query updates, selected result `aria-current`, direct `/jobs/:id` detail when the job is absent from the current result page, closed-job messaging, skeleton/error/empty states, login prompt, and successful application path to My Applications.

- [ ] **Step 2: Run focused tests and confirm failure**

Run: `npm test -- src/pages/JobsPage.test.tsx src/pages/JobDetailPage.test.tsx src/components/JobCard.test.tsx`

Expected: FAIL against the current card-only pages.

- [ ] **Step 3: Implement the nested responsive explorer**

Use semantic links for results, preserve query parameters in navigation, render a `minmax(340px, 400px)` result column at `lg`, hide it on mobile detail routes, and use route scroll restoration for returning to mobile results. Keep the detail action sticky only at `lg` and above without nested scroll trapping.

- [ ] **Step 4: Re-run focused tests**

Run the Step 2 command.

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add client/src/api/jobs.ts client/src/types/job.ts client/src/App.tsx client/src/pages/JobsPage.tsx client/src/pages/JobDetailPage.tsx client/src/components/JobCard.tsx client/src/components/jobs client/src/pages/JobsPage.test.tsx client/src/pages/JobDetailPage.test.tsx client/src/components/JobCard.test.tsx
git commit -m "feat(ui): build responsive job explorer"
```

### Task 6: Employer My Jobs and Shared Job Editor

**Files:**
- Modify: `client/src/api/jobs.ts`
- Modify: `client/src/types/job.ts`
- Modify: `client/src/App.tsx`
- Create: `client/src/components/jobs/JobForm.tsx`
- Create: `client/src/pages/EmployerJobsPage.tsx`
- Create: `client/src/pages/JobEditorPage.tsx`
- Delete: `client/src/pages/CreateJobPage.tsx`
- Create: `client/src/pages/EmployerJobsPage.test.tsx`
- Replace: `client/src/pages/CreateJobPage.test.tsx` with `client/src/pages/JobEditorPage.test.tsx`

**Interfaces:**
- Consumes: `GET /api/jobs/mine` from Task 2 and Task 4 confirmation/pagination primitives.
- Produces: `fetchMyJobs({ isActive, page, pageSize }): Promise<PagedResult<Job>>`.
- Produces: `updateJob(id: string, request: UpdateJobRequest): Promise<Job>` where `UpdateJobRequest = CreateJobRequest & { isActive: boolean }`.
- Produces: shared create/edit `JobForm` and routes `/employer/jobs`, `/employer/jobs/new`, `/employer/jobs/:id/edit`.

- [ ] **Step 1: Write failing employer-page tests**

Cover Open/Closed API filtering, paging, empty/error/retry states, create/edit prefill and submission, close/reopen confirmation, mutation pending state, successful query refresh, and failed mutation retaining the confirmed row state.

- [ ] **Step 2: Run focused tests and confirm failure**

Run: `npm test -- src/pages/EmployerJobsPage.test.tsx src/pages/JobEditorPage.test.tsx`

Expected: FAIL because pages and client methods do not exist.

- [ ] **Step 3: Implement employer jobs and editor**

Use full update payloads required by the existing PUT contract. Never issue per-job application-count requests. After create, navigate to `/employer/jobs`; after edit, stay on the editor with success feedback; after close/reopen, invalidate employer and public job queries.

- [ ] **Step 4: Re-run focused tests**

Run the Step 2 command.

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add client/src/api/jobs.ts client/src/types/job.ts client/src/App.tsx client/src/components/jobs/JobForm.tsx client/src/pages/EmployerJobsPage.tsx client/src/pages/JobEditorPage.tsx client/src/pages/EmployerJobsPage.test.tsx client/src/pages/JobEditorPage.test.tsx client/src/pages/CreateJobPage.tsx client/src/pages/CreateJobPage.test.tsx
git commit -m "feat(ui): add employer job management"
```

### Task 7: Employer Application Review and Resume Access

**Files:**
- Modify: `client/src/api/applications.ts`
- Modify: `client/src/types/application.ts`
- Modify: `client/src/App.tsx`
- Create: `client/src/components/applications/ApplicationList.tsx`
- Create: `client/src/components/applications/ApplicationDetail.tsx`
- Create: `client/src/pages/ApplicationReviewPage.tsx`
- Test: `client/src/pages/ApplicationReviewPage.test.tsx`

**Interfaces:**
- Consumes: Task 3 paged/detail endpoints and Task 4 primitives.
- Produces: `fetchApplicationsByJob(jobId, page, pageSize): Promise<PagedResult<ApplicationDto>>`.
- Produces: `fetchApplication(id): Promise<ApplicationDto>`.
- Produces: `updateApplicationStatus(id, status): Promise<ApplicationDto>`.
- Produces: `fetchResumeDownloadUrl(id): Promise<{ downloadUrl: string }>`.

- [ ] **Step 1: Write failing application-review tests**

Cover paged candidate loading, direct application deep link not on the current list page, selected-row state, empty/error/retry states, resume request pending/failure/success, explicit status confirmation, successful invalidation, and failed status mutation retaining the prior status.

- [ ] **Step 2: Run the focused test and confirm failure**

Run: `npm test -- src/pages/ApplicationReviewPage.test.tsx`

Expected: FAIL because the feature does not exist.

- [ ] **Step 3: Implement list/detail review routes**

Add `/employer/jobs/:jobId/applications` and `/:applicationId`. Use the detail endpoint for direct links, never render `resumeUrl`, and create a temporary anchor from the authorized `downloadUrl` with `target="_blank"` and `rel="noopener noreferrer"` after a successful user-triggered request.

- [ ] **Step 4: Re-run the focused test**

Run the Step 2 command.

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add client/src/api/applications.ts client/src/types/application.ts client/src/App.tsx client/src/components/applications client/src/pages/ApplicationReviewPage.tsx client/src/pages/ApplicationReviewPage.test.tsx
git commit -m "feat(ui): add employer application review"
```

### Task 8: Job-Seeker Application History

**Files:**
- Modify: `client/src/api/applications.ts`
- Modify: `client/src/App.tsx`
- Create: `client/src/pages/MyApplicationsPage.tsx`
- Test: `client/src/pages/MyApplicationsPage.test.tsx`

**Interfaces:**
- Consumes: Task 3 `PagedResult<ApplicationDto>` including `companyName`, and Task 4 status/pagination primitives.
- Produces: `fetchMyApplications(page, pageSize): Promise<PagedResult<ApplicationDto>>` and protected `/applications` route.

- [ ] **Step 1: Write failing history tests**

Assert company, job, submitted date, and textual status render; paging calls the correct query; the empty state links to Find Jobs; failures offer Retry; and status data refreshed after an employer update is visible after query refetch.

- [ ] **Step 2: Run the focused test and confirm failure**

Run: `npm test -- src/pages/MyApplicationsPage.test.tsx`

Expected: FAIL because the page does not exist.

- [ ] **Step 3: Implement My Applications**

Use chronological compact rows/cards, link available jobs to `/jobs/:jobId`, and keep all status copy grounded in the existing enum without response-time or audit claims.

- [ ] **Step 4: Re-run the focused test**

Run the Step 2 command.

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add client/src/api/applications.ts client/src/App.tsx client/src/pages/MyApplicationsPage.tsx client/src/pages/MyApplicationsPage.test.tsx
git commit -m "feat(ui): add job seeker application history"
```

### Task 9: Role-Aware Application Shell and Remaining Page Redesign

**Files:**
- Modify: `client/src/App.tsx`
- Modify: `client/src/components/Navbar.tsx`
- Modify: `client/src/types/auth.ts`
- Create: `client/src/components/layout/AppShell.tsx`
- Create: `client/src/components/layout/RoleNavigation.tsx`
- Create: `client/src/components/layout/MobileNavigation.tsx`
- Create: `client/src/routes/roleHomePath.ts`
- Modify: `client/src/context/ProtectedRoute.tsx`
- Modify: `client/src/pages/HomePage.tsx`
- Modify: `client/src/pages/LoginPage.tsx`
- Modify: `client/src/pages/RegisterPage.tsx`
- Modify: `client/src/pages/AdminPage.tsx`
- Create: `client/src/components/layout/AppShell.test.tsx`
- Modify: `client/src/pages/HomePage.test.tsx`
- Modify: `client/src/pages/LoginPage.test.tsx`
- Modify: `client/src/pages/RegisterPage.test.tsx`
- Modify: `client/src/pages/AdminPage.test.tsx`

**Interfaces:**
- Consumes: Tasks 4–8 routes and components.
- Produces: `CurrentUser.role: 'Employer' | 'JobSeeker' | 'Admin'` and `roleHomePath(role: CurrentUser['role']): '/employer/jobs' | '/jobs' | '/admin'`.
- Produces: shared shell with skip link, `<main id="main-content" tabIndex={-1}>`, desktop role navigation, and Radix mobile navigation dialog.

- [ ] **Step 1: Write failing shell and page tests**

Test role-specific links, role landing paths, unauthorized redirects, skip-link target, route-change main focus, mobile dialog Escape/focus restoration, logout failure feedback, admin mobile labeled cards, and the home/auth pages' approved Career Desk copy and states.

- [ ] **Step 2: Run focused tests and confirm failure**

Run: `npm test -- src/components/layout/AppShell.test.tsx src/pages/HomePage.test.tsx src/pages/LoginPage.test.tsx src/pages/RegisterPage.test.tsx src/pages/AdminPage.test.tsx`

Expected: FAIL against the current single-row navbar and generic page layouts.

- [ ] **Step 3: Implement the shell and complete the visual pass**

Keep search primary on public/job-seeker surfaces, send Employer to My Jobs, Job Seeker to Find Jobs, and Admin to `/admin`. Render administrator statistics as a compact summary strip rather than a generic card grid. On mobile, render accounts as labeled cards instead of a horizontally scrolling table.

- [ ] **Step 4: Run the complete component suite**

Run: `npm test`

Expected: PASS.

- [ ] **Step 5: Commit**

```text
git add client/src/App.tsx client/src/components/Navbar.tsx client/src/components/layout client/src/routes client/src/context/ProtectedRoute.tsx client/src/types/auth.ts client/src/pages/HomePage.tsx client/src/pages/LoginPage.tsx client/src/pages/RegisterPage.tsx client/src/pages/AdminPage.tsx client/src/pages/HomePage.test.tsx client/src/pages/LoginPage.test.tsx client/src/pages/RegisterPage.test.tsx client/src/pages/AdminPage.test.tsx
git commit -m "feat(ui): add role-aware Career Desk shell"
```

### Task 10: Accessibility and Complete Browser Workflow

**Files:**
- Modify: `client/package.json`
- Modify: `client/package-lock.json`
- Modify: `client/playwright.config.ts`
- Replace: `client/tests/core-user-journey.spec.ts`
- Create: `client/tests/accessibility.spec.ts`
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: all prior routes and API contracts.
- Produces: desktop Chromium/Firefox/WebKit and mobile Chromium projects.
- Produces: Axe smoke checks for public, employer, seeker, and administrator representative screens.

- [ ] **Step 1: Extend the failing E2E journey and accessibility checks**

Make the journey prove employer registration, creation, My Jobs visibility, seeker registration/search/application, employer candidate review, authorized resume request response, status update, seeker-visible status, and administrator account inspection/update. Add mobile assertions for menu operation, filter dialog, focused detail route, no horizontal overflow, and visible focus.

- [ ] **Step 2: Run browser tests and confirm the new assertions fail before final wiring**

Run: `npm run e2e:infra:up`, then `npx playwright test --project=chromium`.

Expected: FAIL until admin bootstrap test configuration, new routes, resume action, status propagation, and accessibility expectations are fully wired.

- [ ] **Step 3: Configure deterministic E2E identities and accessibility tooling**

Add `@axe-core/playwright`. Configure the E2E API web server with opt-in administrator bootstrap credentials supplied only through E2E environment configuration. Add `mobile-chromium` using a current Pixel device profile. Keep all four projects in hosted CI.

- [ ] **Step 4: Run the full browser matrix and shut down dependencies**

Run: `npx playwright test`; finally run `npm run e2e:infra:down` even if tests fail.

Expected: PASS in Chromium, Firefox, WebKit, and mobile Chromium with no serious or critical Axe violations on scoped pages.

- [ ] **Step 5: Commit**

```text
git add client/package.json client/package-lock.json client/playwright.config.ts client/tests/core-user-journey.spec.ts client/tests/accessibility.spec.ts .github/workflows/ci.yml
git commit -m "test(ui): prove complete hiring workflow"
```

### Task 11: Release Verification and Evidence Gate

**Files:**
- Modify only if verification reveals a defect in an already-owned file; do not add screenshots or demo assets.

**Interfaces:**
- Consumes: the complete implementation.
- Produces: verified local release evidence, a clean pushed branch, and a successful hosted CI run.

- [ ] **Step 1: Run backend release verification**

Run:

```text
dotnet restore Pursuit.slnx --force-evaluate
dotnet list Pursuit.slnx package --vulnerable --include-transitive
dotnet build Pursuit.slnx --configuration Release --no-restore
dotnet test Pursuit.slnx --configuration Release --no-build
```

Expected: restore succeeds, no vulnerable packages are reported, build has zero warnings, and all integration tests pass.

- [ ] **Step 2: Run frontend release verification**

Run from `client`:

```text
npm run lint
npm test
npm run build
npm audit --audit-level=high
```

Expected: lint, component tests, TypeScript/Vite build, and audit pass.

- [ ] **Step 3: Run the full E2E matrix and manual browser review**

Start E2E dependencies, run all Playwright projects, and inspect 1440×900, 1024×768, 390×844, and 360×800 layouts. Verify keyboard traversal, focus visibility/restoration, reduced motion, 200% text scaling, dialogs, sticky actions, overflow, and all loading/empty/error/success states.

Expected: the complete workflow passes and no acceptance issue remains. If Docker is unavailable, record an environment block and do not declare the phase complete.

- [ ] **Step 4: Review the final diff against the spec twice**

First pass: production readiness, tenant boundaries, pagination, mutation behavior, failure recovery, and truthful claims. Second pass: Glassdoor-inspired structure without copying, responsive recomposition, accessibility, maintainability, and interview value. Confirm no screenshot, mockup, seed-data, or deployment material was added.

- [ ] **Step 5: Commit any verification-only fixes, push, and check hosted CI**

Use a narrowly scoped commit only if Step 1–4 required corrections. Push `main`, verify the exact SHA's hosted CI run and all browser jobs, and finish with a clean `git status` synchronized with `origin/main`.
