export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  firstName: string
  lastName: string
  email: string
  password: string
  role: 'Employer' | 'JobSeeker'
  tenantName?: string
}

export interface CurrentUser {
  email: string
  role: string
  tenantId: string | null
}