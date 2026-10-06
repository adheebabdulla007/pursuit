import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './Button'

interface PaginationProps {
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}

function visiblePages(page: number, totalPages: number): Array<number | 'ellipsis'> {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, index) => index + 1)
  }

  const pageNumbers = new Set([1, totalPages, page - 1, page, page + 1])
  if (page <= 4) [2, 3, 4, 5].forEach((value) => pageNumbers.add(value))
  if (page >= totalPages - 3) {
    [totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1]
      .forEach((value) => pageNumbers.add(value))
  }

  const sorted = [...pageNumbers]
    .filter((value) => value >= 1 && value <= totalPages)
    .sort((left, right) => left - right)
  const result: Array<number | 'ellipsis'> = []

  sorted.forEach((value, index) => {
    if (index > 0 && value - sorted[index - 1] > 1) result.push('ellipsis')
    result.push(value)
  })

  return result
}

export function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null

  return (
    <nav aria-label="Pagination" className="flex flex-wrap items-center justify-center gap-2">
      <Button
        type="button"
        variant="secondary"
        size="sm"
        aria-label="Previous page"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        <ChevronLeft aria-hidden="true" size={16} />
        Previous
      </Button>
      {visiblePages(page, totalPages).map((item, index) => item === 'ellipsis' ? (
        <span key={`ellipsis-${index}`} aria-hidden="true" className="px-1 text-muted">…</span>
      ) : (
        <Button
          key={item}
          type="button"
          variant={item === page ? 'primary' : 'secondary'}
          size="sm"
          aria-label={`Page ${item}`}
          aria-current={item === page ? 'page' : undefined}
          onClick={() => onPageChange(item)}
        >
          {item}
        </Button>
      ))}
      <Button
        type="button"
        variant="secondary"
        size="sm"
        aria-label="Next page"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Next
        <ChevronRight aria-hidden="true" size={16} />
      </Button>
    </nav>
  )
}
