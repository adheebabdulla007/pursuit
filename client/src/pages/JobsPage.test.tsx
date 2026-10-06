import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route, useNavigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import JobsPage from './JobsPage'
import { AuthContext } from '../context/auth-context'

function createTestQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } })
}

function BackButton() {
  const navigate = useNavigate()
  return <button onClick={() => navigate(-1)}>Browser back</button>
}

function renderJobsPage(initialPath = '/jobs', entries = [initialPath], initialIndex = entries.length - 1) {
  const queryClient = createTestQueryClient()
  return render(
    <AuthContext.Provider value={{
      user: null,
      isLoading: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    }}>
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={entries} initialIndex={initialIndex}>
          <BackButton />
          <Routes>
            <Route path="/jobs" element={<JobsPage />} />
            <Route path="/jobs/:id" element={<JobsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </AuthContext.Provider>
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
    await user.click(screen.getByRole('button', { name: /search jobs/i }))

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

  it('renders active filter chips and removes one without discarding the others', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 1, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()

    renderJobsPage('/jobs?keyword=dotnet&location=Riyadh&jobType=Remote')
    await screen.findByText('Backend Engineer')
    await user.click(screen.getByRole('button', { name: 'Remove Keyword filter' }))

    const lastUrl = fetchMock.mock.calls.at(-1)?.[0] as string
    expect(lastUrl).not.toContain('keyword=')
    expect(lastUrl).toContain('location=Riyadh')
    expect(lastUrl).toContain('jobType=Remote')
  })

  it('opens filters as a keyboard-dismissable mobile dialog', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 1, page: 1, pageSize: 10 })
    ))
    const user = userEvent.setup()
    renderJobsPage()
    await screen.findByText('Backend Engineer')

    const trigger = screen.getByRole('button', { name: 'Filters' })
    await user.click(trigger)
    const dialog = screen.getByRole('dialog', { name: 'Filter jobs' })
    expect(within(dialog).getByLabelText('Job Type')).toBeInTheDocument()
    await user.keyboard('{Escape}')
    expect(screen.queryByRole('dialog', { name: 'Filter jobs' })).not.toBeInTheDocument()
    expect(trigger).toHaveFocus()
  })

  it('syncs form values when browser history changes the URL', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 1, page: 1, pageSize: 10 })
    ))
    const user = userEvent.setup()

    renderJobsPage('/jobs?keyword=second', ['/jobs?keyword=first', '/jobs?keyword=second'], 1)
    expect(await screen.findByLabelText('Keyword')).toHaveValue('second')
    await user.click(screen.getByRole('button', { name: 'Browser back' }))
    expect(await screen.findByLabelText('Keyword')).toHaveValue('first')
  })

  it('marks the selected result and fetches direct detail even when absent from the result page', async () => {
    const fetchMock = vi.fn().mockImplementation((input: string) => {
      if (input.endsWith('/api/jobs/job-1')) {
        return Promise.resolve(jsonResponse(makeJob({ id: 'job-1', title: 'Direct Detail Role' })))
      }
      return Promise.resolve(jsonResponse({
        items: [makeJob({ id: 'job-2', title: 'Result Page Role' })],
        totalCount: 1,
        page: 1,
        pageSize: 10,
      }))
    })
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage('/jobs/job-1?keyword=engineer')

    expect(await screen.findByRole('heading', { name: 'Direct Detail Role' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Result Page Role/ })).not.toHaveAttribute('aria-current')
    expect(fetchMock.mock.calls.some(([url]) => String(url).endsWith('/api/jobs/job-1'))).toBe(true)
  })

  it('renders skeleton, error, and empty states with explicit semantics', async () => {
    let resolveFetch!: (value: Response) => void
    const pending = new Promise<Response>((resolve) => { resolveFetch = resolve })
    const fetchMock = vi.fn().mockReturnValueOnce(pending)
    vi.stubGlobal('fetch', fetchMock)
    const view = renderJobsPage()
    expect(screen.getByTestId('job-list-skeleton')).toBeInTheDocument()

    resolveFetch(jsonResponse({ items: [], totalCount: 0, page: 1, pageSize: 10 }))
    expect(await screen.findByRole('heading', { name: 'No jobs match this search' })).toBeInTheDocument()

    view.unmount()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse({}, 500)))
    renderJobsPage()
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load jobs')
  })

  it('disables Previous on page 1 and enables Next when more pages exist', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 25, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage()

    expect(await screen.findByRole('button', { name: 'Previous page' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Page 1' })).toHaveAttribute('aria-current', 'page')
  })

  it('clicking Next requests page 2', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({ items: [makeJob()], totalCount: 25, page: 1, pageSize: 10 })
    )
    vi.stubGlobal('fetch', fetchMock)

    renderJobsPage()
    const user = userEvent.setup()

    await screen.findByRole('button', { name: 'Page 1' })
    await user.click(screen.getByRole('button', { name: 'Next page' }))

    const lastUrl = fetchMock.mock.calls.at(-1)?.[0] as string
    expect(lastUrl).toContain('page=2')
  })
})
