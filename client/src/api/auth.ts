import type { LoginRequest, RegisterRequest, CurrentUser } from '../types/auth'
import { API_BASE_URL } from '../config/env'
import { extractErrorMessage } from './shared'
import { apiFetch } from './fetchClient'
import { csrfFetch } from './csrfFetch'

export async function login(credentials: LoginRequest): Promise<CurrentUser> {
  const response = await csrfFetch(`${API_BASE_URL}/api/auth/login`, {
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
  const response = await csrfFetch(`${API_BASE_URL}/api/auth/register`, {
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
  const response = await apiFetch(`${API_BASE_URL}/api/auth/refresh/logout`, {
    method: 'POST',
  })

  if (!response.ok) {
    throw new Error(await extractErrorMessage(response))
  }
}

export async function getMe(): Promise<CurrentUser> {
  const response = await apiFetch(`${API_BASE_URL}/api/auth/me`, {}, { redirectOnFailure: false })

  if (!response.ok) {
    throw new Error('Not authenticated')
  }

  return response.json()
}
