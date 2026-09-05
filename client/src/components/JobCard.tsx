import { Link } from 'react-router-dom'
import { Card } from './ui/Card'

type JobCardProps = {
  id: string
  title: string
  companyName: string
  location: string
}

function JobCard({ id, title, companyName, location }: JobCardProps) {
  return (
    <Link to={`/jobs/${id}`} className="block">
      <Card className="hover:shadow-md hover:border-primary-300 transition-shadow cursor-pointer">
        <h2 className="text-lg font-semibold text-neutral-900">{title}</h2>
        <p className="text-sm text-neutral-600 mt-1">
          {companyName} · {location}
        </p>
      </Card>
    </Link>
  )
}

export default JobCard