import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Card } from '../components/ui/Card'

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
      await register({
        firstName,
        lastName,
        email,
        password,
        role,
        tenantName: role === 'Employer' ? tenantName : undefined,
      })
      navigate('/jobs')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed.')
      console.error(err)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-neutral-50 px-4">
      <Card className="w-full max-w-md">
        <h1 className="text-2xl font-semibold text-neutral-900 mb-6">Register</h1>
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
            <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
              {error}
            </p>
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