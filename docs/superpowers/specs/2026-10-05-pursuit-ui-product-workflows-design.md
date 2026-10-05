# Pursuit Product Workflows and UI Redesign

**Date:** 5 October 2026
**Status:** Design approved in conversation; implementation not started

## 1. Purpose

This phase turns Pursuit's existing technical capabilities into a credible, complete hiring product experience. It must let an employer publish and manage jobs, let a job seeker discover and apply for work, let the employer review and progress the application, let the job seeker see the updated outcome, and retain the existing administrator account controls.

The redesign uses Glassdoor's job-search interaction model as a reference for information density, persistent search, list-and-detail navigation, and separation of candidate and employer areas. It does not copy Glassdoor's branding, visual styling, proprietary content, reviews, ratings, community features, or unsupported product capabilities.

This phase does not add repository screenshots, mockups, public UI images, synthetic demonstration data, or a public deployment. Those remain gated behind the completed workflow, accessibility, responsiveness, browser, and visual-quality reviews.

## 2. Verified Starting Point

The current client exposes:

- Home
- Job search
- Job details and application submission
- Employer job creation
- Login and registration
- Administrator statistics and account activation controls

The backend already exposes job update, job deletion, application history, applications by job, application status update, and resume-download URL operations. `IJobService.GetByTenantAsync` exists, but no HTTP endpoint currently exposes an employer's own jobs. The current browser journey stops after the job seeker submits an application.

Application queries are protected by the existing tenant/applicant query filters. Status updates currently accept any defined status without transition rules, audit history, or optimistic concurrency. The UI must not imply those controls exist.

## 3. Product Principles

1. **Search first.** Job discovery remains immediately available instead of being hidden behind dashboard navigation.
2. **Dense but calm.** Users should scan jobs, candidates, and statuses quickly without oversized cards or decorative dashboard furniture.
3. **Role clarity.** Employer, job-seeker, administrator, and public navigation expose only relevant destinations and actions.
4. **Truthful product copy.** The interface does not claim ratings, matching intelligence, verified employers, audited status history, delivery guarantees, or other unproved behavior.
5. **Reversible lifecycle actions.** Closing and reopening use `isActive`. Hard deletion is not presented as the normal job-management workflow.
6. **Accessible by construction.** Semantic elements, keyboard operation, focus handling, contrast, reduced motion, and status text are design requirements, not final polish.
7. **Responsive recomposition.** Mobile screens prioritize one task at a time rather than stacking a compressed desktop workspace.

## 4. Information Architecture

### 4.1 Public

- `/` — Home
- `/jobs` — Search and results
- `/jobs/:id` — Selected job details
- `/login` — Login
- `/register` — Role-aware registration

### 4.2 Employer

- `/employer/jobs` — Employer's jobs
- `/employer/jobs/new` — Create a job
- `/employer/jobs/:id/edit` — Edit a job
- `/employer/jobs/:jobId/applications` — Applications for a job
- `/employer/jobs/:jobId/applications/:applicationId` — Selected application review

An employer lands on `/employer/jobs` after registration or login.

### 4.3 Job seeker

- `/jobs` — Search and results
- `/jobs/:id` — Job details and application action
- `/applications` — Current user's application history

A job seeker lands on `/jobs` after registration or login. My Applications remains a primary navigation destination.

### 4.4 Administrator

- `/admin` — Platform overview and account management

An administrator lands on `/admin` after login.

## 5. Application Shell and Navigation

One shared application shell provides:

- A skip link to the main content.
- Pursuit identity linked to the role-appropriate landing page.
- Persistent job search access for public users and job seekers.
- Role-specific primary navigation.
- Account identity and logout.
- A semantic `main` region with a stable focus target after route changes.

Desktop uses top navigation because each role has only a small number of destinations. Mobile uses an accessible menu dialog/disclosure with the same destinations, account identity, and logout action. It must support Escape, focus containment where applicable, focus restoration, `aria-expanded`, and visible focus.

Navigation by role:

| Role | Primary destinations |
| --- | --- |
| Public | Find Jobs, Login, Register |
| Job seeker | Find Jobs, My Applications |
| Employer | My Jobs, Post a Job |
| Administrator | Overview, Accounts (sections within `/admin`) |

## 6. Visual Direction: Career Desk

Pursuit's identity is warm, direct, and information-led.

### 6.1 Typography

