import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Card } from '../components/ui/Card'

function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setIsLoading(true)

    try {
      await login({ email, password })
      navigate('/jobs')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed. Check your credentials.')
      console.error(err)
    } finally {
      setIsLoading(false)
    }
  }

  return (
  <div className="min-h-screen flex items-center justify-center bg-neutral-50 px-4">
    <Card className="w-full max-w-md">
      <h1 className="text-2xl font-semibold text-neutral-900 mb-6">Login</h1>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <Input
          id="email"
          label="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <Input
          id="password"
          label="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        {error && (
          <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
            {error}
          </p>
        )}
        <Button type="submit" disabled={isLoading} className="w-full">
          {isLoading ? 'Logging in...' : 'Log In'}
        </Button>
      </form>
      <p className="text-sm text-neutral-600 text-center mt-4">
        Don't have an account?{' '}
        <Link to="/register" className="text-primary-600 hover:underline">
          Register
        </Link>
      </p>
    </Card>
  </div>
)
}

export default LoginPage