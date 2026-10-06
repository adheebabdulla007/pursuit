import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useParams, useSearchParams } from 'react-router-dom'
import { fetchJobs } from '../api/jobs'
import { JobFilters } from '../components/jobs/JobFilters'
import { JobResultsList } from '../components/jobs/JobResultsList'
import { JobSearchBar } from '../components/jobs/JobSearchBar'
import { Card } from '../components/ui/Card'
import JobDetailPage from './JobDetailPage'
import type { JobType } from '../types/job'

const PAGE_SIZE = 10

function JobsPage() {
  const { id: selectedId } = useParams<{ id: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page')) || 1)
  const keyword = searchParams.get('keyword') ?? ''
  const location = searchParams.get('location') ?? ''
  const jobType = (searchParams.get('jobType') as JobType | null) ?? ''
  const { data, isLoading, error } = useQuery({
    queryKey: ['jobs', page, keyword, location, jobType],
    queryFn: () => fetchJobs({
      page,
      pageSize: PAGE_SIZE,
      keyword: keyword || undefined,
      location: location || undefined,
      jobType: jobType || undefined,
    }),
  })

  function updateSearchParams(updates: Record<string, string | undefined>) {
    const next = new URLSearchParams(searchParams)
    Object.entries(updates).forEach(([key, value]) => {
      if (value) next.set(key, value)
      else next.delete(key)
    })
    setSearchParams(next)
  }

  function submitSearch(nextKeyword: string, nextLocation: string) {
    updateSearchParams({
      keyword: nextKeyword.trim() || undefined,
      location: nextLocation.trim() || undefined,
      page: '1',
    })
  }

  function removeFilter(filter: 'keyword' | 'location' | 'jobType') {
    updateSearchParams({ [filter]: undefined, page: '1' })
  }

  function clearFilters() {
    updateSearchParams({ keyword: undefined, location: undefined, jobType: undefined, page: '1' })
  }

  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const preservedSearch = searchParams.toString() ? `?${searchParams.toString()}` : ''

  useEffect(() => {
    if (selectedId) return
    const storageKey = `pursuit:jobs-scroll:${preservedSearch}`
    const savedPosition = sessionStorage.getItem(storageKey)
    const savedOffset = Number(savedPosition)
    if (savedOffset > 0) {
      requestAnimationFrame(() => window.scrollTo({ top: savedOffset, behavior: 'auto' }))
    }

    return () => {
      sessionStorage.setItem(storageKey, String(window.scrollY))
    }
  }, [preservedSearch, selectedId])

  return (
    <div className="min-h-screen bg-canvas px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-[90rem]">
        <header className="mb-5">
          <p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Career Desk</p>
          <h1 className="mt-1 text-3xl font-extrabold tracking-[-0.03em] text-ink">Find work worth pursuing</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">
            Search active roles, compare the details, and keep your application trail in one place.
          </p>
        </header>

        <Card className="mb-5" padding="sm">
          <JobSearchBar
            key={`${keyword}\u0000${location}`}
            initialKeyword={keyword}
            initialLocation={location}
            onSubmit={submitSearch}
          />
          <JobFilters
            keyword={keyword}
            location={location}
            jobType={jobType}
            onJobTypeChange={(value) => updateSearchParams({ jobType: value || undefined, page: '1' })}
            onRemove={removeFilter}
            onClear={clearFilters}
          />
        </Card>

        <div className="lg:grid lg:grid-cols-[minmax(340px,400px)_minmax(0,1fr)] lg:items-start lg:gap-5">
          <section aria-label="Job search results" className={selectedId ? 'hidden lg:block' : ''}>
            <JobResultsList
              jobs={data?.items ?? []}
              totalCount={totalCount}
              page={page}
              totalPages={totalPages}
              selectedId={selectedId}
              search={preservedSearch}
              isLoading={isLoading}
              error={error instanceof Error ? error : null}
              onPageChange={(nextPage) => updateSearchParams({ page: String(nextPage) })}
            />
          </section>

          <section aria-label="Selected job" className={selectedId ? '' : 'hidden lg:block'}>
            {selectedId ? (
              <JobDetailPage embedded />
            ) : (
              <div className="border border-dashed border-line bg-surface px-8 py-16 text-center">
                <h2 className="text-lg font-bold text-ink">Select a role to inspect the details</h2>
                <p className="mt-2 text-sm text-muted">Your search and filters remain in place while you compare roles.</p>
              </div>
            )}
          </section>
        </div>
      </div>
    </div>
  )
}

export default JobsPage