- Replace Inter with a self-hosted Manrope Variable font.
- Use strong, compact job and page titles.
- Use smaller, quieter supporting metadata with comfortable line height.
- Use tabular numerals for salary, dates, and counts where supported.
- Preserve a readable measure for job descriptions and form guidance.

### 6.2 Color

The initial token set is:

| Purpose | Value |
| --- | --- |
| Primary ink/navigation | `#17212B` |
| Primary action | `#A33A2B` |
| Primary action hover | `#842E23` |
| Canvas | `#F6F1E8` |
| Surface | `#FFFCF7` |
| Muted text | `#5D625F` |
| Border | `#D7CEC1` |
| Success text/background | `#176348` / `#E5F4EC` |
| Warning text/background | `#7A4B00` / `#FFF1CF` |
| Danger text/background | `#9D2F26` / `#FBE8E5` |
| Informational text/background | `#285C86` / `#E6F0F8` |

The primary action has a 6.57:1 contrast ratio against white. Primary ink and muted text have 14.48:1 and 5.53:1 contrast against the canvas. Each status text/background pair exceeds 6:1. Color never carries status or selection alone.

### 6.3 Shape, spacing, and motion

- Use modest corner radii rather than pill-shaped or heavily rounded containers.
- Prefer borders, spacing, and selected-edge treatments over shadows.
- Use a consistent 4px-based spacing scale.
- Use restrained transitions for hover, selection, disclosure, and status feedback.
- Disable nonessential motion under `prefers-reduced-motion`.
- Do not use gradients, glass effects, oversized decorative initials, or generic dashboard statistic-card grids as the visual identity.

## 7. Job Discovery Experience

### 7.1 Search and filters

Search is visible on the jobs experience and remains close to results. Keyword and location are primary fields. Job type and any additional supported filters appear as compact controls below or beside search. Active filters are visible and individually removable. Search state remains URL-backed so searches are shareable and browser Back/Forward works.

### 7.2 Desktop list-and-detail workspace

The desktop jobs experience contains:

- A compact search row.
- A filter row.
- A results column sized with `minmax(340px, 400px)`.
- A flexible selected-job detail region.

Each result displays only verified job data: title, company, location, job type, salary range, active state where relevant, and posting date. The selected result has an edge/border treatment and `aria-current`; it does not rely only on background color.

`/jobs` shows search and results. `/jobs/:id` keeps the result context visible on desktop while showing the selected job. Direct visits to `/jobs/:id` remain supported.

### 7.3 Mobile

Mobile shows search and results as one screen. Selecting a job navigates to a focused `/jobs/:id` detail screen. Filters open in an accessible dialog with explicit Apply and Clear actions. Search parameters remain in the return URL, and route scroll restoration returns the user to the previous result position.

### 7.4 Job details and applying

Job details lead with company, title, location, type, salary, and active/closed state, followed by the description. On screens at least 1024px wide, the application action uses a sticky region below the application header; on smaller screens it remains in normal document flow. The sticky region must not cover content or create a nested scrolling trap.

Only job seekers see the resume submission control. Logged-out users receive a clear login action. Employers do not see an application form. Closed jobs clearly state that applications are no longer accepted.

After successful submission, the user sees confirmation and a path to My Applications. Duplicate or invalid submissions show the server's safe user-facing message.

## 8. Employer Experience

### 8.1 My Jobs

My Jobs contains Open and Closed views and a clear Post a Job action. Rows show title, location, type, posting date, and active state. Available actions are Edit, View Applications, and Close or Reopen.

The UI must not compute application counts by issuing one request per job. Counts remain absent until an aggregate API supports them.

Closing or reopening requires explicit confirmation, does not use optimistic UI, and refreshes affected job queries after success. Hard deletion is not exposed as a primary workflow.

### 8.2 Create and edit

Create and edit use one shared `JobEditor` form with sections for:

- Role title and description
- Location and job type
- Minimum and maximum salary
- Publication state when editing

Client validation mirrors clear constraints for immediate feedback, while server validation remains authoritative. Server validation errors must be associated with the relevant field when possible and otherwise shown in a form-level alert.

### 8.3 Application review

Desktop application review uses a candidate list and selected-candidate detail. Mobile uses separate list and focused detail screens.

Candidate entries show applicant name, submission date, and current status. The detail shows the related job, applicant identity already supplied by the API, submission date, resume action, and status control.

