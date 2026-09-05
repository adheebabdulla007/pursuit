import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { fetchJobs } from '../api/jobs'
import JobCard from '../components/JobCard'
import { Card } from '../components/ui/Card'
import { Input } from '../components/ui/Input'
import { Button } from '../components/ui/Button'
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
    <div className="min-h-screen bg-neutral-50 px-4 py-8">
      <div className="max-w-3xl mx-auto">
        <h1 className="text-2xl font-semibold text-neutral-900 mb-6">Pursuit — Jobs</h1>

        <Card className="mb-6">
          <form onSubmit={handleFilterSubmit} className="flex flex-col sm:flex-row gap-3 sm:items-end">
            <div className="flex-1">
              <Input
                id="keyword"
                label="Keyword"
                type="text"
                value={keywordDraft}
                onChange={(e) => setKeywordDraft(e.target.value)}
              />
            </div>
            <div className="flex-1">
              <Input
                id="location"
                label="Location"
                type="text"
                value={locationDraft}
                onChange={(e) => setLocationDraft(e.target.value)}
              />
            </div>
            <div className="flex flex-col gap-1 flex-1">
              <label htmlFor="jobType" className="text-sm font-medium text-neutral-700">
                Job Type
              </label>
              <select
                id="jobType"
                value={jobType}
                onChange={(e) => handleJobTypeChange(e.target.value)}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-base bg-white text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="">All Job Types</option>
                {JOB_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex gap-2">
              <Button type="submit">Search</Button>
              <Button type="button" variant="secondary" onClick={clearFilters}>
                Clear
              </Button>
            </div>
          </form>
        </Card>

        {isLoading && <p className="text-neutral-600">Loading jobs...</p>}
        {isError && (
          <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
            Error loading jobs: {error.message}
          </p>
        )}

        {!isLoading && !isError && (
          <>
            <div className="flex flex-col gap-4">
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
            <div className="flex items-center justify-center gap-4 mt-6">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => goToPage(page - 1)}
                disabled={page <= 1}
              >
                Previous
              </Button>
              <span className="text-sm text-neutral-600">
                Page {page} of {totalPages}
              </span>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => goToPage(page + 1)}
                disabled={page >= totalPages}
              >
                Next
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

export default JobsPage