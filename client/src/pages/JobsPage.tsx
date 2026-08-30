import { useQuery } from '@tanstack/react-query'
import { fetchJobs } from '../api/jobs'
import JobCard from '../components/JobCard'

function JobsPage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['jobs'],
    queryFn: () => fetchJobs(),
  })

  if (isLoading) {
    return <p>Loading jobs...</p>
  }

  if (isError) {
    return <p>Error loading jobs: {error.message}</p>
  }

  return (
    <div>
      <h1>Pursuit — Jobs</h1>
      {data?.items.map((job) => (
        <JobCard
          key={job.id}
          id={job.id}
          title={job.title}
          companyName={job.companyName}
          location={job.location}
        />
      ))}
    </div>
  )
}

export default JobsPage