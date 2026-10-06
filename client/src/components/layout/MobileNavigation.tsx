import * as Dialog from '@radix-ui/react-dialog'
import { Menu, X } from 'lucide-react'
import { useState } from 'react'
import type { CurrentUser } from '../../types/auth'
import { Button } from '../ui/Button'
import { RoleNavigation } from './RoleNavigation'
export function MobileNavigation({ user, onLogout }: { user: CurrentUser | null; onLogout: () => Promise<void> }) {
  const [open, setOpen] = useState(false)
  return <Dialog.Root open={open} onOpenChange={setOpen}><Dialog.Trigger asChild><Button type="button" variant="ghost" aria-label="Open navigation"><Menu /></Button></Dialog.Trigger><Dialog.Portal><Dialog.Overlay className="fixed inset-0 z-40 bg-ink/55" /><Dialog.Content className="fixed right-0 top-0 z-50 flex h-full w-[min(84vw,22rem)] flex-col bg-surface p-5"><div className="flex items-center justify-between"><Dialog.Title className="font-extrabold text-ink">Navigation</Dialog.Title><Dialog.Close asChild><Button variant="ghost" aria-label="Close navigation"><X /></Button></Dialog.Close></div><nav className="mt-8 flex flex-col gap-2"><RoleNavigation user={user} onNavigate={() => setOpen(false)} /></nav>{user && <><p className="mt-auto border-t border-line pt-5 text-sm text-muted">{user.email}<br />{user.role}</p><Button className="mt-3" variant="secondary" onClick={() => void onLogout()}>Log out</Button></>}</Dialog.Content></Dialog.Portal></Dialog.Root>
}
