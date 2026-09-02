import type { LoginRequest, RegisterRequest, CurrentUser } from '../types/auth'
import { API_BASE_URL } from '../config/env'

async function extractErrorMessage(response: Response): Promise<string> {
  try {
    const body = await response.json()
    return body.message ?? `Request failed: ${response.status}`
  } catch {
    return `Request failed: ${response.status}`
  }
}

export async function login(credentials: LoginRequest): Promise<CurrentUser> {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(credentials),
  })

  if (!response.ok) {
    throw new Error(await extractErrorMessage(response))
  }

  return getMe()
}

export async function register(data: RegisterRequest): Promise<CurrentUser> {
  const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    throw new Error(await extractErrorMessage(response))
  }

  return getMe()
}

export async function logout(): Promise<void> {
  await fetch(`${API_BASE_URL}/api/auth/logout`, {
    method: 'POST',
    credentials: 'include',
  })
}

export async function getMe(): Promise<CurrentUser> {
  const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('Not authenticated')
  }

  return response.json()
}