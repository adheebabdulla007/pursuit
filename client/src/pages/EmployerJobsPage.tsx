import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BriefcaseBusiness, Plus } from 'lucide-react'
import { Link } from 'react-router-dom'
import { fetchMyJobs, updateJob } from '../api/jobs'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { ConfirmationDialog } from '../components/ui/ConfirmationDialog'
import { EmptyState } from '../components/ui/EmptyState'
import { Pagination } from '../components/ui/Pagination'
import { Skeleton } from '../components/ui/Skeleton'
import { StatusBadge } from '../components/ui/StatusBadge'
import type { Job } from '../types/job'

const PAGE_SIZE = 10
type Filter = 'all' | 'open' | 'closed'

function statusPayload(job: Job) {
  return {
    title: job.title, description: job.description, location: job.location,
    salaryMin: job.salaryMin, salaryMax: job.salaryMax, jobType: job.jobType, isActive: !job.isActive,
  }
}

export default function EmployerJobsPage() {
  const [filter, setFilter] = useState<Filter>('all')
  const [page, setPage] = useState(1)
  const queryClient = useQueryClient()
  const isActive = filter === 'all' ? undefined : filter === 'open'
  const query = useQuery({
    queryKey: ['employer', 'jobs', isActive, page],
    queryFn: () => fetchMyJobs({ isActive, page, pageSize: PAGE_SIZE }),
  })
  const mutation = useMutation({
    mutationFn: (job: Job) => updateJob(job.id, statusPayload(job)),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['employer', 'jobs'] }),
        queryClient.invalidateQueries({ queryKey: ['jobs'] }),
      ])
    },
  })
  const totalPages = Math.max(1, Math.ceil((query.data?.totalCount ?? 0) / PAGE_SIZE))

  function selectFilter(next: Filter) {
    setFilter(next)
    setPage(1)
    mutation.reset()
  }

  return (
    <div className="min-h-screen bg-canvas px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-4 border-b border-line pb-6 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Employer desk</p>
            <h1 className="mt-1 text-3xl font-extrabold tracking-[-0.03em] text-ink">My jobs</h1>
            <p className="mt-2 text-sm text-muted">Manage published roles and control which ones remain open.</p>
          </div>
          <Link to="/employer/jobs/new" className="inline-flex h-10 items-center justify-center gap-2 rounded-md bg-action px-4 font-semibold text-white hover:bg-action-hover">
            <Plus size={17} aria-hidden="true" /> Post a job
          </Link>
        </header>

        <div className="mt-6 flex gap-2" aria-label="Job status filters">
          {(['all', 'open', 'closed'] as const).map((value) => (
            <Button key={value} type="button" size="sm" variant={filter === value ? 'primary' : 'secondary'} onClick={() => selectFilter(value)}>
              {value[0].toUpperCase() + value.slice(1)}
            </Button>
          ))}
        </div>

        {mutation.error && <Alert className="mt-5" variant="danger">{mutation.error.message}</Alert>}
        {query.isLoading && <div className="mt-5 space-y-3" aria-label="Loading jobs"><Skeleton className="h-36" /><Skeleton className="h-36" /></div>}
        {query.error && (
          <Alert className="mt-5" variant="danger" title="Jobs could not be loaded">
            <p>{query.error.message}</p>
            <Button type="button" size="sm" variant="secondary" className="mt-3" onClick={() => void query.refetch()}>Try again</Button>
          </Alert>
        )}
        {!query.isLoading && !query.error && query.data?.items.length === 0 && (
          <EmptyState className="mt-5" icon={<BriefcaseBusiness />} title="No jobs in this view" description="Change the status filter or post a new role." />
        )}
        {query.data && query.data.items.length > 0 && (
          <div className="mt-5 space-y-3">
            {query.data.items.map((job) => (
              <Card key={job.id} className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-lg font-bold text-ink">{job.title}</h2>
                    <StatusBadge status={job.isActive ? 'Open' : 'Closed'} />
                  </div>
                  <p className="mt-1 text-sm text-muted">{job.location} · {job.jobType}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Link to={`/employer/jobs/${job.id}/applications`} className="inline-flex h-8 items-center rounded-md border border-line bg-surface px-3 text-sm font-semibold text-ink hover:bg-canvas">Applications</Link>
                  <Link to={`/employer/jobs/${job.id}/edit`} className="inline-flex h-8 items-center rounded-md border border-line bg-surface px-3 text-sm font-semibold text-ink hover:bg-canvas">Edit</Link>
                  <ConfirmationDialog
                    trigger={<Button type="button" size="sm" variant={job.isActive ? 'destructive' : 'secondary'} aria-label={`${job.isActive ? 'Close' : 'Reopen'} ${job.title}`} disabled={mutation.isPending}>{mutation.isPending && mutation.variables?.id === job.id ? 'Working…' : job.isActive ? 'Close' : 'Reopen'}</Button>}
                    title={`${job.isActive ? 'Close' : 'Reopen'} ${job.title}?`}
                    description={job.isActive ? 'The role will leave public search, but its record and applications remain available.' : 'The role will return to public search.'}
                    confirmLabel={job.isActive ? 'Close job' : 'Reopen job'}
                    variant={job.isActive ? 'danger' : 'default'}
                    pending={mutation.isPending}
                    onConfirm={() => mutation.mutate(job)}
                  />
                </div>
              </Card>
            ))}
            <div className="pt-4"><Pagination page={page} totalPages={totalPages} onPageChange={setPage} /></div>
          </div>
        )}
      </div>
    </div>
  )
}
