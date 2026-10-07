import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Users } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { fetchApplication, fetchApplicationsByJob } from '../api/applications'
import { ApplicationDetail } from '../components/applications/ApplicationDetail'
import { ApplicationList } from '../components/applications/ApplicationList'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { EmptyState } from '../components/ui/EmptyState'
import { Pagination } from '../components/ui/Pagination'
import { Skeleton } from '../components/ui/Skeleton'

const PAGE_SIZE = 10
export default function ApplicationReviewPage() {
  const { jobId = '', applicationId } = useParams<{ jobId: string; applicationId: string }>()
  const [page, setPage] = useState(1)
  const list = useQuery({ queryKey: ['applications', jobId, page], queryFn: () => fetchApplicationsByJob(jobId, page, PAGE_SIZE) })
  const detail = useQuery({ queryKey: ['application', applicationId], queryFn: () => fetchApplication(applicationId!), enabled: Boolean(applicationId) })
  const totalPages = Math.max(1, Math.ceil((list.data?.totalCount ?? 0) / PAGE_SIZE))
  return <div className="min-h-screen bg-canvas px-4 py-8 sm:px-6 lg:px-8"><div className="mx-auto max-w-6xl">
    <Link to="/employer/jobs" className="inline-flex items-center gap-2 text-sm font-semibold text-ink hover:text-action"><ArrowLeft size={16} /> My jobs</Link>
    <header className="mt-5 border-b border-line pb-5"><p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Employer desk</p><h1 className="mt-1 text-3xl font-extrabold text-ink">Application review</h1></header>
    <div className="mt-6 grid gap-5 lg:grid-cols-[340px_minmax(0,1fr)]">
      <section aria-label="Candidates">{list.isLoading && <Skeleton className="h-72" />}{list.error && <Alert variant="danger"><p>{list.error.message}</p><Button className="mt-3" size="sm" variant="secondary" onClick={() => void list.refetch()}>Try again</Button></Alert>}{list.data?.items.length === 0 && <EmptyState icon={<Users />} title="No candidates yet" description="Applications will appear here when candidates apply." />}{list.data && <><ApplicationList applications={list.data.items} jobId={jobId} selectedId={applicationId} /><div className="mt-4"><Pagination page={page} totalPages={totalPages} onPageChange={setPage} /></div></>}</section>
      <Card>{applicationId ? <>{detail.isLoading && <Skeleton className="h-72" />}{detail.error && <Alert variant="danger">{detail.error.message}</Alert>}{detail.data && <ApplicationDetail application={detail.data} />}</> : <EmptyState title="Select a candidate" description="Open a candidate to review their resume and update their status." />}</Card>
    </div>
  </div></div>
}
