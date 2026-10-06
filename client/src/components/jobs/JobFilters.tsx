import * as Dialog from '@radix-ui/react-dialog'
import { SlidersHorizontal, X } from 'lucide-react'
import type { JobType } from '../../types/job'
import { Button } from '../ui/Button'

const JOB_TYPES: JobType[] = ['FullTime', 'PartTime', 'Contract', 'Internship', 'Remote']

const JOB_TYPE_LABELS: Record<JobType, string> = {
  FullTime: 'Full time',
  PartTime: 'Part time',
  Contract: 'Contract',
  Internship: 'Internship',
  Remote: 'Remote',
}

interface JobFiltersProps {
  keyword: string
  location: string
  jobType: JobType | ''
  onJobTypeChange: (value: JobType | '') => void
  onRemove: (filter: 'keyword' | 'location' | 'jobType') => void
  onClear: () => void
}

export function JobFilters({
  keyword,
  location,
  jobType,
  onJobTypeChange,
  onRemove,
  onClear,
}: JobFiltersProps) {
  const activeFilters = [
    keyword ? { key: 'keyword' as const, label: 'Keyword', value: keyword } : null,
    location ? { key: 'location' as const, label: 'Location', value: location } : null,
    jobType ? { key: 'jobType' as const, label: 'Type', value: JOB_TYPE_LABELS[jobType] } : null,
  ].filter(Boolean) as Array<{ key: 'keyword' | 'location' | 'jobType'; label: string; value: string }>

  const jobTypeOptions = JOB_TYPES.map((type) => (
    <option key={type} value={type}>{JOB_TYPE_LABELS[type]}</option>
  ))

  return (
    <div className="mt-4 flex flex-col gap-3 border-t border-line pt-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="hidden flex-wrap items-center gap-2 sm:flex">
        <label htmlFor="jobType" className="text-sm font-semibold text-ink">Job Type</label>
        <select
          id="jobType"
          value={jobType}
          onChange={(event) => onJobTypeChange(event.target.value as JobType | '')}
          className="h-10 rounded-md border border-line bg-surface px-3 text-sm text-ink focus:ring-2 focus:ring-info"
        >
          <option value="">All job types</option>
          {jobTypeOptions}
        </select>
      </div>

      <Dialog.Root>
        <Dialog.Trigger asChild>
          <Button type="button" variant="secondary" size="sm" className="sm:hidden">
            <SlidersHorizontal aria-hidden="true" size={16} /> Filters
          </Button>
        </Dialog.Trigger>
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 z-40 bg-ink/55" />
          <Dialog.Content className="fixed inset-x-3 bottom-3 z-50 rounded-lg border border-line bg-surface p-5 text-ink sm:hidden">
            <Dialog.Title className="text-lg font-bold">Filter jobs</Dialog.Title>
            <Dialog.Description className="mt-1 text-sm text-muted">
              Narrow the current search without losing your keyword or location.
            </Dialog.Description>
            <label htmlFor="jobType-mobile" className="mt-5 block text-sm font-semibold">Job Type</label>
            <select
              id="jobType-mobile"
              value={jobType}
              onChange={(event) => onJobTypeChange(event.target.value as JobType | '')}
              className="mt-2 h-11 w-full rounded-md border border-line bg-surface px-3 text-ink focus:ring-2 focus:ring-info"
            >
              <option value="">All job types</option>
              {jobTypeOptions}
            </select>
            <div className="mt-5 flex justify-end">
              <Dialog.Close asChild><Button type="button">Done</Button></Dialog.Close>
            </div>
            <Dialog.Close asChild>
              <button type="button" aria-label="Close filters" className="absolute right-4 top-4 rounded p-1 text-muted hover:text-ink">
                <X aria-hidden="true" size={18} />
              </button>
            </Dialog.Close>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>

      {activeFilters.length > 0 && (
        <div aria-label="Active filters" className="flex flex-wrap items-center gap-2">
          {activeFilters.map((filter) => (
            <button
              key={filter.key}
              type="button"
              aria-label={`Remove ${filter.label} filter`}
              onClick={() => onRemove(filter.key)}
              className="inline-flex items-center gap-1 rounded border border-line bg-surface px-2.5 py-1.5 text-xs font-semibold text-ink hover:border-action"
            >
              <span className="text-muted">{filter.label}:</span> {filter.value}
              <X aria-hidden="true" size={13} />
            </button>
          ))}
          <Button type="button" variant="ghost" size="sm" onClick={onClear}>Clear all</Button>
        </div>
      )}
    </div>
  )
}
