import { Link } from 'react-router-dom'
import { Card } from '../ui/Card'
import { StatusBadge } from '../ui/StatusBadge'
import type { ApplicationDto } from '../../types/application'

export function ApplicationList({ applications, jobId, selectedId }: { applications: ApplicationDto[]; jobId: string; selectedId?: string }) {
  return <div className="space-y-2">{applications.map((item) => (
    <Link key={item.id} to={`/employer/jobs/${jobId}/applications/${item.id}`} aria-current={selectedId === item.id ? 'true' : undefined} className="block">
      <Card padding="sm" className={selectedId === item.id ? 'border-action border-l-4' : 'hover:border-neutral-400'}>
        <div className="flex items-start justify-between gap-3"><div><h2 className="font-bold text-ink">{item.applicantName}</h2><p className="mt-1 text-xs text-muted">Applied {new Date(item.createdAt).toLocaleDateString()}</p></div><StatusBadge status={item.status} /></div>
      </Card>
    </Link>
  ))}</div>
}
