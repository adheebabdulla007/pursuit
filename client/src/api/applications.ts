import type { ApplicationDto } from '../types/application'
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