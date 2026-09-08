import { describe, it, expect} from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Input } from './Input'

describe('Input', () => {
  it('renders with an accessible label linked via htmlFor/id', () => {
    render(<Input id="email" label="Email" />)
    expect(screen.getByLabelText('Email')).toBeInTheDocument()
  })

  it('always sets a single-space placeholder for the floating-label CSS technique', () => {
    render(<Input id="email" label="Email" />)
    expect(screen.getByLabelText('Email')).toHaveAttribute('placeholder', ' ')
  })

  it('shows the error message and applies error border styling when error is set', () => {
    render(<Input id="email" label="Email" error="Email is required" />)
    expect(screen.getByText('Email is required')).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toHaveClass('border-red-500')
  })

  it('does not render an error message when error is not set', () => {
    render(<Input id="email" label="Email" />)
    expect(screen.queryByText(/required/i)).not.toBeInTheDocument()
  })

  it('forwards ref to the underlying input element', () => {
    const ref = { current: null as HTMLInputElement | null }
    render(<Input id="email" label="Email" ref={ref} />)
    expect(ref.current).toBeInstanceOf(HTMLInputElement)
  })

  it('accepts typed input and reflects the value', async () => {
    const user = userEvent.setup()
    render(<Input id="email" label="Email" />)
    const input = screen.getByLabelText('Email')
    await user.type(input, 'test@pursuit.local')
    expect(input).toHaveValue('test@pursuit.local')
  })

  it('merges a custom className with the base input classes', () => {
    render(<Input id="email" label="Email" className="my-custom-class" />)
    const input = screen.getByLabelText('Email')
    expect(input).toHaveClass('my-custom-class')
    expect(input).toHaveClass('peer')
  })
})