import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import JobDetailPage from './JobDetailPage'
import { useAuth } from '../context/AuthContext'
import { fireEvent } from '@testing-library/react'

vi.mock('../context/AuthContext', () => ({
  useAuth: vi.fn(),
}))

function createTestQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } })
}

function renderJobDetailPage(id = 'job-1') {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/jobs/${id}`]}>
        <Routes>
          <Route path="/jobs/:id" element={<JobDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

const job = {
  id: 'job-1',
  tenantId: 'tenant-1',
  title: 'Backend Engineer',
  companyName: 'Acme Corp',
  description: 'Line one.\nLine two.',
  location: 'Remote',
  salaryMin: 50000,
  salaryMax: 80000,
  jobType: 'FullTime',
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
}

function mockUser(overrides: Partial<{ id: string; email: string; role: string; tenantId: string | null }> | null) {
  vi.mocked(useAuth).mockReturnValue({
    user: overrides,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  } as ReturnType<typeof useAuth>)
}

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('JobDetailPage', () => {
  it('renders job details for any visitor', async () => {
    mockUser(null)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(jsonResponse(job)))

    renderJobDetailPage()

    expect(await screen.findByText('Backend Engineer')).toBeInTheDocument()
    expect(screen.getByText('Acme Corp · Remote')).toBeInTheDocument()
  })

  it('shows a not-found message when the job does not exist', async () => {
    mockUser(null)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(jsonResponse({}, 404)))

    renderJobDetailPage()

    expect(await screen.findByText('This job could not be found.')).toBeInTheDocument()
  })

  it('prompts login for a visitor who is not authenticated', async () => {
    mockUser(null)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(jsonResponse(job)))

    renderJobDetailPage()

    expect(await screen.findByText(/as a job seeker to apply/i)).toBeInTheDocument()
  })

  it('hides the apply form for an Employer', async () => {
    mockUser({ id: 'u1', email: 'emp@test.com', role: 'Employer', tenantId: 'tenant-1' })
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(jsonResponse(job)))

    renderJobDetailPage()

    await screen.findByText('Backend Engineer')
    expect(screen.queryByLabelText(/resume/i)).not.toBeInTheDocument()
  })

  it('rejects a disallowed file type before submission is possible', async () => {
    mockUser({ id: 'u2', email: 'js@test.com', role: 'JobSeeker', tenantId: null })
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(jsonResponse(job)))

    renderJobDetailPage()

    const fileInput = await screen.findByLabelText(/resume/i)
    const badFile = new File(['x'], 'resume.txt', { type: 'text/plain' })

    fireEvent.change(fileInput, { target: { files: [badFile] } })

    expect(await screen.findByText('Only PDF and DOCX files are allowed.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /apply/i })).toBeDisabled()
})

  it('submits a valid resume and shows the success message', async () => {
    mockUser({ id: 'u2', email: 'js@test.com', role: 'JobSeeker', tenantId: null })
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse(job))
      .mockResolvedValueOnce(jsonResponse({ id: 'app-1', jobId: 'job-1', status: 'Submitted' }))
    vi.stubGlobal('fetch', fetchMock)

    renderJobDetailPage()
    const user = userEvent.setup()

    const fileInput = await screen.findByLabelText(/resume/i)
    const goodFile = new File(['%PDF-1.4'], 'resume.pdf', { type: 'application/pdf' })
    await user.upload(fileInput, goodFile)
    await user.click(screen.getByRole('button', { name: /apply/i }))

    expect(await screen.findByText('Application submitted.')).toBeInTheDocument()
  })
})