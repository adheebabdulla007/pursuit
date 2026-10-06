import { useState } from 'react'
import { Button } from '../ui/Button'
import { Input } from '../ui/Input'
import type { CreateJobRequest, JobType } from '../../types/job'

const JOB_TYPES: Array<{ value: JobType; label: string }> = [
  { value: 'FullTime', label: 'Full time' },
  { value: 'PartTime', label: 'Part time' },
  { value: 'Contract', label: 'Contract' },
  { value: 'Internship', label: 'Internship' },
  { value: 'Remote', label: 'Remote' },
]

type JobFormValues = CreateJobRequest

interface JobFormProps {
  initialValues?: JobFormValues
  submitLabel: string
  pendingLabel: string
  isPending: boolean
  onSubmit: (values: JobFormValues) => void | Promise<void>
}

const emptyValues: JobFormValues = {
  title: '', description: '', location: '', salaryMin: 0, salaryMax: 0, jobType: 'FullTime',
}

export function JobForm({ initialValues = emptyValues, submitLabel, pendingLabel, isPending, onSubmit }: JobFormProps) {
  const [title, setTitle] = useState(initialValues.title)
  const [description, setDescription] = useState(initialValues.description)
  const [location, setLocation] = useState(initialValues.location)
  const [salaryMin, setSalaryMin] = useState(initialValues.salaryMin ? String(initialValues.salaryMin) : '')
  const [salaryMax, setSalaryMax] = useState(initialValues.salaryMax ? String(initialValues.salaryMax) : '')
  const [jobType, setJobType] = useState<JobType>(initialValues.jobType)
  const [errors, setErrors] = useState<Record<string, string>>({})

  function submit(event: React.FormEvent) {
    event.preventDefault()
    const nextErrors: Record<string, string> = {}
    if (!title.trim()) nextErrors.title = 'Job title is required.'
    if (!description.trim()) nextErrors.description = 'Description is required.'
    if (!location.trim()) nextErrors.location = 'Location is required.'
    if (salaryMin.trim() === '' || Number(salaryMin) < 0) nextErrors.salaryMin = 'Enter a valid minimum salary.'
    if (salaryMax.trim() === '' || Number(salaryMax) <= Number(salaryMin)) nextErrors.salaryMax = 'Maximum salary must be greater than minimum salary.'
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return
    void onSubmit({
      title: title.trim(), description: description.trim(), location: location.trim(),
      salaryMin: Number(salaryMin), salaryMax: Number(salaryMax), jobType,
    })
  }

  return (
    <form onSubmit={submit} className="space-y-5">
      <Input id="job-title" label="Job title" value={title} onChange={(e) => setTitle(e.target.value)} error={errors.title} />
      <div>
        <label htmlFor="job-description" className="mb-1.5 block text-sm font-semibold text-ink">Description</label>
        <textarea id="job-description" rows={8} value={description} onChange={(e) => setDescription(e.target.value)}
          aria-invalid={errors.description ? true : undefined}
          className="w-full resize-y rounded-md border border-line bg-surface px-3 py-3 text-base text-ink focus:outline-none focus:ring-2 focus:ring-info" />
        {errors.description && <p className="mt-1 text-sm text-danger">{errors.description}</p>}
      </div>
      <Input id="job-location" label="Location" value={location} onChange={(e) => setLocation(e.target.value)} error={errors.location} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Input id="salary-min" label="Minimum salary" type="number" min="0" value={salaryMin} onChange={(e) => setSalaryMin(e.target.value)} error={errors.salaryMin} />
        <Input id="salary-max" label="Maximum salary" type="number" min="0" value={salaryMax} onChange={(e) => setSalaryMax(e.target.value)} error={errors.salaryMax} />
      </div>
      <div>
        <label htmlFor="job-type" className="mb-1.5 block text-sm font-semibold text-ink">Work arrangement</label>
        <select id="job-type" value={jobType} onChange={(e) => setJobType(e.target.value as JobType)}
          className="h-12 w-full rounded-md border border-line bg-surface px-3 text-base text-ink focus:outline-none focus:ring-2 focus:ring-info">
          {JOB_TYPES.map((type) => <option key={type.value} value={type.value}>{type.label}</option>)}
        </select>
      </div>
      <Button type="submit" disabled={isPending}>{isPending ? pendingLabel : submitLabel}</Button>
    </form>
  )
}
