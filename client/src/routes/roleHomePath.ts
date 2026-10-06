import type { CurrentUser } from '../types/auth'
export function roleHomePath(role: CurrentUser['role']): '/employer/jobs' | '/jobs' | '/admin' {
  if (role === 'Employer') return '/employer/jobs'
  if (role === 'Admin') return '/admin'
  return '/jobs'
}
