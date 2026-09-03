import type { PagedResult } from '../types/job'
import type { AdminUser, AdminStats } from '../types/admin'
import { API_BASE_URL } from '../config/env'
import { extractErrorMessage } from './shared'

export async function fetchUsers(page: number, pageSize: number): Promise<PagedResult<AdminUser>> {
  const response = await fetch(`${API_BASE_URL}/api/admin/users?page=${page}&pageSize=${pageSize}`, {
    credentials: 'include',
  })
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}

export async function fetchStats(): Promise<AdminStats> {
  const response = await fetch(`${API_BASE_URL}/api/admin/stats`, {
    credentials: 'include',
  })
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}

export async function updateUserStatus(userId: string, isActive: boolean): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/admin/users/${userId}/status`, {
    method: 'PATCH',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isActive }),
  })
  if (!response.ok) throw new Error(await extractErrorMessage(response))
}