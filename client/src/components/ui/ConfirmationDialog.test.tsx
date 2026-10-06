import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ConfirmationDialog } from './ConfirmationDialog'

describe('ConfirmationDialog', () => {
  it('contains focus, closes on Escape, and restores focus to its trigger', async () => {
    const user = userEvent.setup()
    render(
      <ConfirmationDialog
        trigger={<button>Close job</button>}
        title="Close this job?"
        description="Candidates can no longer apply."
        confirmLabel="Close job"
        variant="danger"
        onConfirm={vi.fn()}
      />,
    )

    const trigger = screen.getByRole('button', { name: 'Close job' })
    await user.click(trigger)
    const dialog = screen.getByRole('alertdialog', { name: 'Close this job?' })
    expect(dialog).toBeInTheDocument()
    expect(screen.getByText('Candidates can no longer apply.')).toBeInTheDocument()

    await user.tab()
    expect(dialog).toContainElement(document.activeElement as HTMLElement)
    await user.keyboard('{Escape}')
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
    expect(trigger).toHaveFocus()
  })

  it('disables actions and exposes progress while confirmation is pending', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(
      <ConfirmationDialog
        trigger={<button>Deactivate</button>}
        title="Deactivate account?"
        description="The user will lose access."
        confirmLabel="Deactivate"
        variant="danger"
        pending
        onConfirm={onConfirm}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Deactivate' }))
    expect(screen.getByRole('button', { name: 'Deactivate, pending' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    expect(onConfirm).not.toHaveBeenCalled()
  })
})
