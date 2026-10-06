import { createContext } from 'react'
import type { CurrentUser, LoginRequest, RegisterRequest } from '../types/auth'

export interface AuthContextType {
  user: CurrentUser | null
  isLoading: boolean
  login: (credentials: LoginRequest) => Promise<CurrentUser>
  register: (data: RegisterRequest) => Promise<CurrentUser>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined)
