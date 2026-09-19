import { useState, useEffect } from 'react'
import type { ReactNode } from 'react'
import { getMe, login as apiLogin, logout as apiLogout, register as apiRegister } from '../api/auth'
import type { CurrentUser, LoginRequest, RegisterRequest } from '../types/auth'
import { AuthContext } from './auth-context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    getMe()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setIsLoading(false))
  }, [])

  async function login(credentials: LoginRequest) {
    const currentUser = await apiLogin(credentials)
    setUser(currentUser)
  }

  async function register(data: RegisterRequest) {
    const currentUser = await apiRegister(data)
    setUser(currentUser)
  }

  async function logout() {
    await apiLogout()
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}
