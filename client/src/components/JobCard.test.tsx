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
  }
  return render(
    <MemoryRouter>
      <JobCard {...defaultProps} {...props} />
    </MemoryRouter>
  )
}

describe('JobCard', () => {
  it('renders title, company name, and location', () => {
    renderJobCard()

    expect(screen.getByRole('heading', { name: 'Senior .NET Developer' })).toBeInTheDocument()
    expect(screen.getByText('Acme Corp · Riyadh, Saudi Arabia')).toBeInTheDocument()
  })

  it('links to the correct job detail page', () => {
    renderJobCard({ id: 'job-456' })

    expect(screen.getByRole('link')).toHaveAttribute('href', '/jobs/job-456')
  })
})