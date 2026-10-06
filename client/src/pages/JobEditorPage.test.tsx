import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import JobEditorPage from './JobEditorPage'

const api = vi.hoisted(() => ({ createJob: vi.fn(), fetchJobById: vi.fn(), updateJob: vi.fn() }))
vi.mock('../api/jobs', () => api)

const job = {
  id: 'job-1', tenantId: 'tenant-1', title: 'Backend Engineer', companyName: 'Acme',
  description: 'Build reliable APIs', location: 'Kochi', salaryMin: 50000, salaryMax: 80000,
  jobType: 'FullTime' as const, isActive: true, createdAt: '2026-10-01T00:00:00Z',
}

function renderEditor(path: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/employer/jobs/new" element={<JobEditorPage />} />
          <Route path="/employer/jobs/:id/edit" element={<JobEditorPage />} />
          <Route path="/employer/jobs" element={<div>Employer Jobs Stub</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

async function fillNewForm() {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('Job title'), 'Backend Engineer')
  await user.type(screen.getByLabelText('Description'), 'Build reliable APIs')
  await user.type(screen.getByLabelText('Location'), 'Kochi')
  await user.type(screen.getByLabelText('Minimum salary'), '50000')
  await user.type(screen.getByLabelText('Maximum salary'), '80000')
  return user
}

beforeEach(() => vi.clearAllMocks())

describe('JobEditorPage', () => {
  it('creates a job and returns to My jobs', async () => {
    api.createJob.mockResolvedValue(job)
    renderEditor('/employer/jobs/new')
    const user = await fillNewForm()
    await user.click(screen.getByRole('button', { name: 'Publish job' }))
    expect(await screen.findByText('Employer Jobs Stub')).toBeInTheDocument()
    expect(api.createJob).toHaveBeenCalledWith(expect.objectContaining({ title: 'Backend Engineer', salaryMin: 50000 }))
  })

  it('prefills and updates an existing job without leaving the editor', async () => {
    api.fetchJobById.mockResolvedValue(job)
    api.updateJob.mockResolvedValue({ ...job, title: 'Senior Backend Engineer' })
    renderEditor('/employer/jobs/job-1/edit')
    expect(await screen.findByDisplayValue('Backend Engineer')).toBeInTheDocument()
    const user = userEvent.setup()
    const title = screen.getByLabelText('Job title')
    await user.clear(title)
    await user.type(title, 'Senior Backend Engineer')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('Job changes saved.')).toBeInTheDocument()
    expect(api.updateJob).toHaveBeenCalledWith('job-1', expect.objectContaining({
      title: 'Senior Backend Engineer', isActive: true,
    }))
  })

  it('keeps the form available and reports a failed save', async () => {
    api.fetchJobById.mockResolvedValue(job)
    api.updateJob.mockRejectedValue(new Error('Save failed'))
    renderEditor('/employer/jobs/job-1/edit')
    await screen.findByDisplayValue('Backend Engineer')
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }))
    expect(await screen.findByText('Save failed')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Backend Engineer')).toBeInTheDocument()
  })

  it('disables submission while a create request is pending', async () => {
    api.createJob.mockReturnValue(new Promise(() => {}))
    renderEditor('/employer/jobs/new')
    const user = await fillNewForm()
    await user.click(screen.getByRole('button', { name: 'Publish job' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Publishing…' })).toBeDisabled())
  })
})
