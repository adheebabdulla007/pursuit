import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { fetchJobs } from '../api/jobs'
import JobCard from '../components/JobCard'
import type { JobType } from '../types/job'

const PAGE_SIZE = 10
const JOB_TYPES: JobType[] = ['FullTime', 'PartTime', 'Contract', 'Internship', 'Remote']

function JobsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page')) || 1)
  const keyword = searchParams.get('keyword') ?? ''
  const location = searchParams.get('location') ?? ''
  const jobType = (searchParams.get('jobType') as JobType | null) ?? ''

  const [keywordDraft, setKeywordDraft] = useState(keyword)
  const [locationDraft, setLocationDraft] = useState(location)

  function updateSearchParams(updates: Record<string, string | undefined>) {
    const next = new URLSearchParams(searchParams)
    for (const [key, value] of Object.entries(updates)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    setSearchParams(next)
  }

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['jobs', page, keyword, location, jobType],
    queryFn: () =>
      fetchJobs({
        page,
        pageSize: PAGE_SIZE,
        keyword: keyword || undefined,
        location: location || undefined,
        jobType: (jobType as JobType) || undefined,
      }),
  })

  function goToPage(newPage: number) {
    updateSearchParams({ page: String(newPage) })
  }

  function handleFilterSubmit(e: React.FormEvent) {
    e.preventDefault()
    updateSearchParams({
      keyword: keywordDraft,
      location: locationDraft,
      page: '1',
    })
  }

  function handleJobTypeChange(value: string) {
    updateSearchParams({ jobType: value, page: '1' })
  }

  function clearFilters() {
    setKeywordDraft('')
    setLocationDraft('')
    updateSearchParams({ keyword: undefined, location: undefined, jobType: undefined, page: '1' })
  }

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 1

  return (
    <div>
      <h1>Pursuit — Jobs</h1>

      <form onSubmit={handleFilterSubmit}>
        <input
          type="text"
          placeholder="Keyword"
          value={keywordDraft}
          onChange={(e) => setKeywordDraft(e.target.value)}
        />
        <input
          type="text"
          placeholder="Location"
          value={locationDraft}
          onChange={(e) => setLocationDraft(e.target.value)}
        />
        <select value={jobType} onChange={(e) => handleJobTypeChange(e.target.value)}>
          <option value="">All Job Types</option>
          {JOB_TYPES.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
        <button type="submit">Search</button>
        <button type="button" onClick={clearFilters}>
          Clear
        </button>
      </form>

      {isLoading && <p>Loading jobs...</p>}
      {isError && <p>Error loading jobs: {error.message}</p>}

      {!isLoading && !isError && (
        <>
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
        </>
      )}
    </div>
  )
}

export default JobsPage