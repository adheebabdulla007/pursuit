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
    <div className="min-h-screen bg-neutral-50">
      {/* Hero */}
      <section className="bg-white border-b border-neutral-200">
        <div className="max-w-3xl mx-auto px-4 py-16 text-center">
          <div className="w-14 h-14 rounded-lg bg-primary-600 text-white flex items-center justify-center text-2xl font-bold mx-auto mb-6">
            P
          </div>
          <h1 className="text-3xl sm:text-4xl font-semibold text-neutral-900">
            Find your next role, or your next hire
          </h1>
          <p className="text-neutral-600 mt-3">
            Pursuit connects job seekers and employers on one platform.
          </p>

          <form
            onSubmit={handleSearch}
            className="mt-8 flex flex-col sm:flex-row gap-3 sm:items-end text-left"
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
            <Button type="submit">Search Jobs</Button>
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
      <section className="bg-primary-600">
        <div className="max-w-3xl mx-auto px-4 py-12 text-center">
          <h2 className="text-2xl font-semibold text-white">Hiring?</h2>
          <p className="text-primary-100 mt-2">
            Post a job and start receiving applications right away.
          </p>
          <Link to="/jobs/new">
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