import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchJobById } from '../api/jobs'

function JobDetailPage() {
  const { id } = useParams<{ id: string }>()

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['job', id],
    queryFn: () => fetchJobById(id!),
    enabled: !!id,
  })

  if (isLoading) {
    return <p>Loading job...</p>
  }

  if (isError) {
    if (error.message === 'NOT_FOUND') {
      return (
        <div>
          <p>This job could not be found.</p>
          <Link to="/jobs">Back to jobs</Link>
        </div>
      )
    }
    return <p>Error loading job: {error.message}</p>
  }

  return (
    <div>
      <h1>{data?.title}</h1>
      <p>{data?.companyName} | {data?.location}</p>
      <p>{data?.jobType}</p>
      <p>${data?.salaryMin.toLocaleString()} – ${data?.salaryMax.toLocaleString()}</p>
      <p>{data?.description}</p>
      <Link to="/jobs">Back to jobs</Link>
    </div>
  )
}

export default JobDetailPage