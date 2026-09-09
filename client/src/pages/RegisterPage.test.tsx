import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { AuthProvider } from '../context/AuthContext'
import RegisterPage from './RegisterPage'

function renderRegisterPage() {
  return render(
    <MemoryRouter initialEntries={['/register']}>
      <AuthProvider>
        <Routes>
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/jobs" element={<div>Jobs Page Stub</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  )
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

async function fillCommonFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(await screen.findByLabelText('First Name'), 'Ada')
  await user.type(screen.getByLabelText('Last Name'), 'Lovelace')
  await user.type(screen.getByLabelText('Email'), 'ada@test.com')
  await user.type(screen.getByLabelText('Password'), 'password123')
}

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('RegisterPage', () => {
  it('defaults to Job Seeker and does not show the Company Name field', async () => {
    renderRegisterPage()

    expect(await screen.findByLabelText('I am a')).toHaveValue('JobSeeker')
    expect(screen.queryByLabelText('Company Name')).not.toBeInTheDocument()
  })

  it('reveals the Company Name field when Employer is selected', async () => {
    renderRegisterPage()
    const user = userEvent.setup()

    await user.selectOptions(await screen.findByLabelText('I am a'), 'Employer')

    expect(screen.getByLabelText('Company Name')).toBeInTheDocument()
  })

  it('sends tenantName when registering as Employer and navigates on success', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ message: 'Not authenticated' }, 401)) // mount getMe
      .mockResolvedValueOnce(jsonResponse({}, 200)) // POST /register
      .mockResolvedValueOnce(jsonResponse({ id: '1', email: 'ada@test.com', role: 'Employer', tenantId: 'tenant-1' }, 200)) // getMe
    vi.stubGlobal('fetch', fetchMock)

    renderRegisterPage()
    const user = userEvent.setup()

    await fillCommonFields(user)
    await user.selectOptions(screen.getByLabelText('I am a'), 'Employer')
    await user.type(screen.getByLabelText('Company Name'), 'Acme Corp')
    await user.click(screen.getByRole('button', { name: /register/i }))

    expect(await screen.findByText('Jobs Page Stub')).toBeInTheDocument()

    const registerCall = fetchMock.mock.calls[1]
    const sentBody = JSON.parse(registerCall[1].body)
    expect(sentBody.tenantName).toBe('Acme Corp')
    expect(sentBody.role).toBe('Employer')
  })

  it('omits tenantName when registering as Job Seeker', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ message: 'Not authenticated' }, 401))
      .mockResolvedValueOnce(jsonResponse({}, 200))
      .mockResolvedValueOnce(jsonResponse({ id: '2', email: 'ada@test.com', role: 'JobSeeker', tenantId: null }, 200))
    vi.stubGlobal('fetch', fetchMock)

    renderRegisterPage()
    const user = userEvent.setup()

    await fillCommonFields(user)
    await user.click(screen.getByRole('button', { name: /register/i }))

    await screen.findByText('Jobs Page Stub')

    const registerCall = fetchMock.mock.calls[1]
    const sentBody = JSON.parse(registerCall[1].body)
    expect(sentBody.tenantName).toBeUndefined()
  })

  it('shows an error message on registration failure', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ message: 'Not authenticated' }, 401))
      .mockResolvedValueOnce(jsonResponse({ message: 'Email already registered' }, 400))
    vi.stubGlobal('fetch', fetchMock)

    renderRegisterPage()
    const user = userEvent.setup()

    await fillCommonFields(user)
    await user.click(screen.getByRole('button', { name: /register/i }))

    expect(await screen.findByText('Email already registered')).toBeInTheDocument()
    expect(screen.queryByText('Jobs Page Stub')).not.toBeInTheDocument()
  })
})