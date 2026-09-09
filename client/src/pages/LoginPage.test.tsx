import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { AuthProvider } from '../context/AuthContext'
import LoginPage from './LoginPage'

function renderLoginPage() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/jobs" element={<div>Jobs Page Stub</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  )
}

function mockFetchSequence(handlers: Array<(url: string, init?: RequestInit) => Response | Promise<Response>>) {
  let call = 0
  vi.stubGlobal('fetch', vi.fn((url: string, init?: RequestInit) => {
    const handler = handlers[call]
    call += 1
    if (!handler) throw new Error(`Unexpected extra fetch call: ${url}`)
    return Promise.resolve(handler(url, init))
  }))
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('LoginPage', () => {
  it('logs in successfully and navigates to /jobs', async () => {
    mockFetchSequence([
      // mount-time getMe() inside AuthProvider — not authenticated yet
      () => jsonResponse({ message: 'Not authenticated' }, 401),
      // POST /api/auth/login — body unused by login(), only .ok is checked
      () => jsonResponse({}, 200),
      // post-login getMe() — returns the real user
      () => jsonResponse({ id: '1', email: 'user@test.com', role: 'JobSeeker', tenantId: null }, 200),
    ])

    renderLoginPage()
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText('Email'), 'user@test.com')
    await user.type(screen.getByLabelText('Password'), 'password123')
    await user.click(screen.getByRole('button', { name: /log in/i }))

    expect(await screen.findByText('Jobs Page Stub')).toBeInTheDocument()
  })

  it('shows an error message and does not navigate on invalid credentials', async () => {
    mockFetchSequence([
      () => jsonResponse({ message: 'Not authenticated' }, 401),
      () => jsonResponse({ message: 'Invalid email or password' }, 401),
    ])

    renderLoginPage()
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText('Email'), 'user@test.com')
    await user.type(screen.getByLabelText('Password'), 'wrongpass')
    await user.click(screen.getByRole('button', { name: /log in/i }))

    expect(await screen.findByText('Invalid email or password')).toBeInTheDocument()
    expect(screen.queryByText('Jobs Page Stub')).not.toBeInTheDocument()
  })
})