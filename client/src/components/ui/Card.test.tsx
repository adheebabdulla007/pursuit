import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Card } from './Card'

describe('Card', () => {
  it('renders its children', () => {
    render(<Card>Card content</Card>)
    expect(screen.getByText('Card content')).toBeInTheDocument()
  })

  it('applies the md padding class by default', () => {
    render(<Card data-testid="card">Content</Card>)
    expect(screen.getByTestId('card')).toHaveClass('p-6')
  })

  it('applies no padding class when padding is none', () => {
    render(<Card data-testid="card" padding="none">Content</Card>)
    const card = screen.getByTestId('card')
    expect(card).not.toHaveClass('p-6')
    expect(card).not.toHaveClass('p-4')
  })

  it('merges a custom className with the base classes', () => {
    render(<Card data-testid="card" className="my-custom-class">Content</Card>)
    const card = screen.getByTestId('card')
    expect(card).toHaveClass('my-custom-class')
    expect(card).toHaveClass('bg-white')
  })

  it('forwards ref to the underlying div element', () => {
    const ref = { current: null as HTMLDivElement | null }
    render(<Card ref={ref}>Content</Card>)
    expect(ref.current).toBeInstanceOf(HTMLDivElement)
  })
})