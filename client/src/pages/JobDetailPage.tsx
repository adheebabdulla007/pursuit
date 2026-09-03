import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchJobById } from '../api/jobs'
import { applyToJob } from '../api/applications'
import { useAuth } from '../context/AuthContext'

const ALLOWED_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
]
const MAX_SIZE_BYTES = 5 * 1024 * 1024

function JobDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()

  const [resumeFile, setResumeFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState('')
  const [applyError, setApplyError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [hasApplied, setHasApplied] = useState(false)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['job', id],
    queryFn: () => fetchJobById(id!),
    enabled: !!id,
  })

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0] ?? null
    setFileError('')

    if (!file) {
      setResumeFile(null)
      return
    }

    if (!ALLOWED_TYPES.includes(file.type)) {
      setFileError('Only PDF and DOCX files are allowed.')
      setResumeFile(null)
      return
    }

    if (file.size > MAX_SIZE_BYTES) {
      setFileError('File size must not exceed 5MB.')
      setResumeFile(null)
      return
    }

    setResumeFile(file)
  }

  async function handleApply(e: React.FormEvent) {
    e.preventDefault()
    if (!resumeFile || !id) return

    setApplyError('')
    setIsSubmitting(true)

    try {
      await applyToJob(id, resumeFile)
      setHasApplied(true)
    } catch (err) {
      setApplyError(err instanceof Error ? err.message : 'Failed to submit application.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return <p>Loading job...</p>
  }

  if (isError) {
    if (error.message === 'NOT_FOUND') {
      return (
        <div>
          <p>This job could not be found.</p>
          <Link to="/jobs">Back to jobs</Link>
        </div>
      )
    }
    return <p>Error loading job: {error.message}</p>
  }

  return (
    <div>
      <h1>{data?.title}</h1>
      <p>{data?.companyName} | {data?.location}</p>
      <p>{data?.jobType}</p>
      <p>${data?.salaryMin.toLocaleString()} – ${data?.salaryMax.toLocaleString()}</p>
      <p>{data?.description}</p>

      {user?.role === 'JobSeeker' && (
        <div>
          {hasApplied ? (
            <p>Application submitted.</p>
          ) : (
            <form onSubmit={handleApply}>
              <label htmlFor="resume">Resume (PDF or DOCX, max 5MB)</label>
              <input id="resume" type="file" accept=".pdf,.docx" onChange={handleFileChange} />
              {fileError && <p style={{ color: 'red' }}>{fileError}</p>}
              {applyError && <p style={{ color: 'red' }}>{applyError}</p>}
              <button type="submit" disabled={!resumeFile || isSubmitting}>
                {isSubmitting ? 'Submitting...' : 'Apply'}
              </button>
            </form>
          )}
        </div>
      )}

      {!user && (
        <p>
          <Link to="/login">Log in</Link> as a job seeker to apply.
        </p>
      )}

      <Link to="/jobs">Back to jobs</Link>
    </div>
  )
}

export default JobDetailPage