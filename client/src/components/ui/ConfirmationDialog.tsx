import * as AlertDialog from '@radix-ui/react-alert-dialog'
import type { ReactElement } from 'react'
import { Button } from './Button'

interface ConfirmationDialogProps {
  trigger: ReactElement
  title: string
  description: string
  confirmLabel: string
  variant?: 'default' | 'danger'
  pending?: boolean
  onConfirm: () => void | Promise<void>
}

export function ConfirmationDialog({
  trigger,
  title,
  description,
  confirmLabel,
  variant = 'default',
  pending = false,
  onConfirm,
}: ConfirmationDialogProps) {
  return (
    <AlertDialog.Root>
      <AlertDialog.Trigger asChild>{trigger}</AlertDialog.Trigger>
      <AlertDialog.Portal>
        <AlertDialog.Overlay className="fixed inset-0 z-40 bg-ink/55" />
        <AlertDialog.Content className="fixed left-1/2 top-1/2 z-50 w-[min(92vw,30rem)] -translate-x-1/2 -translate-y-1/2 rounded-lg border border-line bg-surface p-6 text-ink">
          <AlertDialog.Title className="text-xl font-semibold">{title}</AlertDialog.Title>
          <AlertDialog.Description className="mt-2 text-sm leading-6 text-muted">
            {description}
          </AlertDialog.Description>
          <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <AlertDialog.Cancel asChild disabled={pending}>
              <Button type="button" variant="secondary" disabled={pending}>Cancel</Button>
            </AlertDialog.Cancel>
            <AlertDialog.Action asChild disabled={pending}>
              <Button
                type="button"
                variant={variant === 'danger' ? 'destructive' : 'primary'}
                disabled={pending}
                aria-label={pending ? `${confirmLabel}, pending` : confirmLabel}
                onClick={() => void onConfirm()}
              >
                {pending ? 'Working…' : confirmLabel}
              </Button>
            </AlertDialog.Action>
          </div>
        </AlertDialog.Content>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  )
}
