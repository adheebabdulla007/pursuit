export type JobType = 'FullTime' | 'PartTime' | 'Contract' | 'Internship' | 'Remote'

export type Job = {
  id: string
  tenantId: string
  title: string
  companyName: string
  description: string
  location: string
  salaryMin: number
  salaryMax: number
  jobType: JobType
  isActive: boolean
  createdAt: string
}

export type PagedResult<T> = {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}