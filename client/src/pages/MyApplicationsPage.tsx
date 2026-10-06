import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Briefcase, RefreshCw } from 'lucide-react'
import { Link } from 'react-router-dom'
import { fetchMyApplications } from '../api/applications'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { EmptyState } from '../components/ui/EmptyState'
import { Pagination } from '../components/ui/Pagination'
import { Skeleton } from '../components/ui/Skeleton'
import { StatusBadge } from '../components/ui/StatusBadge'

const PAGE_SIZE = 10
export default function MyApplicationsPage() {
  const [page, setPage] = useState(1)
  const query = useQuery({ queryKey: ['my-applications', page], queryFn: () => fetchMyApplications(page, PAGE_SIZE) })
  const pages = Math.max(1, Math.ceil((query.data?.totalCount ?? 0) / PAGE_SIZE))
  return <main className="min-h-screen bg-canvas px-4 py-8 sm:px-6 lg:px-8"><div className="mx-auto max-w-5xl">
    <header className="flex flex-col gap-4 border-b border-line pb-6 sm:flex-row sm:items-end sm:justify-between"><div><p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Career desk</p><h1 className="mt-1 text-3xl font-extrabold text-ink">My applications</h1><p className="mt-2 text-sm text-muted">A current record of the roles you have applied for.</p></div><Button variant="secondary" onClick={() => void query.refetch()} disabled={query.isFetching}><RefreshCw size={16} /> Refresh status</Button></header>
    {query.isLoading && <div className="mt-6 space-y-3"><Skeleton className="h-28" /><Skeleton className="h-28" /></div>}
    {query.error && <Alert className="mt-6" variant="danger"><p>{query.error.message}</p><Button className="mt-3" size="sm" variant="secondary" onClick={() => void query.refetch()}>Try again</Button></Alert>}
    {query.data?.items.length === 0 && <EmptyState className="mt-6" icon={<Briefcase />} title="No applications yet" description="Find an active role and apply when the work fits." action={<Link to="/jobs" className="font-semibold text-action hover:text-action-hover">Find jobs</Link>} />}
    {query.data && query.data.items.length > 0 && <div className="mt-6 space-y-3">{query.data.items.map((application) => <Card key={application.id} className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center"><div><p className="text-sm font-semibold text-action">{application.companyName}</p><Link to={`/jobs/${application.jobId}`} className="mt-1 inline-block text-lg font-bold text-ink hover:text-action">{application.jobTitle}</Link><p className="mt-2 text-sm text-muted">Submitted {new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(application.createdAt))}</p></div><StatusBadge status={application.status} /></Card>)}<div className="pt-3"><Pagination page={page} totalPages={pages} onPageChange={setPage} /></div></div>}
  </div></main>
}
