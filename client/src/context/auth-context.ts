import { createContext } from 'react'
import type { CurrentUser, LoginRequest, RegisterRequest } from '../types/auth'

export interface AuthContextType {
  user: CurrentUser | null
  isLoading: boolean
  login: (credentials: LoginRequest) => Promise<void>
  register: (data: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined)
