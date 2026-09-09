import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AdminPage from './AdminPage'

function createTestQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } })
}

function renderAdminPage() {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      <AdminPage />
    </QueryClientProvider>
  )
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function noContentResponse() {
  return new Response(null, { status: 204 })
}

function mockFetchRoutes(routes: Record<string, Array<() => Response>>) {
  const queues = new Map(Object.entries(routes).map(([key, responses]) => [key, [...responses]]))
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init?: RequestInit) => {
      const method = (init?.method ?? 'GET').toUpperCase()
      const matchKey = Object.keys(routes).find((key) => {
        const [routeMethod, routePath] = key.split(' ')
        return routeMethod === method && url.includes(routePath)
      })
      if (!matchKey) throw new Error(`No mock route for ${method} ${url}`)
      const queue = queues.get(matchKey)!
      const next = queue.length > 1 ? queue.shift()! : queue[0]
      return Promise.resolve(next())
    })
  )
}

const statsBody = {
  totalUsers: 42,
  totalEmployers: 10,
  totalJobSeekers: 32,
  totalJobs: 15,
  totalApplications: 60,
}

function makeUser(overrides: Partial<{
  id: string; firstName: string; lastName: string; email: string
  role: string; isActive: boolean; createdAt: string
}> = {}) {
  return {
    id: 'user-1',
    firstName: 'Ada',
    lastName: 'Lovelace',
    email: 'ada@test.com',
    role: 'JobSeeker',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('AdminPage', () => {
  it('renders stats and the user table', async () => {
    mockFetchRoutes({
      'GET /api/admin/stats': [() => jsonResponse(statsBody)],
      'GET /api/admin/users': [() => jsonResponse({ items: [makeUser()], totalCount: 1, page: 1, pageSize: 10 })],
    })

    renderAdminPage()

    expect(await screen.findByText('42')).toBeInTheDocument()
    expect(screen.getByText('Total Users')).toBeInTheDocument()
    expect(await screen.findByText('Ada Lovelace')).toBeInTheDocument()
    expect(screen.getByText('Active')).toBeInTheDocument()
  })

  it('deactivates an active user and reflects the updated status after refetch', async () => {
    mockFetchRoutes({
      'GET /api/admin/stats': [() => jsonResponse(statsBody)],
      'GET /api/admin/users': [
        () => jsonResponse({ items: [makeUser({ isActive: true })], totalCount: 1, page: 1, pageSize: 10 }),
        () => jsonResponse({ items: [makeUser({ isActive: false })], totalCount: 1, page: 1, pageSize: 10 }),
      ],
      'PATCH /api/admin/users': [noContentResponse],
    })

    renderAdminPage()
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Deactivate' }))

    expect(await screen.findByRole('button', { name: 'Activate' })).toBeInTheDocument()
    expect(screen.getByText('Inactive')).toBeInTheDocument()
  })

  it('shows an error message when the status update fails, without changing the row', async () => {
    mockFetchRoutes({
      'GET /api/admin/stats': [() => jsonResponse(statsBody)],
      'GET /api/admin/users': [() => jsonResponse({ items: [makeUser({ isActive: true })], totalCount: 1, page: 1, pageSize: 10 })],
      'PATCH /api/admin/users': [() => jsonResponse({ message: 'Cannot deactivate yourself' }, 400)],
    })

    renderAdminPage()
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Deactivate' }))

    expect(await screen.findByText('Cannot deactivate yourself')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Deactivate' })).toBeInTheDocument()
  })

  it('disables Previous on the first page and Next on the last page', async () => {
    mockFetchRoutes({
      'GET /api/admin/stats': [() => jsonResponse(statsBody)],
      'GET /api/admin/users': [() => jsonResponse({ items: [makeUser()], totalCount: 1, page: 1, pageSize: 10 })],
    })

    renderAdminPage()

    expect(await screen.findByRole('button', { name: 'Previous' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled()
  })
})