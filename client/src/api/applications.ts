import type { ApplicationDto, ApplicationStatus } from '../types/application'
import type { PagedResult } from '../types/job'
import { API_BASE_URL } from '../config/env'
import { extractErrorMessage } from './shared'
import { apiFetch } from './fetchClient'

export async function applyToJob(jobId: string, resume: File): Promise<ApplicationDto> {
  const formData = new FormData()
  formData.append('jobId', jobId)
  formData.append('resume', resume)

  const response = await apiFetch(`${API_BASE_URL}/api/applications`, {
    method: 'POST',
    body: formData,
  })
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}

export async function fetchApplicationsByJob(jobId: string, page: number, pageSize: number): Promise<PagedResult<ApplicationDto>> {
  const response = await apiFetch(`${API_BASE_URL}/api/applications/job/${jobId}?page=${page}&pageSize=${pageSize}`)
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}

export async function fetchApplication(id: string): Promise<ApplicationDto> {
  const response = await apiFetch(`${API_BASE_URL}/api/applications/${id}`)
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}

export async function updateApplicationStatus(id: string, status: ApplicationStatus): Promise<ApplicationDto> {
  const response = await apiFetch(`${API_BASE_URL}/api/applications/${id}/status`, {
    method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ status }),
  })
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}

export async function fetchResumeDownloadUrl(id: string): Promise<{ downloadUrl: string }> {
  const response = await apiFetch(`${API_BASE_URL}/api/applications/${id}/resume`)
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}
