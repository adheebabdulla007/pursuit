import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import EmployerJobsPage from './EmployerJobsPage'

const api = vi.hoisted(() => ({
  fetchMyJobs: vi.fn(),
  updateJob: vi.fn(),
}))

vi.mock('../api/jobs', () => api)

const job = {
  id: 'job-1', tenantId: 'tenant-1', title: 'Platform Engineer', companyName: 'Acme',
  description: 'Build systems', location: 'Remote', salaryMin: 50000, salaryMax: 80000,
  jobType: 'FullTime' as const, isActive: true, createdAt: '2026-10-01T00:00:00Z',
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><EmployerJobsPage /></MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  api.fetchMyJobs.mockResolvedValue({ items: [job], totalCount: 1, page: 1, pageSize: 10 })
})

describe('EmployerJobsPage', () => {
  it('filters the employer list and supports paging', async () => {
    api.fetchMyJobs
      .mockResolvedValueOnce({ items: [job], totalCount: 12, page: 1, pageSize: 10 })
      .mockResolvedValueOnce({ items: [{ ...job, id: 'job-2', isActive: false }], totalCount: 1, page: 1, pageSize: 10 })

    renderPage()
    expect(await screen.findByText('Platform Engineer')).toBeInTheDocument()
    expect(api.fetchMyJobs).toHaveBeenCalledWith({ isActive: undefined, page: 1, pageSize: 10 })

    await userEvent.click(screen.getByRole('button', { name: 'Closed' }))
    await waitFor(() => expect(api.fetchMyJobs).toHaveBeenCalledWith({ isActive: false, page: 1, pageSize: 10 }))

    api.fetchMyJobs.mockResolvedValueOnce({ items: [job], totalCount: 12, page: 2, pageSize: 10 })
    await userEvent.click(screen.getByRole('button', { name: 'All' }))
    await screen.findByText('Platform Engineer')
    await userEvent.click(screen.getByRole('button', { name: /next/i }))
    await waitFor(() => expect(api.fetchMyJobs).toHaveBeenCalledWith({ isActive: undefined, page: 2, pageSize: 10 }))
  })

  it('shows empty, error, and retry states', async () => {
    api.fetchMyJobs.mockRejectedValueOnce(new Error('Network unavailable'))
    renderPage()
    expect(await screen.findByText('Network unavailable')).toBeInTheDocument()
    api.fetchMyJobs.mockResolvedValueOnce({ items: [], totalCount: 0, page: 1, pageSize: 10 })
    await userEvent.click(screen.getByRole('button', { name: /try again/i }))
    expect(await screen.findByText(/no jobs in this view/i)).toBeInTheDocument()
  })

  it('confirms a close, sends the full payload, and refreshes the list', async () => {
    let resolveUpdate!: (value: typeof job) => void
    api.updateJob.mockReturnValue(new Promise((resolve) => { resolveUpdate = resolve }))
    renderPage()
    await screen.findByText('Platform Engineer')

    await userEvent.click(screen.getByRole('button', { name: /close platform engineer/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Close job' }))
    expect(screen.getByRole('button', { name: /close platform engineer/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /close platform engineer/i })).toHaveTextContent('Working…')
    expect(api.updateJob).toHaveBeenCalledWith('job-1', {
      title: job.title, description: job.description, location: job.location,
      salaryMin: job.salaryMin, salaryMax: job.salaryMax, jobType: job.jobType, isActive: false,
    })

    resolveUpdate({ ...job, isActive: false })
    await waitFor(() => expect(api.fetchMyJobs).toHaveBeenCalledTimes(2))
  })

  it('retains the visible row state when a confirmed mutation fails', async () => {
    api.updateJob.mockRejectedValue(new Error('Could not close job'))
    renderPage()
    await screen.findByText('Platform Engineer')
    await userEvent.click(screen.getByRole('button', { name: /close platform engineer/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Close job' }))

    expect(await screen.findByText('Could not close job')).toBeInTheDocument()
    expect(document.querySelector('[data-status="open"]')).toHaveTextContent('Open')
  })

  it('returns to the last valid page when closing its only row', async () => {
    api.fetchMyJobs
      .mockResolvedValueOnce({ items: [job], totalCount: 1, page: 1, pageSize: 10 })
      .mockResolvedValueOnce({ items: [job], totalCount: 11, page: 1, pageSize: 10 })
      .mockResolvedValueOnce({ items: [{ ...job, id: 'job-11' }], totalCount: 11, page: 2, pageSize: 10 })
      .mockResolvedValueOnce({ items: [job], totalCount: 10, page: 1, pageSize: 10 })
    api.updateJob.mockResolvedValue({ ...job, id: 'job-11', isActive: false })

    renderPage()
    await screen.findByText('Platform Engineer')
    await userEvent.click(screen.getByRole('button', { name: 'Open' }))
    await waitFor(() => expect(api.fetchMyJobs).toHaveBeenLastCalledWith({ isActive: true, page: 1, pageSize: 10 }))
    await userEvent.click(screen.getByRole('button', { name: /next/i }))
    await waitFor(() => expect(api.fetchMyJobs).toHaveBeenLastCalledWith({ isActive: true, page: 2, pageSize: 10 }))
    await userEvent.click(screen.getByRole('button', { name: /close platform engineer/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Close job' }))

    await waitFor(() => expect(api.fetchMyJobs).toHaveBeenLastCalledWith({ isActive: true, page: 1, pageSize: 10 }))
    expect(await screen.findByText('Platform Engineer')).toBeInTheDocument()
  })
})
