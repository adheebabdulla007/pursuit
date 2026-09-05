import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createJob } from '../api/jobs'
import { Card } from '../components/ui/Card'
import { Input } from '../components/ui/Input'
import { Button } from '../components/ui/Button'
import type { JobType } from '../types/job'

const JOB_TYPES: JobType[] = ['FullTime', 'PartTime', 'Contract', 'Internship', 'Remote']

function CreateJobPage() {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [location, setLocation] = useState('')
  const [salaryMin, setSalaryMin] = useState('')
  const [salaryMax, setSalaryMax] = useState('')
  const [jobType, setJobType] = useState<JobType>('FullTime')
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const navigate = useNavigate()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setIsLoading(true)

    try {
      const job = await createJob({
        title,
        description,
        location,
        salaryMin: Number(salaryMin),
        salaryMax: Number(salaryMax),
        jobType,
      })
      navigate(`/jobs/${job.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create job.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-neutral-50 px-4 py-8">
      <div className="max-w-xl mx-auto">
        <h1 className="text-2xl font-semibold text-neutral-900 mb-6">Post a Job</h1>
        <Card>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <Input
              id="title"
              label="Title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
            />

            <div className="flex flex-col gap-1">
              <label htmlFor="description" className="text-sm font-medium text-neutral-700">
                Description
              </label>
              <textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={6}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-base bg-white text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 resize-y"
              />
            </div>

            <Input
              id="location"
              label="Location"
              type="text"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
            />

            <div className="flex gap-4">
              <div className="flex-1">
                <Input
                  id="salaryMin"
                  label="Salary Min"
                  type="number"
                  value={salaryMin}
                  onChange={(e) => setSalaryMin(e.target.value)}
                />
              </div>
              <div className="flex-1">
                <Input
                  id="salaryMax"
                  label="Salary Max"
                  type="number"
                  value={salaryMax}
                  onChange={(e) => setSalaryMax(e.target.value)}
                />
              </div>
            </div>

            <div className="flex flex-col gap-1">
              <label htmlFor="jobType" className="text-sm font-medium text-neutral-700">
                Job Type
              </label>
              <select
                id="jobType"
                value={jobType}
                onChange={(e) => setJobType(e.target.value as JobType)}
                className="w-full rounded-md border border-neutral-300 px-3 py-2 text-base bg-white text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                {JOB_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>

            {error && (
              <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
                {error}
              </p>
            )}

            <Button type="submit" disabled={isLoading} className="w-full">
              {isLoading ? 'Posting...' : 'Post Job'}
            </Button>
          </form>
        </Card>
      </div>
    </div>
  )
}

export default CreateJobPage