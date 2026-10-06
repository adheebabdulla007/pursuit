import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Alert } from './Alert'
import { EmptyState } from './EmptyState'
import { Pagination } from './Pagination'
import { Skeleton } from './Skeleton'
import { StatusBadge } from './StatusBadge'

describe('feedback primitives', () => {
  it('announces informational and dangerous alerts through appropriate live regions', () => {
    const { rerender } = render(<Alert variant="info" title="Application update">Status changed.</Alert>)
    expect(screen.getByRole('status')).toHaveAttribute('aria-live', 'polite')
    expect(screen.getByText('Application update')).toBeInTheDocument()

    rerender(<Alert variant="danger">Update failed.</Alert>)
    expect(screen.getByRole('alert')).toHaveTextContent('Update failed.')
  })

  it('renders status as text rather than a color-only marker', () => {
    render(<StatusBadge status="Interview" />)
    expect(screen.getByText('Interview')).toHaveAttribute('data-status', 'interview')
  })

  it('disables pagination edges and reports the current page', async () => {
    const user = userEvent.setup()
    const onPageChange = vi.fn()
    const { rerender } = render(
      <Pagination page={1} totalPages={3} onPageChange={onPageChange} />,
    )

    expect(screen.getByRole('navigation', { name: 'Pagination' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Previous page' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Page 1' })).toHaveAttribute('aria-current', 'page')
    await user.click(screen.getByRole('button', { name: 'Next page' }))
    expect(onPageChange).toHaveBeenCalledWith(2)

    rerender(<Pagination page={3} totalPages={3} onPageChange={onPageChange} />)
    expect(screen.getByRole('button', { name: 'Next page' })).toBeDisabled()
  })

  it('keeps large result sets compact instead of rendering every page button', () => {
    render(<Pagination page={25} totalPages={50} onPageChange={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Page 1' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Page 25' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('button', { name: 'Page 50' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Page 2' })).not.toBeInTheDocument()
  })

  it('provides an action-oriented empty state and a non-announced loading skeleton', () => {
    render(
      <>
        <EmptyState title="No jobs yet" description="Publish the first role." action={<button>Post a job</button>} />
        <Skeleton data-testid="loading-jobs" className="h-8" />
      </>,
    )

    expect(screen.getByRole('heading', { name: 'No jobs yet' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Post a job' })).toBeInTheDocument()
    expect(screen.getByTestId('loading-jobs')).toHaveAttribute('aria-hidden', 'true')
    expect(screen.getByTestId('loading-jobs')).toHaveClass('h-8')
  })
})
