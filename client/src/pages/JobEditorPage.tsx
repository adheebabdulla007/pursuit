import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { createJob, fetchJobById, updateJob } from '../api/jobs'
import { JobForm } from '../components/jobs/JobForm'
import { Alert } from '../components/ui/Alert'
import { Card } from '../components/ui/Card'
import { Skeleton } from '../components/ui/Skeleton'
import type { CreateJobRequest } from '../types/job'

export default function JobEditorPage() {
  const { id } = useParams<{ id: string }>()
  const isEditing = Boolean(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const jobQuery = useQuery({ queryKey: ['job', id], queryFn: () => fetchJobById(id!), enabled: isEditing })
  const mutation = useMutation({
    mutationFn: (values: CreateJobRequest) => isEditing
      ? updateJob(id!, { ...values, isActive: jobQuery.data!.isActive })
      : createJob(values),
    onSuccess: async (job) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['employer', 'jobs'] }),
        queryClient.invalidateQueries({ queryKey: ['jobs'] }),
        queryClient.invalidateQueries({ queryKey: ['job', job.id] }),
      ])
      if (!isEditing) navigate('/employer/jobs')
    },
  })

  return (
    <main className="min-h-screen bg-canvas px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-3xl">
        <Link to="/employer/jobs" className="inline-flex items-center gap-2 text-sm font-semibold text-ink hover:text-action"><ArrowLeft size={16} aria-hidden="true" /> My jobs</Link>
        <header className="mt-5 border-b border-line pb-5">
          <p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Employer desk</p>
          <h1 className="mt-1 text-3xl font-extrabold tracking-[-0.03em] text-ink">{isEditing ? 'Edit job' : 'Post a job'}</h1>
          <p className="mt-2 text-sm text-muted">Give candidates enough concrete detail to decide whether the role fits.</p>
        </header>
        {jobQuery.isLoading && <Skeleton className="mt-6 h-[32rem]" aria-label="Loading job" />}
        {jobQuery.error && <Alert className="mt-6" variant="danger">{jobQuery.error.message}</Alert>}
        {mutation.error && <Alert className="mt-6" variant="danger">{mutation.error.message}</Alert>}
        {mutation.isSuccess && isEditing && <Alert className="mt-6" variant="success">Job changes saved.</Alert>}
        {(!isEditing || jobQuery.data) && (
          <Card className="mt-6">
            <JobForm
              key={jobQuery.data?.id ?? 'new'}
              initialValues={jobQuery.data}
              submitLabel={isEditing ? 'Save changes' : 'Publish job'}
              pendingLabel={isEditing ? 'Saving…' : 'Publishing…'}
              isPending={mutation.isPending}
              onSubmit={(values) => mutation.mutate(values)}
            />
          </Card>
        )}
      </div>
    </main>
  )
}
