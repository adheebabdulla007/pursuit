import type { Job, PagedResult } from '../types/job'
import { API_BASE_URL } from '../config/env'

export async function fetchJobs(page: number = 1, pageSize: number = 10): Promise<PagedResult<Job>> {
  const response = await fetch(
    `${API_BASE_URL}/api/jobs?page=${page}&pageSize=${pageSize}`,
    { credentials: 'include' }
  )

  if (!response.ok) {
    throw new Error(`Failed to fetch jobs: ${response.status}`)
  }

  return response.json()
}

export async function fetchJobById(id: string): Promise<Job> {
  const response = await fetch(`${API_BASE_URL}/api/jobs/${id}`, {
    credentials: 'include',
  })

  if (response.status === 404) {
    throw new Error('NOT_FOUND')
  }

  if (!response.ok) {
    throw new Error(`Failed to fetch job: ${response.status}`)
  }

  return response.json()
}