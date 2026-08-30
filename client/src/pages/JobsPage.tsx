import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { fetchJobs } from '../api/jobs'
import JobCard from '../components/JobCard'

const PAGE_SIZE = 10

function JobsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page')) || 1)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['jobs', page],
    queryFn: () => fetchJobs(page, PAGE_SIZE),
  })

  function goToPage(newPage: number) {
    setSearchParams({ page: String(newPage) })
  }

  if (isLoading) {
    return <p>Loading jobs...</p>
  }

  if (isError) {
    return <p>Error loading jobs: {error.message}</p>
  }

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 1

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
      <div>
        <button onClick={() => goToPage(page - 1)} disabled={page <= 1}>
          Previous
        </button>
        <span> Page {page} of {totalPages} </span>
        <button onClick={() => goToPage(page + 1)} disabled={page >= totalPages}>
          Next
        </button>
      </div>
    </div>
  )
}

export default JobsPage