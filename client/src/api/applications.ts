import type { ApplicationDto } from '../types/application'
import { API_BASE_URL } from '../config/env'
import { extractErrorMessage } from './shared'

export async function applyToJob(jobId: string, resume: File): Promise<ApplicationDto> {
  const formData = new FormData()
  formData.append('jobId', jobId)
  formData.append('resume', resume)

  const response = await fetch(`${API_BASE_URL}/api/applications`, {
    method: 'POST',
    credentials: 'include',
    body: formData,
  })
  if (!response.ok) throw new Error(await extractErrorMessage(response))
  return response.json()
}