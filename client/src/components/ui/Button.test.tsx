import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from './Button'

describe('Button', () => {
  it('renders its children', () => {
    render(<Button>Click me</Button>)
    expect(screen.getByRole('button', { name: 'Click me' })).toBeInTheDocument()
  })

  it('applies the primary variant by default', () => {
    render(<Button>Default</Button>)
    expect(screen.getByRole('button')).toHaveClass('bg-primary-600')
  })

  it('applies the destructive variant when specified', () => {
    render(<Button variant="destructive">Delete</Button>)
    expect(screen.getByRole('button')).toHaveClass('bg-red-600')
  })

  it('merges a custom className with the base and variant classes', () => {
    render(<Button className="my-custom-class">Custom</Button>)
    const button = screen.getByRole('button')
    expect(button).toHaveClass('my-custom-class')
    expect(button).toHaveClass('bg-primary-600')
  })

  it('forwards ref to the underlying button element', () => {
    const ref = { current: null as HTMLButtonElement | null }
    render(<Button ref={ref}>Ref test</Button>)
    expect(ref.current).toBeInstanceOf(HTMLButtonElement)
  })

  it('calls onClick when clicked, and not when disabled', async () => {
    const user = userEvent.setup()
    const handleClick = vi.fn()

    const { rerender } = render(<Button onClick={handleClick}>Enabled</Button>)
    await user.click(screen.getByRole('button'))
    expect(handleClick).toHaveBeenCalledTimes(1)

    rerender(<Button onClick={handleClick} disabled>Disabled</Button>)
    await user.click(screen.getByRole('button'))
    expect(handleClick).toHaveBeenCalledTimes(1)
  })
})