import { Link } from 'react-router-dom'

type JobCardProps = {
  id: string
  title: string
  companyName: string
  location: string
}

function JobCard({ id, title, companyName, location }: JobCardProps) {
  return (
    <Link to={`/jobs/${id}`}>
      <div>
        <h1>{title}</h1>
        <p>{companyName} | {location}</p>
      </div>
    </Link>
  )
}

export default JobCard