import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route, useLocation } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import HomePage from './HomePage'

function createTestQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } })
}

function JobsRouteStub() {
  const location = useLocation()
  return <div>Jobs Page Stub: {location.search}</div>
}

function renderHomePage() {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/jobs" element={<JobsRouteStub />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function makeJob(overrides: Partial<{ id: string; title: string; createdAt: string }> = {}) {
  return {
    id: 'job-1',
    tenantId: 'tenant-1',
    title: 'Backend Engineer',
    companyName: 'Acme Corp',
    description: 'desc',
    location: 'Remote',
    salaryMin: 50000,
    salaryMax: 80000,
    jobType: 'FullTime',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function mockFetchRoutes(routes: Record<string, () => Response>) {
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string) => {
      const key = Object.keys(routes).find((k) => url.includes(k))
      if (!key) throw new Error(`No mock route for ${url}`)
      return Promise.resolve(routes[key]())
    })
  )
}

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('HomePage', () => {
  it('renders the total jobs count and the recent jobs list', async () => {
    mockFetchRoutes({
      'pageSize=1': () => jsonResponse({ items: [], totalCount: 250, page: 1, pageSize: 1 }),
      'pageSize=20': () =>
        jsonResponse({
          items: [makeJob({ id: 'job-1', title: 'Backend Engineer', createdAt: '2026-02-01T00:00:00Z' })],
          totalCount: 250,
          page: 1,
          pageSize: 20,
        }),
    })

    renderHomePage()

    expect(await screen.findByText('250 jobs currently listed')).toBeInTheDocument()
    expect(await screen.findByText('Backend Engineer')).toBeInTheDocument()
  })

  it('does not render the Recent Jobs section when there are no jobs', async () => {
    mockFetchRoutes({
      'pageSize=1': () => jsonResponse({ items: [], totalCount: 0, page: 1, pageSize: 1 }),
      'pageSize=20': () => jsonResponse({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
    })

    renderHomePage()

    await screen.findByText('0 jobs currently listed')
    expect(screen.queryByText('Recent Jobs')).not.toBeInTheDocument()
  })

  it('navigates to /jobs with keyword and location query params on search', async () => {
    mockFetchRoutes({
      'pageSize=1': () => jsonResponse({ items: [], totalCount: 0, page: 1, pageSize: 1 }),
      'pageSize=20': () => jsonResponse({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
    })

    renderHomePage()
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText('Keyword'), 'developer')
    await user.type(screen.getByLabelText('Location'), 'Riyadh')
    await user.click(screen.getByRole('button', { name: /search jobs/i }))

    const stub = await screen.findByText(/Jobs Page Stub/)
    expect(stub.textContent).toContain('keyword=developer')
    expect(stub.textContent).toContain('location=Riyadh')
  })
})