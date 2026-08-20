export interface LoginRequest {
  email: string
  password: string
}

export interface CurrentUser {
  email: string
  role: string
  tenantId: string | null
}