Resume download first requests the authorized, time-limited URL and then starts navigation/download. Pending and failure states remain visible. The raw stored resume URL is never shown.

Status changes use an explicit selection and confirmation action. They are not optimistic. The UI supports only the existing values: Applied, Reviewed, Rejected, and Hired. It does not claim that transitions are validated or audited.

## 9. Job-Seeker Application History

My Applications presents compact, chronological entries containing company, job title, submission date, and current status. Entries link back to the relevant job when it remains available.

The page provides:

- A skeleton while loading.
- A useful empty state with a Find Jobs action.
- A retry action for recoverable failures.
- Textual status labels with accessible color treatment.
- Pagination rather than an unbounded result set.

The page does not claim email delivery, employer response time, or audit history.

## 10. Administrator Experience

The administrator area retains existing statistics and account activation controls but adopts the new shell and visual system. Desktop uses a compact, accessible table. Mobile converts each record into a labeled card rather than requiring horizontal scrolling.

Activation/deactivation actions show pending state, require confirmation where the effect is disruptive, and report failures without mutating visible status. Existing prohibition against changing the current administrator's own status remains server-authoritative.

## 11. Frontend Architecture

The implementation introduces feature-focused components rather than a dashboard framework:

- `AppShell`
- `RoleNavigation`
- `MobileNavigation`
- `JobsExplorer`
- `JobResultsList`
- `JobResultItem`
- `JobDetailContent`
- `EmployerJobsPage`
- `JobEditor`
- `ApplicationReviewPage`
- `ApplicationList`
- `ApplicationDetail`
- `MyApplicationsPage`
- `StatusBadge`
- `Alert`
- `Skeleton`
- `EmptyState`
- `PageHeader`
- `ConfirmationDialog`
- `Pagination`

Desktop and mobile reuse the same data and content components. Responsive layouts may change visibility and placement, but business behavior must not be implemented twice.

TanStack Query remains the server-state authority. No additional global state library is introduced. Mutations invalidate only affected query keys. Authentication remains in the existing auth context.

Use `@radix-ui/react-dialog` for mobile navigation and filters and `@radix-ui/react-alert-dialog` for confirmations, styled entirely through Pursuit's tokens. Use Lucide's tree-shakeable React icons only where icons improve scanning or comprehension.

## 12. API Changes

### 12.1 Employer jobs

Add:

`GET /api/jobs/mine?page=1&pageSize=10`

Requirements:

- Employer authorization.
- Existing tenant scope.
- Existing `PagedResult<JobDto>` response shape.
- Stable newest-first ordering.
- Return HTTP 400 when `page < 1` or `pageSize` is outside 1–50. The default page size is 10.

### 12.2 Application lists

Paginate the touched list endpoints:

- `GET /api/applications/my?page=1&pageSize=10`
- `GET /api/applications/job/{jobId}?page=1&pageSize=10`

Both return `PagedResult<ApplicationDto>`, order by `CreatedAt` descending and then `Id` descending, and return HTTP 400 when `page < 1` or `pageSize` is outside 1–50. The default page size is 10. The employer endpoint retains tenant isolation; the job-seeker endpoint retains applicant isolation.

### 12.3 Public job pagination

Apply the same server-side page and page-size bounds to public job search. Order public results by `CreatedAt` descending and then `Id` descending so paging is stable. Do not redesign search indexing, cursor pagination, or cache architecture in this phase.

### 12.4 Existing mutations

Reuse:

- `POST /api/jobs`
- `PUT /api/jobs/{id}`
- `PATCH /api/applications/{id}/status`
- `GET /api/applications/{id}/resume`

Job close/reopen uses the existing job update contract's `isActive` field. The UI does not use the delete endpoint in the normal workflow.

## 13. Loading, Empty, Error, and Success States

Every asynchronous surface defines all four state classes:

- **Loading:** layout-preserving skeletons with an accessible loading label where needed.
- **Empty:** explanation plus the most useful next action.
- **Error:** concise safe message and Retry when the operation is recoverable.
- **Success:** confirmation through visible text and a polite live region.

Mutation controls remain disabled while pending and prevent accidental duplicate submission. Status-changing operations keep the previous confirmed state until the server succeeds. Unauthorized sessions continue through the existing refresh/logout behavior. Forbidden and not-found responses use non-disclosing user-facing messages.

