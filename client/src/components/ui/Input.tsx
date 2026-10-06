import { forwardRef } from 'react'
import type { InputHTMLAttributes } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  id: string
  error?: string
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, id, error, className = '', 'aria-describedby': ariaDescribedBy, ...props }, ref) => {
    const errorId = `${id}-error`
    const inputStyles = [
      'peer w-full rounded-md border bg-surface px-3 pt-5 pb-2 text-base text-ink',
      'focus:outline-none focus:ring-2 focus:ring-offset-1',
      error
        ? 'border-danger focus:ring-danger'
        : 'border-line focus:ring-info',
      className,
    ]
      .filter(Boolean)
      .join(' ')

    const labelStyles = [
      'absolute left-3 text-neutral-500 transition-all pointer-events-none',
      'peer-placeholder-shown:top-3.5 peer-placeholder-shown:text-base',
      'peer-focus:top-2 peer-focus:text-xs peer-focus:text-primary-600',
      'top-2 text-xs',
    ].join(' ')

    return (
      <div className="w-full">
        <div className="relative">
          <input
            ref={ref}
            id={id}
            placeholder=" "
            className={inputStyles}
            aria-invalid={error ? true : undefined}
            aria-describedby={[ariaDescribedBy, error ? errorId : undefined].filter(Boolean).join(' ') || undefined}
            {...props}
          />
          <label htmlFor={id} className={labelStyles}>
            {label}
          </label>
        </div>
        {error && <p id={errorId} className="mt-1 text-sm text-danger">{error}</p>}
      </div>
    )
  }
)

Input.displayName = 'Input'
