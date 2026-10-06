import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MyApplicationsPage from './MyApplicationsPage'

const api = vi.hoisted(() => ({ fetchMyApplications: vi.fn() }))
vi.mock('../api/applications', () => api)
const item = { id: 'app-1', jobId: 'job-1', jobTitle: 'Backend Engineer', companyName: 'Acme', applicantId: 'user-1', applicantName: 'Asha', status: 'Applied', createdAt: '2026-10-01T00:00:00Z' }
function renderPage() { const client = new QueryClient({ defaultOptions: { queries: { retry: false } } }); return render(<QueryClientProvider client={client}><MemoryRouter><MyApplicationsPage /></MemoryRouter></QueryClientProvider>) }
beforeEach(() => { vi.clearAllMocks(); api.fetchMyApplications.mockResolvedValue({ items: [item], totalCount: 11, page: 1, pageSize: 10 }) })

describe('MyApplicationsPage', () => {
  it('renders company, job, date, status, and paging', async () => {
    renderPage(); expect(await screen.findByText('Backend Engineer')).toBeInTheDocument(); expect(screen.getByText('Acme')).toBeInTheDocument(); expect(screen.getByText('Applied')).toBeInTheDocument(); expect(screen.getByText(/Oct 1, 2026/i)).toBeInTheDocument()
    api.fetchMyApplications.mockResolvedValueOnce({ items: [], totalCount: 11, page: 2, pageSize: 10 }); await userEvent.click(screen.getByRole('button', { name: /next page/i })); await waitFor(() => expect(api.fetchMyApplications).toHaveBeenCalledWith(2, 10))
  })
  it('links an empty history back to Find jobs', async () => {
    api.fetchMyApplications.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 10 }); renderPage(); expect(await screen.findByText(/no applications yet/i)).toBeInTheDocument(); expect(screen.getByRole('link', { name: /find jobs/i })).toHaveAttribute('href', '/jobs')
  })
  it('offers retry and shows refreshed employer status', async () => {
    api.fetchMyApplications.mockRejectedValueOnce(new Error('Load failed')).mockResolvedValueOnce({ items: [{ ...item, status: 'Reviewed' }], totalCount: 1, page: 1, pageSize: 10 }); renderPage(); expect(await screen.findByText('Load failed')).toBeInTheDocument(); await userEvent.click(screen.getByRole('button', { name: /try again/i })); expect(await screen.findByText('Reviewed')).toBeInTheDocument()
  })
})
