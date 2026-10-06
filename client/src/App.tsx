import { Routes, Route } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import HomePage from './pages/HomePage'
import JobsPage from './pages/JobsPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import EmployerJobsPage from './pages/EmployerJobsPage'
import JobEditorPage from './pages/JobEditorPage'
import ApplicationReviewPage from './pages/ApplicationReviewPage'
import MyApplicationsPage from './pages/MyApplicationsPage'
import AdminPage from './pages/AdminPage'
import ProtectedRoute from './context/ProtectedRoute'

function App() {
  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/jobs" element={<JobsPage />} />
        <Route path="/jobs/:id" element={<JobsPage />} />
        <Route
          path="/employer/jobs"
          element={
            <ProtectedRoute requiredRole="Employer">
              <EmployerJobsPage />
            </ProtectedRoute>
          }
        />
        <Route path="/employer/jobs/new" element={<ProtectedRoute requiredRole="Employer"><JobEditorPage /></ProtectedRoute>} />
        <Route path="/employer/jobs/:id/edit" element={<ProtectedRoute requiredRole="Employer"><JobEditorPage /></ProtectedRoute>} />
        <Route path="/employer/jobs/:jobId/applications" element={<ProtectedRoute requiredRole="Employer"><ApplicationReviewPage /></ProtectedRoute>} />
        <Route path="/employer/jobs/:jobId/applications/:applicationId" element={<ProtectedRoute requiredRole="Employer"><ApplicationReviewPage /></ProtectedRoute>} />
        <Route path="/applications" element={<ProtectedRoute requiredRole="JobSeeker"><MyApplicationsPage /></ProtectedRoute>} />
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
    </AppShell>
  )
}

export default App
