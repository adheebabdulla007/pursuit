import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchJobs } from '../api/jobs'
import JobCard from '../components/JobCard'
import { Input } from '../components/ui/Input'
import { Button } from '../components/ui/Button'

function HomePage() {
  const navigate = useNavigate()
  const [keyword, setKeyword] = useState('')
  const [location, setLocation] = useState('')

  const { data: statsData } = useQuery({
    queryKey: ['jobs', 'stats'],
    queryFn: () => fetchJobs({ pageSize: 1 }),
  })

  const { data: recentData } = useQuery({
    queryKey: ['jobs', 'recent'],
    queryFn: () => fetchJobs({ pageSize: 20 }),
  })

  const recentJobs = recentData?.items
    ? [...recentData.items]
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
        .slice(0, 5)
    : []

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    const params = new URLSearchParams()
    if (keyword) params.set('keyword', keyword)
    if (location) params.set('location', location)
    navigate(`/jobs?${params.toString()}`)
  }

  return (
    <div className="min-h-screen bg-canvas">
      {/* Hero */}
      <section className="border-b border-line bg-surface">
        <div className="mx-auto max-w-5xl px-4 py-16 sm:py-20">
          <p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Career desk</p>
          <h1 className="mt-2 max-w-3xl text-4xl font-extrabold tracking-[-0.04em] text-ink sm:text-5xl">
            Make the next career move with the facts in view.
          </h1>
          <p className="mt-4 max-w-2xl text-lg leading-7 text-muted">
            Search active roles, inspect the full brief, and keep every application in one working record.
          </p>

          <form
            onSubmit={handleSearch}
            className="mt-8 flex flex-col gap-3 text-left sm:flex-row sm:items-end"
          >
            <div className="flex-1">
              <Input
                id="home-keyword"
                label="Keyword"
                type="text"
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
              />
            </div>
            <div className="flex-1">
              <Input
                id="home-location"
                label="Location"
                type="text"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
              />
            </div>
            <Button type="submit">Search jobs</Button>
          </form>

          {statsData && (
            <p className="text-sm text-neutral-500 mt-4">
              {statsData.totalCount.toLocaleString()} jobs currently listed
            </p>
          )}
        </div>
      </section>

      {/* Recent Jobs */}
      {recentJobs.length > 0 && (
        <section className="max-w-3xl mx-auto px-4 py-12">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-neutral-900">Recent Jobs</h2>
            <Link to="/jobs" className="text-sm text-primary-600 hover:underline">
              View all jobs
            </Link>
          </div>
          <div className="flex flex-col gap-4">
            {recentJobs.map((job) => (
              <JobCard
                key={job.id}
                id={job.id}
                title={job.title}
                companyName={job.companyName}
                location={job.location}
              />
            ))}
          </div>
        </section>
      )}

      {/* Employer CTA */}
      <section className="border-t border-line bg-ink">
        <div className="max-w-3xl mx-auto px-4 py-12 text-center">
          <h2 className="text-2xl font-extrabold text-white">Hiring with a clear brief?</h2>
          <p className="mt-2 text-neutral-300">
            Publish the role, manage its status, and review candidates from one desk.
          </p>
          <Link to="/employer/jobs/new">
            <Button variant="secondary" size="lg" className="mt-6">
              Post a Job
            </Button>
          </Link>
        </div>
      </section>
    </div>
  )
}

export default HomePage
