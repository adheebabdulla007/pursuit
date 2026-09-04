import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'

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
    <div>
      <h1>Login</h1>
      <form onSubmit={handleSubmit}>
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
        {error && <p style={{ color: 'red' }}>{error}</p>}
        <Button type="submit" disabled={isLoading}>
          {isLoading ? 'Logging in...' : 'Log In'}
        </Button>
        <p> Don't have an account? <Link to="/register">Register</Link></p>
      </form>
    </div>
  )
}

export default LoginPage