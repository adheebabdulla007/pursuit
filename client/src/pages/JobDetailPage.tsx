import { useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchJobById } from '../api/jobs'
import { applyToJob } from '../api/applications'
import { useAuth } from '../context/useAuth'
import { Alert } from '../components/ui/Alert'
import { EmptyState } from '../components/ui/EmptyState'
import { Skeleton } from '../components/ui/Skeleton'
import { JobDetailContent, type JobApplicationState } from '../components/jobs/JobDetailContent'

const ALLOWED_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
]
const MAX_SIZE_BYTES = 5 * 1024 * 1024

interface JobDetailPageProps {
  embedded?: boolean
}

function JobDetailPage({ embedded = false }: JobDetailPageProps) {
  const { id } = useParams<{ id: string }>()
  const location = useLocation()
  const { user } = useAuth()
  const [resumeFile, setResumeFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState('')
  const [applyError, setApplyError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [hasApplied, setHasApplied] = useState(false)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['job', id],
    queryFn: () => fetchJobById(id!),
    enabled: Boolean(id),
  })

  function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null
    setFileError('')
    if (!file) return setResumeFile(null)
    if (!ALLOWED_TYPES.includes(file.type)) {
      setResumeFile(null)
      return setFileError('Only PDF and DOCX files are allowed.')
    }
    if (file.size > MAX_SIZE_BYTES) {
      setResumeFile(null)
      return setFileError('File size must not exceed 5MB.')
    }
    setResumeFile(file)
  }

  async function handleApply(event: React.FormEvent) {
    event.preventDefault()
    if (!resumeFile || !id) return
    setApplyError('')
    setIsSubmitting(true)
    try {
      await applyToJob(id, resumeFile)
      setHasApplied(true)
    } catch (caughtError) {
      setApplyError(caughtError instanceof Error ? caughtError.message : 'Failed to submit application.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const returnToJobs = `/jobs${location.search}`
  const frameClassName = embedded ? '' : 'min-h-screen bg-canvas px-4 py-8'
  const contentClassName = embedded ? '' : 'mx-auto max-w-4xl'

  if (isLoading) {
    return (
      <div className={frameClassName}>
        <div className={`${contentClassName} space-y-3`} aria-label="Loading job detail">
          <Skeleton className="h-52 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      </div>
    )
  }

  if (isError) {
    const notFound = error.message === 'NOT_FOUND'
    return (
      <div className={frameClassName}>
        <div className={contentClassName}>
          {notFound ? (
            <EmptyState
              title="This job could not be found."
              description="It may have been removed or the link may be incorrect."
              action={<Link to={returnToJobs} className="font-semibold text-action underline">Back to jobs</Link>}
            />
          ) : (
            <Alert variant="danger" title="Could not load this job">Try again in a moment.</Alert>
          )}
        </div>
      </div>
    )
  }

  if (!data) return null

  let applicationState: JobApplicationState
  if (!data.isActive) {
    applicationState = { kind: 'closed' }
  } else if (!user) {
    const returnTo = encodeURIComponent(location.pathname + location.search)
    applicationState = { kind: 'visitor', loginHref: `/login?returnTo=${returnTo}` }
  } else if (user.role !== 'JobSeeker') {
    applicationState = { kind: 'employer' }
  } else {
    applicationState = {
      kind: 'jobSeeker', resumeFile, fileError, applyError, isSubmitting, hasApplied,
      onFileChange: handleFileChange, onSubmit: handleApply,
    }
  }

  return (
    <div className={frameClassName}>
      <div className={contentClassName}>
        <Link to={returnToJobs} className="mb-4 inline-flex text-sm font-semibold text-action hover:underline">
          ← Back to results
        </Link>
        <JobDetailContent job={data} applicationState={applicationState} />
      </div>
    </div>
  )
}

export default JobDetailPage
