import { Link } from 'react-router-dom'
import { Card } from './ui/Card'

type JobCardProps = {
  id: string
  title: string
  companyName: string
  location: string
  search?: string
  selected?: boolean
}

function JobCard({ id, title, companyName, location, search = '', selected = false }: JobCardProps) {
  return (
    <Link
      to={`/jobs/${id}${search}`}
      aria-current={selected ? 'page' : undefined}
      className="group block focus-visible:outline-none"
    >
      <Card
        padding="sm"
        className={`relative transition-colors group-hover:border-action ${
          selected ? 'border-action bg-primary-50 before:absolute before:inset-y-0 before:left-0 before:w-1 before:bg-action' : ''
        }`}
      >
        <div className="flex items-start justify-between gap-3">
          <h2 className="text-base font-bold leading-6 text-ink">{title}</h2>
          {selected && <span className="text-xs font-semibold text-action">Selected</span>}
        </div>
        <p className="mt-1 text-sm font-medium text-neutral-700">{companyName}</p>
        <p className="mt-1 text-sm text-muted">
          {location}
        </p>
      </Card>
    </Link>
  )
}

export default JobCard
