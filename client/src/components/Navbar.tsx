import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { MobileNavigation } from './layout/MobileNavigation'
import { RoleNavigation } from './layout/RoleNavigation'
import { Button } from './ui/Button'
export default function Navbar() {
  const { user, isLoading, logout } = useAuth(); const navigate = useNavigate(); const [error, setError] = useState('')
  async function handleLogout() { setError(''); try { await logout(); navigate('/login') } catch (err) { setError(err instanceof Error ? err.message : 'Log out failed.') } }
  return <header className="sticky top-0 z-30 border-b border-line bg-surface"><div className="mx-auto flex h-16 max-w-[90rem] items-center justify-between px-4 sm:px-6 lg:px-8"><div className="flex items-center gap-6"><Link to="/" className="flex items-center gap-2"><span className="flex h-8 w-8 items-center justify-center rounded-md bg-action text-sm font-black text-white">P</span><span className="text-lg font-extrabold tracking-[-0.03em] text-ink">Pursuit</span></Link><nav className="hidden items-center gap-2 md:flex" aria-label="Primary"><RoleNavigation user={user} /></nav></div>{!isLoading && <><div className="hidden items-center gap-3 md:flex">{user ? <><span className="text-sm text-muted">{user.email}</span><Button variant="ghost" size="sm" onClick={handleLogout}>Log out</Button></> : <Link to="/login" className="text-sm font-semibold text-ink">Log in</Link>}</div><div className="md:hidden"><MobileNavigation user={user} onLogout={handleLogout} /></div></>}</div>{error && <p role="alert" className="border-t border-danger/20 bg-danger-soft px-4 py-2 text-center text-sm text-danger">{error}</p>}</header>
}
