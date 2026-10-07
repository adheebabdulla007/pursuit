import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import JobCard from './JobCard'

function renderJobCard(props: Partial<Parameters<typeof JobCard>[0]> = {}) {
  const defaultProps = {
    id: 'job-123',
    title: 'Senior .NET Developer',
    companyName: 'Acme Corp',
    location: 'Riyadh, Saudi Arabia',
    jobType: 'FullTime',
    salaryMin: 50000,
    salaryMax: 80000,
    createdAt: '2026-10-01T00:00:00Z',
  }
  return render(
    <MemoryRouter>
      <JobCard {...defaultProps} {...props} />
    </MemoryRouter>
  )
}

describe('JobCard', () => {
  it('renders the verified job metadata', () => {
    renderJobCard()

    expect(screen.getByRole('heading', { name: 'Senior .NET Developer' })).toBeInTheDocument()
    expect(screen.getByText('Acme Corp')).toBeInTheDocument()
    expect(screen.getByText(/Riyadh, Saudi Arabia/)).toBeInTheDocument()
    expect(screen.getByText(/Full Time/)).toBeInTheDocument()
    expect(screen.getByText(/50,000 – 80,000/)).toBeInTheDocument()
    expect(screen.getByText(/Posted Oct 1, 2026/)).toBeInTheDocument()
  })

  it('links to the correct job detail page', () => {
    renderJobCard({ id: 'job-456', search: '?keyword=dotnet' })

    expect(screen.getByRole('link')).toHaveAttribute('href', '/jobs/job-456?keyword=dotnet')
  })

  it('marks the selected result without relying on color alone', () => {
    renderJobCard({ selected: true })

    expect(screen.getByRole('link')).toHaveAttribute('aria-current', 'page')
    expect(screen.getByText('Selected')).toBeInTheDocument()
  })
})
