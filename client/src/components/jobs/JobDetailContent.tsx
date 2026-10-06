import { BriefcaseBusiness, Building2, CalendarDays, MapPin } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { Job } from '../../types/job'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { StatusBadge } from '../ui/StatusBadge'

export type JobApplicationState =
  | { kind: 'visitor'; loginHref: string }
  | { kind: 'employer' }
  | { kind: 'closed' }
  | {
      kind: 'jobSeeker'
      resumeFile: File | null
      fileError: string
      applyError: string
      isSubmitting: boolean
      hasApplied: boolean
      onFileChange: (event: React.ChangeEvent<HTMLInputElement>) => void
      onSubmit: (event: React.FormEvent) => void
    }

interface JobDetailContentProps {
  job: Job
  applicationState: JobApplicationState
}

function formatJobType(jobType: Job['jobType']) {
  return jobType.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function JobDetailContent({ job, applicationState }: JobDetailContentProps) {
  return (
    <article className="space-y-5">
      <Card className="overflow-hidden" padding="none">
        <header className="border-b border-line px-5 py-6 sm:px-7">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-sm font-semibold text-action">{job.companyName}</p>
              <h1 className="mt-1 text-2xl font-extrabold tracking-[-0.025em] text-ink sm:text-3xl">
                {job.title}
              </h1>
            </div>
            <StatusBadge status={job.isActive ? 'Open' : 'Closed'} />
          </div>
          <dl className="mt-5 grid gap-3 text-sm text-muted sm:grid-cols-2">
            <div className="flex items-center gap-2"><Building2 aria-hidden="true" size={16} /><dt className="sr-only">Company</dt><dd>{job.companyName}</dd></div>
            <div className="flex items-center gap-2"><MapPin aria-hidden="true" size={16} /><dt className="sr-only">Location</dt><dd>{job.location}</dd></div>
            <div className="flex items-center gap-2"><BriefcaseBusiness aria-hidden="true" size={16} /><dt className="sr-only">Job type</dt><dd>{formatJobType(job.jobType)}</dd></div>
            <div className="flex items-center gap-2"><CalendarDays aria-hidden="true" size={16} /><dt className="sr-only">Posted</dt><dd>Posted {new Date(job.createdAt).toLocaleDateString()}</dd></div>
          </dl>
          <p className="mt-5 text-sm font-bold text-ink tabular-nums">
            Salary range: {job.salaryMin.toLocaleString()} – {job.salaryMax.toLocaleString()}
          </p>
        </header>
        <section aria-labelledby="job-description-heading" className="px-5 py-6 sm:px-7">
          <h2 id="job-description-heading" className="text-lg font-bold text-ink">About the role</h2>
          <p className="mt-3 max-w-[72ch] whitespace-pre-line text-[0.95rem] leading-7 text-neutral-700">
            {job.description}
          </p>
        </section>
      </Card>

      <div className="lg:sticky lg:bottom-4">
        {applicationState.kind === 'closed' && (
          <Alert variant="warning" title="This job is closed">
            Applications are no longer being accepted. Existing applicants can still follow their status.
          </Alert>
        )}

        {applicationState.kind === 'visitor' && (
          <Card padding="sm" className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-neutral-700">Sign in with a job-seeker account to apply.</p>
            <Link to={applicationState.loginHref} className="font-semibold text-action hover:underline">
              Log in to apply
            </Link>
          </Card>
        )}

        {applicationState.kind === 'employer' && (
          <Alert variant="info">Employer accounts can review the role but cannot submit applications.</Alert>
        )}

        {applicationState.kind === 'jobSeeker' && (
          <Card padding="sm">
            {applicationState.hasApplied ? (
              <Alert variant="success" title="Application submitted.">
                <Link to="/applications" className="font-semibold underline">View My Applications</Link>
              </Alert>
            ) : (
              <form onSubmit={applicationState.onSubmit} className="space-y-4">
                <div>
                  <label htmlFor="resume" className="text-sm font-semibold text-ink">
                    Resume <span className="font-normal text-muted">(PDF or DOCX, max 5MB)</span>
                  </label>
                  <input
                    id="resume"
                    type="file"
                    accept=".pdf,.docx"
                    onChange={applicationState.onFileChange}
                    className="mt-2 block w-full rounded-md border border-line bg-surface p-2 text-sm text-muted file:mr-3 file:rounded file:border-0 file:bg-primary-50 file:px-3 file:py-2 file:font-semibold file:text-action"
                  />
                </div>
                {applicationState.fileError && <Alert variant="danger">{applicationState.fileError}</Alert>}
                {applicationState.applyError && <Alert variant="danger">{applicationState.applyError}</Alert>}
                <Button type="submit" disabled={!applicationState.resumeFile || applicationState.isSubmitting}>
                  {applicationState.isSubmitting ? 'Submitting…' : 'Apply for this role'}
                </Button>
              </form>
            )}
          </Card>
        )}
      </div>
    </article>
  )
}
