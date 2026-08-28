import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

function Navbar() {
  const { user, isLoading, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <nav>
      <Link to="/">Pursuit</Link>
      <Link to="/jobs">Jobs</Link>

      {isLoading ? null : user ? (
        <>
          <span>{user.email} ({user.role})</span>
          <button onClick={handleLogout}>Logout</button>
        </>
      ) : (
        <Link to="/login">Login</Link>
      )}
    </nav>
  )
}

export default Navbar