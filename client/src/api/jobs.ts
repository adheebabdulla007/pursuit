import type { Job, JobType, PagedResult } from '../types/job'
import { API_BASE_URL } from '../config/env'

export type JobSearchParams = {
  page?: number
  pageSize?: number
  keyword?: string
  location?: string
  jobType?: JobType
}

export async function fetchJobs(params: JobSearchParams): Promise<PagedResult<Job>> {
  const query = new URLSearchParams()
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  if (params.keyword) query.set('keyword', params.keyword)
  if (params.location) query.set('location', params.location)
  if (params.jobType) query.set('jobType', params.jobType)

  const response = await fetch(`${API_BASE_URL}/api/jobs?${query.toString()}`, {
    credentials: 'include',
  })
  if (!response.ok) throw new Error(`Failed to fetch jobs: ${response.status}`)
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