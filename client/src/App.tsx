import { Routes, Route } from 'react-router-dom'
import HomePage from './pages/HomePage'
import JobsPage from './pages/JobsPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/jobs" element={<JobsPage />} />
    </Routes>
  )
}

export default App