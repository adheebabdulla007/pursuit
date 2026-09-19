import { API_BASE_URL } from '../config/env'

/** Fetch a token bound to the current cookie identity for each unsafe request. */
export async function csrfFetch(input: string, init: RequestInit = {}): Promise<Response> {
  const method = (init.method ?? 'GET').toUpperCase()
  if (['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(method)) {
    return fetch(input, { ...init, credentials: 'include' })
  }

  const bootstrap = await fetch(`${API_BASE_URL}/api/auth/csrf`, { credentials: 'include' })
  if (!bootstrap.ok) throw new Error('Could not prepare a secure request. Please try again.')
  const payload: unknown = await bootstrap.json()
  if (!payload || typeof payload !== 'object' || !('token' in payload)
      || typeof payload.token !== 'string' || !payload.token) {
    throw new Error('Could not prepare a secure request. Please try again.')
  }

  const headers = new Headers(init.headers)
  headers.set('X-CSRF-TOKEN', payload.token)
  return fetch(input, { ...init, headers, credentials: 'include' })
}
