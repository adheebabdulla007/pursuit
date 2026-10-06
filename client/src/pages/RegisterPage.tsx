import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Card } from '../components/ui/Card'
import { Alert } from '../components/ui/Alert'
import { roleHomePath } from '../routes/roleHomePath'

function RegisterPage() {
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'Employer' | 'JobSeeker'>('JobSeeker')
  const [tenantName, setTenantName] = useState('')
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const { register } = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setIsLoading(true)

    try {
      const user = await register({
        firstName,
        lastName,
        email,
        password,
        role,
        tenantName: role === 'Employer' ? tenantName : undefined,
      })
      navigate(roleHomePath(user.role))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-[calc(100vh-4rem)] bg-canvas px-4 py-12">
      <Card className="mx-auto w-full max-w-md border-t-4 border-t-action">
        <p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Career desk</p><h1 className="mb-2 mt-1 text-3xl font-extrabold text-ink">Create your account</h1><p className="mb-6 text-sm text-muted">Choose the workspace that matches what you need to do.</p>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <Input
            id="firstName"
            label="First Name"
            type="text"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
          />
          <Input
            id="lastName"
            label="Last Name"
            type="text"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
          />
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
          <div className="flex flex-col gap-1">
            <label htmlFor="role" className="text-sm font-medium text-neutral-700">
              I am a
            </label>
            <select
              id="role"
              value={role}
              onChange={(e) => setRole(e.target.value as 'Employer' | 'JobSeeker')}
              className="w-full rounded-md border border-neutral-300 px-3 py-2 text-base bg-white text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="JobSeeker">Job Seeker</option>
              <option value="Employer">Employer</option>
            </select>
          </div>
          {role === 'Employer' && (
            <Input
              id="tenantName"
              label="Company Name"
              type="text"
              value={tenantName}
              onChange={(e) => setTenantName(e.target.value)}
            />
          )}
          {error && (
            <Alert variant="danger">{error}</Alert>
          )}
          <Button type="submit" disabled={isLoading} className="w-full">
            {isLoading ? 'Registering...' : 'Register'}
          </Button>
        </form>
        <p className="text-sm text-neutral-600 text-center mt-4">
          Already have an account?{' '}
          <Link to="/login" className="text-primary-600 hover:underline">
            Log in
          </Link>
        </p>
      </Card>
    </div>
  )
}

export default RegisterPage
