import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import CreateJobPage from './CreateJobPage'

function renderCreateJobPage() {
  return render(
    <MemoryRouter initialEntries={['/jobs/new']}>
      <Routes>
        <Route path="/jobs/new" element={<CreateJobPage />} />
        <Route path="/jobs/:id" element={<div>Job Detail Stub</div>} />
      </Routes>
    </MemoryRouter>
  )
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

async function fillForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Title'), 'Backend Engineer')
  await user.type(screen.getByLabelText('Description'), 'Build things.')
  await user.type(screen.getByLabelText('Location'), 'Remote')
  await user.type(screen.getByLabelText('Salary Min'), '50000')
  await user.type(screen.getByLabelText('Salary Max'), '80000')
}

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('CreateJobPage', () => {
  it('creates a job and navigates to its detail page', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(jsonResponse({ id: 'job-999' }, 200)))

    renderCreateJobPage()
    const user = userEvent.setup()

    await fillForm(user)
    await user.click(screen.getByRole('button', { name: /post job/i }))

    expect(await screen.findByText('Job Detail Stub')).toBeInTheDocument()
  })

  it('shows an error message and does not navigate when creation fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(jsonResponse({ message: 'Title is required' }, 400)))

    renderCreateJobPage()
    const user = userEvent.setup()

    await fillForm(user)
    await user.click(screen.getByRole('button', { name: /post job/i }))

    expect(await screen.findByText('Title is required')).toBeInTheDocument()
    expect(screen.queryByText('Job Detail Stub')).not.toBeInTheDocument()
  })
})