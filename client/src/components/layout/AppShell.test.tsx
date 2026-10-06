import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AuthContext } from '../../context/auth-context'
import { AppShell } from './AppShell'

function renderShell(role: 'Employer' | 'JobSeeker' | 'Admin' = 'Employer') {
  const logout = vi.fn().mockResolvedValue(undefined)
  render(<AuthContext.Provider value={{ user: { email: 'user@test.com', role, tenantId: role === 'Employer' ? 'tenant-1' : null }, isLoading: false, login: vi.fn(), register: vi.fn(), logout }}><MemoryRouter initialEntries={['/start']}><AppShell><Routes><Route path="/start" element={<div>Start</div>} /><Route path="/employer/jobs" element={<div>Employer home</div>} /><Route path="/jobs" element={<div>Jobs home</div>} /><Route path="/admin" element={<div>Admin home</div>} /></Routes></AppShell></MemoryRouter></AuthContext.Provider>)
}

describe('AppShell', () => {
  it('renders the skip target and employer navigation', async () => {
    renderShell('Employer'); expect(screen.getByRole('link', { name: /skip to main/i })).toHaveAttribute('href', '#main-content'); expect(screen.getAllByRole('link', { name: 'My jobs' }).length).toBeGreaterThan(0); await waitFor(() => expect(document.getElementById('main-content')).toHaveFocus())
  })
  it('renders role-specific destinations', () => {
    renderShell('JobSeeker'); expect(screen.getAllByRole('link', { name: 'My applications' }).length).toBeGreaterThan(0)
  })
  it('closes mobile navigation with Escape and restores trigger focus', async () => {
    renderShell(); const user = userEvent.setup(); const trigger = screen.getByRole('button', { name: /open navigation/i }); await user.click(trigger); expect(screen.getByRole('dialog')).toBeInTheDocument(); await user.keyboard('{Escape}'); await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument()); expect(trigger).toHaveFocus()
  })
})
