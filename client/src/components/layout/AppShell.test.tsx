import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AuthContext } from '../../context/auth-context'
import { AppShell } from './AppShell'

function renderShell(role: 'Employer' | 'JobSeeker' | 'Admin' | null = 'Employer', logout = vi.fn().mockResolvedValue(undefined)) {
  const user = role ? { email: 'user@test.com', role, tenantId: role === 'Employer' ? 'tenant-1' : null } : null
  render(<AuthContext.Provider value={{ user, isLoading: false, login: vi.fn(), register: vi.fn(), logout }}><MemoryRouter initialEntries={['/start']}><AppShell><Routes><Route path="/start" element={<div>Start</div>} /><Route path="/employer/jobs" element={<div>Employer home</div>} /><Route path="/jobs" element={<div>Jobs home</div>} /><Route path="/admin" element={<div>Admin home</div>} /></Routes></AppShell></MemoryRouter></AuthContext.Provider>)
}

describe('AppShell', () => {
  it('keeps the skip link first on initial load and focuses main after route navigation', async () => {
    renderShell('Employer')
    const user = userEvent.setup()
    const skipLink = screen.getByRole('link', { name: /skip to main/i })
    expect(skipLink).toHaveAttribute('href', '#main-content')
    expect(screen.getByRole('link', { name: /pursuit/i })).toHaveAttribute('href', '/employer/jobs')
    await user.tab()
    expect(skipLink).toHaveFocus()
    await user.click(screen.getAllByRole('link', { name: 'My jobs' })[0])
    await waitFor(() => expect(document.getElementById('main-content')).toHaveFocus())
  })
  it('renders role-specific destinations', () => {
    renderShell('JobSeeker'); expect(screen.getAllByRole('link', { name: 'My applications' }).length).toBeGreaterThan(0)
  })
  it('exposes login and registration to public users on desktop and mobile', async () => {
    renderShell(null)
    expect(screen.getByRole('link', { name: /pursuit/i })).toHaveAttribute('href', '/jobs')
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login')
    expect(screen.getByRole('link', { name: 'Register' })).toHaveAttribute('href', '/register')
    await userEvent.click(screen.getByRole('button', { name: /open navigation/i }))
    const dialog = screen.getByRole('dialog', { name: 'Navigation' })
    expect(dialog).toContainElement(screen.getByRole('link', { name: 'Log in' }))
    expect(dialog).toContainElement(screen.getByRole('link', { name: 'Register' }))
  })
  it('closes mobile navigation with Escape and restores trigger focus', async () => {
    renderShell(); const user = userEvent.setup(); const trigger = screen.getByRole('button', { name: /open navigation/i }); await user.click(trigger); expect(screen.getByRole('dialog')).toBeInTheDocument(); await user.keyboard('{Escape}'); await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument()); expect(trigger).toHaveFocus()
  })
  it('closes mobile navigation after logout so success or failure feedback is visible', async () => {
    const logout = vi.fn().mockRejectedValue(new Error('Session service unavailable'))
    renderShell('Employer', logout)
    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: /open navigation/i }))
    await user.click(screen.getByRole('button', { name: 'Log out' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Session service unavailable')
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })
})
