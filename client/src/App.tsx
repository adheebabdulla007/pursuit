import { Routes, Route } from 'react-router-dom'
import Navbar from './components/Navbar'
import HomePage from './pages/HomePage'
import JobsPage from './pages/JobsPage'
import JobDetailPage from './pages/JobDetailPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import CreateJobPage from './pages/CreateJobPage'
import AdminPage from './pages/AdminPage'
import ProtectedRoute from './context/ProtectedRoute'

function App() {
  return (
    <>
      <Navbar />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/jobs" element={<JobsPage />} />
        <Route path="/jobs/:id" element={<JobDetailPage />} />
        <Route
          path="/jobs/new"
          element={
            <ProtectedRoute requiredRole="Employer">
              <CreateJobPage />
            </ProtectedRoute>
          }
        />
         <Route
          path="/admin"
          element={
            <ProtectedRoute requiredRole="Admin">
              <AdminPage />
            </ProtectedRoute>
          }
        />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Routes>
    </>
  )
}

export default App