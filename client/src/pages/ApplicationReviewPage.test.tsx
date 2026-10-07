import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ApplicationReviewPage from './ApplicationReviewPage'

const api = vi.hoisted(() => ({
  fetchApplicationsByJob: vi.fn(), fetchApplication: vi.fn(),
  updateApplicationStatus: vi.fn(), fetchResumeDownloadUrl: vi.fn(),
}))
vi.mock('../api/applications', () => api)

const application = { id: 'app-1', jobId: 'job-1', jobTitle: 'Backend Engineer', companyName: 'Acme', applicantId: 'user-1', applicantName: 'Asha', status: 'Applied' as const, createdAt: '2026-10-01T00:00:00Z' }

function renderPage(path = '/employer/jobs/job-1/applications') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[path]}><Routes>
    <Route path="/employer/jobs/:jobId/applications" element={<ApplicationReviewPage />} />
    <Route path="/employer/jobs/:jobId/applications/:applicationId" element={<ApplicationReviewPage />} />
  </Routes></MemoryRouter></QueryClientProvider>)
}

beforeEach(() => {
  vi.clearAllMocks()
  api.fetchApplicationsByJob.mockResolvedValue({ items: [application], totalCount: 1, page: 1, pageSize: 10 })
})

describe('ApplicationReviewPage', () => {
  it('loads a paged list and marks the selected candidate', async () => {
    api.fetchApplication.mockResolvedValue(application)
    renderPage('/employer/jobs/job-1/applications/app-1')
    expect((await screen.findAllByText('Asha')).length).toBeGreaterThan(0)
    expect(screen.getByRole('link', { name: /asha/i })).toHaveAttribute('aria-current', 'true')
    expect(api.fetchApplicationsByJob).toHaveBeenCalledWith('job-1', 1, 10)
  })

  it('loads a direct candidate link independently of the current list page', async () => {
    api.fetchApplicationsByJob.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 10 })
    api.fetchApplication.mockResolvedValue({ ...application, id: 'app-99', applicantName: 'Deep Link Candidate' })
    renderPage('/employer/jobs/job-1/applications/app-99')
    expect(await screen.findByRole('heading', { name: 'Deep Link Candidate' })).toBeInTheDocument()
    expect(api.fetchApplication).toHaveBeenCalledWith('app-99')
  })

  it('resets the status selection when a different candidate is opened', async () => {
    const second = { ...application, id: 'app-2', applicantName: 'Bea Candidate' }
    api.fetchApplicationsByJob.mockResolvedValue({ items: [application, second], totalCount: 2, page: 1, pageSize: 10 })
    api.fetchApplication.mockImplementation((id: string) => Promise.resolve(id === 'app-2' ? second : application))
    renderPage('/employer/jobs/job-1/applications/app-1')

    expect((await screen.findAllByRole('heading', { name: 'Asha' })).length).toBeGreaterThan(0)
    await userEvent.selectOptions(screen.getByLabelText('Application status'), 'Reviewed')
    await userEvent.click(screen.getByRole('link', { name: /bea candidate/i }))

    expect((await screen.findAllByRole('heading', { name: 'Bea Candidate' })).length).toBeGreaterThan(0)
    expect(screen.getByLabelText('Application status')).toHaveValue('Applied')
  })

  it('shows list error, retry, and empty states', async () => {
    api.fetchApplicationsByJob.mockRejectedValueOnce(new Error('Load failed')).mockResolvedValueOnce({ items: [], totalCount: 0, page: 1, pageSize: 10 })
    renderPage()
    expect(await screen.findByText('Load failed')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /try again/i }))
    expect(await screen.findByText(/no candidates yet/i)).toBeInTheDocument()
  })

  it('requests a resume only on click and reports failure', async () => {
    api.fetchApplication.mockResolvedValue(application)
    api.fetchResumeDownloadUrl.mockRejectedValue(new Error('Resume unavailable'))
    renderPage('/employer/jobs/job-1/applications/app-1')
    expect((await screen.findAllByRole('heading', { name: 'Asha' })).length).toBeGreaterThan(0)
    expect(api.fetchResumeDownloadUrl).not.toHaveBeenCalled()
    await userEvent.click(screen.getByRole('button', { name: /open resume/i }))
    expect(await screen.findByText('Resume unavailable')).toBeInTheDocument()
  })

  it('opens the authorized resume URL in a protected new tab', async () => {
    api.fetchApplication.mockResolvedValue(application)
    api.fetchResumeDownloadUrl.mockResolvedValue({ downloadUrl: 'https://files.example/resume?token=short-lived' })
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
    renderPage('/employer/jobs/job-1/applications/app-1')
    expect((await screen.findAllByRole('heading', { name: 'Asha' })).length).toBeGreaterThan(0)
    await userEvent.click(screen.getByRole('button', { name: /open resume/i }))
    await waitFor(() => expect(click).toHaveBeenCalled())
    const anchor = click.mock.instances[0]
    expect(anchor).toHaveAttribute('target', '_blank')
    expect(anchor).toHaveAttribute('rel', 'noopener noreferrer')
  })

  it('confirms status changes, refreshes on success, and retains status on failure', async () => {
    api.fetchApplication.mockResolvedValue(application)
    api.updateApplicationStatus.mockRejectedValueOnce(new Error('Update failed')).mockResolvedValueOnce({ ...application, status: 'Reviewed' })
    renderPage('/employer/jobs/job-1/applications/app-1')
    expect((await screen.findAllByRole('heading', { name: 'Asha' })).length).toBeGreaterThan(0)
    await userEvent.selectOptions(screen.getByLabelText('Application status'), 'Reviewed')
    await userEvent.click(screen.getByRole('button', { name: 'Update status' }))
    await userEvent.click(screen.getByRole('button', { name: 'Confirm status' }))
    expect(await screen.findByText('Update failed')).toBeInTheDocument()
    expect(document.querySelector('[data-status="applied"]')).toHaveTextContent('Applied')

    await userEvent.click(screen.getByRole('button', { name: 'Update status' }))
    await userEvent.click(screen.getByRole('button', { name: 'Confirm status' }))
    await waitFor(() => expect(api.fetchApplication).toHaveBeenCalledTimes(2))
  })
})
