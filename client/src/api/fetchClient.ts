import { API_BASE_URL } from '../config/env'

let refreshPromise: Promise<boolean> | null = null

async function performRefresh(): Promise<boolean> {
  const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
    method: 'POST',
    credentials: 'include',
  })
  return response.ok
}

export async function apiFetch(
  input: string,
  init?: RequestInit,
  options?: { redirectOnFailure?: boolean }
): Promise<Response> {
  const redirectOnFailure = options?.redirectOnFailure ?? true
  const response = await fetch(input, { ...init, credentials: 'include' })

  if (response.status !== 401) {
    return response
  }

  if (!refreshPromise) {
    refreshPromise = performRefresh().finally(() => {
      refreshPromise = null
    })
  }

  const refreshed = await refreshPromise

  if (!refreshed) {
    if (redirectOnFailure) window.location.href = '/login'
    return response
  }

  const retryResponse = await fetch(input, { ...init, credentials: 'include' })

  if (retryResponse.status === 401 && redirectOnFailure) {
    window.location.href = '/login'
  }

  return retryResponse
}