## 14. Accessibility Requirements

- Meet WCAG 2.2 AA for supported flows.
- Provide a skip link and semantic landmarks.
- Preserve logical heading order.
- Give every form control an explicit accessible label.
- Associate field errors with inputs using `aria-describedby` and invalid state.
- Use visible focus treatments on every interactive element.
- Support keyboard operation for menus, dialogs, result selection, forms, and pagination.
- Restore focus after dialogs and route-level actions where appropriate.
- Announce asynchronous success and failure without stealing focus.
- Use text plus visual treatment for every status.
- Avoid mobile horizontal scrolling for primary content.
- Respect reduced motion and browser text scaling.

## 15. Verification Strategy

### 15.1 Backend integration tests

Cover:

- Employer can retrieve only their own jobs.
- Job seeker and anonymous clients cannot retrieve employer jobs.
- Tenant A cannot retrieve Tenant B's jobs or applications.
- Applicant A cannot retrieve Applicant B's application history.
- Application and job pagination returns stable pages and bounded page sizes.
- Close/reopen persists through the update operation.
- Status and resume endpoints retain tenant isolation.

### 15.2 Frontend tests

Cover:

- Role navigation and post-login destinations.
- URL-backed search, filtering, paging, and selected job state.
- Desktop list/detail and mobile focused-detail presentation.
- Create/edit form validation and server errors.
- Close/reopen confirmation and query invalidation.
- Application review, resume action, and status mutation.
- Job-seeker application history.
- Loading, empty, error, retry, pending, and success states.
- Accessible names and keyboard behavior for new primitives.

### 15.3 Automated accessibility

Add Axe-based checks for representative public, employer, job-seeker, and administrator screens. Automated checks supplement rather than replace manual keyboard, focus, contrast, text-scaling, and reduced-motion review.

### 15.4 Browser workflow

Extend Playwright to prove:

1. Employer registers.
2. Employer creates a job and sees it in My Jobs.
3. Job seeker registers, finds the job, and applies.
4. Employer opens the application and requests the resume.
5. Employer changes the application status.
6. Job seeker sees the updated status in My Applications.
7. Administrator inspects accounts and changes account status.

Run the workflow in desktop Chromium, Firefox, and WebKit. Add a mobile Chromium project for the responsive journey.

### 15.5 Final visual review

Inspect representative desktop and mobile widths for overflow, clipped controls, unusable sticky regions, missing focus, poor hierarchy, and hidden actions. Do not add or publish screenshots until the user approves this completed review.

## 16. Deliberate Deferrals

This phase does not implement:

- Company reviews or ratings
- Salary estimates beyond employer-entered ranges
- Saved jobs or saved searches
- AI matching or resume scoring
- Employer analytics
- Application status-transition rules
- Status audit history
- Optimistic concurrency for hiring decisions
- Transactional outbox
- Public demonstration data or deployment
- Job archive semantics beyond close/reopen

These omissions must remain visible as roadmap items where relevant; the UI must not imply they are complete.

## 17. Decision Review

### 17.1 Production readiness and scalability

- New and touched collection endpoints are paginated and bounded.
- Tenant and applicant isolation remain server-side and receive integration coverage.
- Status-changing mutations wait for confirmed server responses.
- Resume access uses the authorized temporary-download operation.
- The design avoids per-job application-count requests.
- Known lifecycle, audit, concurrency, and event-delivery gaps remain explicit.

### 17.2 Architecture and maintainability

- Shared content components serve both desktop and mobile compositions.
- Server state stays in TanStack Query and authentication stays in the existing context.
- Routes encode durable product locations and preserve browser navigation.
- Accessible behavior uses focused primitives rather than page-specific reinvention.
- The scope avoids unrelated backend restructuring.

### 17.3 Portfolio and interview value

The completed phase will visibly demonstrate product judgment, multi-role workflow design, responsive architecture, accessible interaction, tenant-aware APIs, pagination, server-state management, integration testing, and cross-browser verification. It will not be presented as proof of the deliberately deferred production capabilities.

## 18. Reference Boundary

The Glassdoor references informed only the product interaction principles described above:

- <https://www.glassdoor.com/index.htm>
- <https://www.glassdoor.com/Job/index.htm>

Pursuit's implementation, visual tokens, typography, copy, information architecture, and feature scope remain independent.
