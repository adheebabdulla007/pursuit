import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { Button } from './ui/Button'

function Navbar() {
  const { user, isLoading, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <nav className="bg-white border-b border-neutral-200 sticky top-0 z-10">
      <div className="max-w-4xl mx-auto px-4 h-16 flex items-center justify-between">
        <div className="flex items-center gap-6">
          <Link to="/" className="flex items-center gap-2">
            <span className="w-8 h-8 rounded-md bg-primary-600 text-white flex items-center justify-center text-sm font-bold">
              P
            </span>
            <span className="text-lg font-semibold text-neutral-900">Pursuit</span>
          </Link>
          <Link to="/jobs" className="text-sm text-neutral-700 hover:text-primary-600">
            Jobs
          </Link>
        </div>

        {!isLoading && (
          <div className="flex items-center gap-4">
            {user ? (
              <>
                {user.role === 'Employer' && (
                  <Link
                    to="/jobs/new"
                    className="text-sm text-neutral-700 hover:text-primary-600"
                  >
                    Post a Job
                  </Link>
                )}
                {user.role === 'Admin' && (
                  <Link to="/admin" className="text-sm text-neutral-700 hover:text-primary-600">
                    Admin
                  </Link>
                )}
                <span className="text-sm text-neutral-500 hidden sm:inline">
                  {user.email} ({user.role})
                </span>
                <Button variant="ghost" size="sm" onClick={handleLogout}>
                  Logout
                </Button>
              </>
            ) : (
              <Link to="/login" className="text-sm text-neutral-700 hover:text-primary-600">
                Login
              </Link>
            )}
          </div>
        )}
      </div>
    </nav>
  )
}

export default Navbar