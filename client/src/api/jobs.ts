import type { Job, PagedResult } from '../types/job'

const API_BASE_URL = 'http://localhost:5146'

export async function fetchJobs(page: number = 1, pageSize: number = 10): Promise<PagedResult<Job>> {
  const response = await fetch(
    `${API_BASE_URL}/api/jobs?page=${page}&pageSize=${pageSize}`
  )

  if (!response.ok) {
    throw new Error(`Failed to fetch jobs: ${response.status}`)
  }

  return response.json()
}