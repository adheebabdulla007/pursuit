import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchJobById } from '../api/jobs'
import { applyToJob } from '../api/applications'
import { useAuth } from '../context/AuthContext'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'

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
    return (
      <div className="min-h-screen bg-neutral-50 px-4 py-8">
        <p className="max-w-3xl mx-auto text-neutral-600">Loading job...</p>
      </div>
    )
  }

  if (isError) {
    if (error.message === 'NOT_FOUND') {
      return (
        <div className="min-h-screen bg-neutral-50 px-4 py-8">
          <div className="max-w-3xl mx-auto">
            <p className="text-neutral-700 mb-4">This job could not be found.</p>
            <Link to="/jobs" className="text-primary-600 hover:underline">
              Back to jobs
            </Link>
          </div>
        </div>
      )
    }
    return (
      <div className="min-h-screen bg-neutral-50 px-4 py-8">
        <p className="max-w-3xl mx-auto bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
          Error loading job: {error.message}
        </p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-neutral-50 px-4 py-8">
      <div className="max-w-3xl mx-auto">
        <Link to="/jobs" className="text-sm text-primary-600 hover:underline mb-4 inline-block">
          ← Back to jobs
        </Link>

        <Card className="mb-6">
          <h1 className="text-2xl font-semibold text-neutral-900">{data?.title}</h1>
          <p className="text-neutral-600 mt-1">
            {data?.companyName} · {data?.location}
          </p>
          <p className="text-sm text-neutral-500 mt-1">{data?.jobType}</p>
          <p className="text-sm font-medium text-neutral-700 mt-2">
            ${data?.salaryMin.toLocaleString()} – ${data?.salaryMax.toLocaleString()}
          </p>
          <p className="text-neutral-700 mt-4 whitespace-pre-line">{data?.description}</p>
        </Card>

        {user?.role === 'JobSeeker' && (
          <Card>
            {hasApplied ? (
              <p className="bg-green-50 border border-green-200 text-green-700 rounded-md p-3 text-sm">
                Application submitted.
              </p>
            ) : (
              <form onSubmit={handleApply} className="flex flex-col gap-4">
                <div className="flex flex-col gap-1">
                  <label htmlFor="resume" className="text-sm font-medium text-neutral-700">
                    Resume (PDF or DOCX, max 5MB)
                  </label>
                  <input
                    id="resume"
                    type="file"
                    accept=".pdf,.docx"
                    onChange={handleFileChange}
                    className="text-sm text-neutral-600
                      file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0
                      file:bg-primary-50 file:text-primary-700 file:text-sm file:font-medium
                      hover:file:bg-primary-100"
                  />
                </div>
                {fileError && (
                  <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
                    {fileError}
                  </p>
                )}
                {applyError && (
                  <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
                    {applyError}
                  </p>
                )}
                <Button type="submit" disabled={!resumeFile || isSubmitting}>
                  {isSubmitting ? 'Submitting...' : 'Apply'}
                </Button>
              </form>
            )}
          </Card>
        )}

        {!user && (
          <p className="text-neutral-600">
            <Link to="/login" className="text-primary-600 hover:underline">
              Log in
            </Link>{' '}
            as a job seeker to apply.
          </p>
        )}
      </div>
    </div>
  )
}

export default JobDetailPage