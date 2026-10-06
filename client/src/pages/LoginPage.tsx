import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Card } from '../components/ui/Card'
import { Alert } from '../components/ui/Alert'
import { roleHomePath } from '../routes/roleHomePath'

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
      const user = await login({ email, password })
      navigate(roleHomePath(user.role))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed. Check your credentials.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
  <div className="min-h-[calc(100vh-4rem)] bg-canvas px-4 py-12">
    <Card className="mx-auto w-full max-w-md border-t-4 border-t-action">
      <p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Career desk</p><h1 className="mb-2 mt-1 text-3xl font-extrabold text-ink">Welcome back</h1><p className="mb-6 text-sm text-muted">Sign in to continue your work.</p>
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
          <Alert variant="danger">{error}</Alert>
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
