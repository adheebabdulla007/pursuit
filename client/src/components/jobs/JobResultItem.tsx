import type { Job } from '../../types/job'
import JobCard from '../JobCard'

interface JobResultItemProps {
  job: Job
  selected: boolean
  search: string
}

export function JobResultItem({ job, selected, search }: JobResultItemProps) {
  return (
    <li>
      <JobCard
        id={job.id}
        title={job.title}
        companyName={job.companyName}
        location={job.location}
        jobType={job.jobType}
        salaryMin={job.salaryMin}
        salaryMax={job.salaryMax}
        createdAt={job.createdAt}
        selected={selected}
        search={search}
      />
    </li>
  )
}
