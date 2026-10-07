import type { Job } from '../../types/job'
import { Alert } from '../ui/Alert'
import { EmptyState } from '../ui/EmptyState'
import { Pagination } from '../ui/Pagination'
import { Skeleton } from '../ui/Skeleton'
import { JobResultItem } from './JobResultItem'

interface JobResultsListProps {
  jobs: Job[]
  totalCount: number
  page: number
  totalPages: number
  selectedId?: string
  search: string
  isLoading: boolean
  error?: Error | null
  onPageChange: (page: number) => void
}

export function JobResultsList({
  jobs,
  totalCount,
  page,
  totalPages,
  selectedId,
  search,
  isLoading,
  error,
  onPageChange,
}: JobResultsListProps) {
  if (isLoading) {
    return (
      <div data-testid="job-list-skeleton" role="status" aria-label="Loading jobs" className="space-y-3">
        {[1, 2, 3].map((item) => <Skeleton key={item} className="h-32 w-full" />)}
      </div>
    )
  }

  if (error) {
    return <Alert variant="danger" title="Could not load jobs">Try again in a moment.</Alert>
  }

  if (jobs.length === 0) {
    return (
      <EmptyState
        title="No jobs match this search"
        description="Remove a filter or try a broader keyword or location."
      />
    )
  }

  return (
    <>
      <p className="mb-3 text-sm text-muted tabular-nums">{totalCount} {totalCount === 1 ? 'role' : 'roles'}</p>
      <ol aria-label="Job results" className="space-y-3">
        {jobs.map((job) => (
          <JobResultItem key={job.id} job={job} selected={job.id === selectedId} search={search} />
        ))}
      </ol>
      <div className="mt-6">
        <Pagination page={page} totalPages={totalPages} onPageChange={onPageChange} />
      </div>
    </>
  )
}
