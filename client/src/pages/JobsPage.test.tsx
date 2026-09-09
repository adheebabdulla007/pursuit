import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import JobsPage from './JobsPage'

function createTestQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } })
}

function renderJobsPage(initialPath = '/jobs') {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/jobs" element={<JobsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function makeJob(overrides: Partial<{ id: string; title: string }> = {}) {
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

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('JobsPage', () => {
  it('fetches and renders jobs for the default (unfiltered) first page', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 1, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage()

    expect(await screen.findByText('Backend Engineer')).toBeInTheDocument()
    const requestedUrl = fetchMock.mock.calls[0][0] as string
    expect(requestedUrl).toContain('page=1')
    expect(requestedUrl).toContain('pageSize=10')
    expect(requestedUrl).not.toContain('keyword=')
  })

  it('submitting the filter form searches by keyword and location and resets to page 1', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 1, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage()
    const user = userEvent.setup()

    await screen.findByText('Backend Engineer')
    await user.type(screen.getByLabelText('Keyword'), 'engineer')
    await user.type(screen.getByLabelText('Location'), 'Riyadh')
    await user.click(screen.getByRole('button', { name: /^search$/i }))

    const lastUrl = fetchMock.mock.calls.at(-1)?.[0] as string
    expect(lastUrl).toContain('keyword=engineer')
    expect(lastUrl).toContain('location=Riyadh')
    expect(lastUrl).toContain('page=1')
  })

  it('changing the job type filter preserves the existing keyword filter (merge, not replace)', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 1, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage('/jobs?keyword=engineer')
    const user = userEvent.setup()

    await screen.findByText('Backend Engineer')
    await user.selectOptions(screen.getByLabelText('Job Type'), 'Remote')

    const lastUrl = fetchMock.mock.calls.at(-1)?.[0] as string
    expect(lastUrl).toContain('keyword=engineer')
    expect(lastUrl).toContain('jobType=Remote')
  })

  it('disables Previous on page 1 and enables Next when more pages exist', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 25, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage()

    expect(await screen.findByRole('button', { name: 'Previous' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled()
    expect(screen.getByText('Page 1 of 3')).toBeInTheDocument()
  })

  it('clicking Next requests page 2', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 25, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage()
    const user = userEvent.setup()

    await screen.findByText('Page 1 of 3')
    await user.click(screen.getByRole('button', { name: 'Next' }))

    const lastUrl = fetchMock.mock.calls.at(-1)?.[0] as string
    expect(lastUrl).toContain('page=2')
  })
})