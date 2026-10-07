import { useEffect, useRef, type ReactNode } from 'react'
import { useLocation } from 'react-router-dom'
import Navbar from '../Navbar'
export function AppShell({ children }: { children: ReactNode }) {
  const location = useLocation(); const main = useRef<HTMLElement>(null)
  const previousPath = useRef(location.pathname)
  useEffect(() => {
    if (previousPath.current !== location.pathname) main.current?.focus()
    previousPath.current = location.pathname
  }, [location.pathname])
  return <><a href="#main-content" className="fixed left-3 top-3 z-[60] -translate-y-20 rounded bg-ink px-4 py-2 text-white focus:translate-y-0">Skip to main content</a><Navbar /><main id="main-content" ref={main} tabIndex={-1} className="outline-none">{children}</main></>
}
