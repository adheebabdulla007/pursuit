export type ApplicationStatus = 'Applied' | 'Reviewed' | 'Rejected' | 'Hired'

export type ApplicationDto = {
  id: string
  jobId: string
  jobTitle: string
  companyName: string
  applicantId: string
  applicantName: string
  status: ApplicationStatus
  createdAt: string
}
