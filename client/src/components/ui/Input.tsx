import { forwardRef } from 'react'
import type { InputHTMLAttributes } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  id: string
  error?: string
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, id, error, className = '', ...props }, ref) => {
    const inputStyles = [
      'peer w-full rounded-md border px-3 pt-5 pb-2 text-base bg-white text-neutral-900',
      'focus:outline-none focus:ring-2',
      error
        ? 'border-red-500 focus:ring-red-500'
        : 'border-neutral-300 focus:ring-primary-500',
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
          <input ref={ref} id={id} placeholder=" " className={inputStyles} {...props} />
          <label htmlFor={id} className={labelStyles}>
            {label}
          </label>
        </div>
        {error && <p className="mt-1 text-sm text-red-600">{error}</p>}
      </div>
    )
  }
)

Input.displayName = 'Input'