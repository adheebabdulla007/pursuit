interface StatusBadgeProps {
  status: string
  className?: string
}

const statusStyles: Record<string, string> = {
  applied: 'bg-info-soft text-info',
  reviewing: 'bg-warning-soft text-warning',
  interview: 'bg-warning-soft text-warning',
  offered: 'bg-success-soft text-success',
  hired: 'bg-success-soft text-success',
  accepted: 'bg-success-soft text-success',
  rejected: 'bg-danger-soft text-danger',
  closed: 'bg-danger-soft text-danger',
  active: 'bg-success-soft text-success',
  open: 'bg-success-soft text-success',
}

export function StatusBadge({ status, className = '' }: StatusBadgeProps) {
  const token = status.trim().toLowerCase().replace(/\s+/g, '-')
  const style = statusStyles[token] ?? 'bg-neutral-200 text-ink'

  return (
    <span
      data-status={token}
      className={`inline-flex items-center rounded px-2 py-1 text-xs font-semibold ${style} ${className}`}
    >
      {status}
    </span>
  )
}
