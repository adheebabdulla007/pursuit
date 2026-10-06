import { NavLink } from 'react-router-dom'
import type { CurrentUser } from '../../types/auth'
export function RoleNavigation({ user, onNavigate }: { user: CurrentUser | null; onNavigate?: () => void }) {
  const links = !user ? [{ to: '/jobs', label: 'Find jobs' }] : user.role === 'Employer' ? [{ to: '/employer/jobs', label: 'My jobs' }, { to: '/employer/jobs/new', label: 'Post a job' }] : user.role === 'Admin' ? [{ to: '/admin', label: 'Admin' }] : [{ to: '/jobs', label: 'Find jobs' }, { to: '/applications', label: 'My applications' }]
  return <>{links.map((link) => <NavLink key={link.to} to={link.to} onClick={onNavigate} className={({ isActive }) => `rounded px-2 py-1.5 text-sm font-semibold ${isActive ? 'bg-action-soft text-action' : 'text-ink hover:bg-canvas'}`}>{link.label}</NavLink>)}</>
}
