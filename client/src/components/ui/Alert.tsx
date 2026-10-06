import type { HTMLAttributes, ReactNode } from 'react'

type AlertVariant = 'info' | 'success' | 'warning' | 'danger'

interface AlertProps extends HTMLAttributes<HTMLDivElement> {
  variant: AlertVariant
  title?: string
  children: ReactNode
}

const variantStyles: Record<AlertVariant, string> = {
  info: 'border-info/25 bg-info-soft text-info',
  success: 'border-success/25 bg-success-soft text-success',
  warning: 'border-warning/25 bg-warning-soft text-warning',
  danger: 'border-danger/25 bg-danger-soft text-danger',
}

export function Alert({ variant, title, children, className = '', ...props }: AlertProps) {
  const role = variant === 'danger' ? 'alert' : 'status'

  return (
    <div
      role={role}
      aria-live={variant === 'danger' ? 'assertive' : 'polite'}
      className={`rounded-md border px-4 py-3 text-sm ${variantStyles[variant]} ${className}`}
      {...props}
    >
      {title && <p className="font-semibold">{title}</p>}
      <div className={title ? 'mt-1' : ''}>{children}</div>
    </div>
  )
}
