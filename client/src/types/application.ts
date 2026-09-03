export type ApplicationStatus = 'Applied' | 'Reviewed' | 'Rejected' | 'Hired'

export type ApplicationDto = {
  id: string
  jobId: string
  jobTitle: string
  applicantId: string
  applicantName: string
  resumeUrl: string
  status: ApplicationStatus
  createdAt: string
}