import { forwardRef } from 'react'
import type { ButtonHTMLAttributes } from 'react'

type ButtonVariant = 'primary' | 'secondary' | 'destructive' | 'ghost'
type ButtonSize = 'sm' | 'md' | 'lg'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
}

const baseStyles =
  'inline-flex items-center justify-center rounded-md font-medium transition-colors ' +
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-primary-500 ' +
  'disabled:opacity-50 disabled:cursor-not-allowed disabled:pointer-events-none'

const variantStyles: Record<ButtonVariant, string> = {
  primary: 'bg-primary-600 text-white hover:bg-primary-700',
  secondary: 'bg-neutral-100 text-neutral-900 hover:bg-neutral-200',
  destructive: 'bg-red-600 text-white hover:bg-red-700',
  ghost: 'bg-transparent text-neutral-700 hover:bg-neutral-100',
}

const sizeStyles: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-sm',
  md: 'h-10 px-4 text-base',
  lg: 'h-12 px-6 text-lg',
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant = 'primary', size = 'md', className = '', ...props }, ref) => {
    const combinedClassName = [
      baseStyles,
      variantStyles[variant],
      sizeStyles[size],
      className,
    ]
      .filter(Boolean)
      .join(' ')

    return <button ref={ref} className={combinedClassName} {...props} />
  }
)

Button.displayName = 'Button'