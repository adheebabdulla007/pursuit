export type AdminUserRole = 'Admin' | 'Employer' | 'JobSeeker'

export type AdminUser = {
  id: string
  firstName: string
  lastName: string
  email: string
  role: AdminUserRole
  tenantId: string | null
  createdAt: string
  isActive: boolean
}

export type AdminStats = {
  totalUsers: number
  totalEmployers: number
  totalJobSeekers: number
  totalJobs: number
  totalApplications: number
}