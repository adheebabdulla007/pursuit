import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchUsers, fetchStats, updateUserStatus } from '../api/admin'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { StatusBadge } from '../components/ui/StatusBadge'
import { ConfirmationDialog } from '../components/ui/ConfirmationDialog'
import type { AdminUser } from '../types/admin'

const PAGE_SIZE = 10

function AccountStatusAction({ user, pending, onToggle }: { user: AdminUser; pending: boolean; onToggle: () => Promise<void> }) {
  const button = (
    <Button
      className="mt-4 md:mt-0"
      variant={user.isActive ? 'destructive' : 'secondary'}
      size="sm"
      onClick={user.isActive ? undefined : () => void onToggle()}
      disabled={pending}
    >
      {pending ? 'Updating...' : user.isActive ? 'Deactivate' : 'Activate'}
    </Button>
  )

  if (!user.isActive) return button

  return (
    <ConfirmationDialog
      trigger={button}
      title={`Deactivate ${user.firstName} ${user.lastName}?`}
      description="This account will lose access until an administrator activates it again."
      confirmLabel="Confirm deactivation"
      variant="danger"
      pending={pending}
      onConfirm={onToggle}
    />
  )
}

function AdminPage() {
  const [page, setPage] = useState(1)
  const [toggleError, setToggleError] = useState('')
  const [pendingUserId, setPendingUserId] = useState<string | null>(null)
  const queryClient = useQueryClient()

  const statsQuery = useQuery({
    queryKey: ['admin', 'stats'],
    queryFn: fetchStats,
  })

  const usersQuery = useQuery({
    queryKey: ['admin', 'users', page],
    queryFn: () => fetchUsers(page, PAGE_SIZE),
  })

  async function handleToggleStatus(userId: string, currentIsActive: boolean) {
    setToggleError('')
    setPendingUserId(userId)
    try {
      await updateUserStatus(userId, !currentIsActive)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    } catch (err) {
      setToggleError(err instanceof Error ? err.message : 'Failed to update user status.')
    } finally {
      setPendingUserId(null)
    }
  }

  const totalPages = usersQuery.data ? Math.ceil(usersQuery.data.totalCount / PAGE_SIZE) : 1

  const statsItems = statsQuery.data
    ? [
        { label: 'Total Users', value: statsQuery.data.totalUsers },
        { label: 'Employers', value: statsQuery.data.totalEmployers },
        { label: 'Job Seekers', value: statsQuery.data.totalJobSeekers },
        { label: 'Total Jobs', value: statsQuery.data.totalJobs },
        { label: 'Total Applications', value: statsQuery.data.totalApplications },
      ]
    : []

  return (
    <div className="min-h-screen bg-canvas px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl">
        <p className="text-sm font-bold uppercase tracking-[0.16em] text-action">Operations</p><h1 className="mb-6 mt-1 text-3xl font-extrabold text-ink">Admin desk</h1>

        <section className="mb-8">
          <h2 className="text-lg font-semibold text-neutral-900 mb-3">Stats</h2>
          {statsQuery.isLoading && <p className="text-neutral-600">Loading stats...</p>}
          {statsQuery.isError && (
            <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
              Error loading stats: {statsQuery.error.message}
            </p>
          )}
          {statsQuery.data && (
            <div className="grid grid-cols-2 divide-x divide-y divide-line overflow-hidden rounded-lg border border-line bg-surface sm:grid-cols-3 lg:grid-cols-5">
              {statsItems.map((item) => (
                <div key={item.label} className="p-4">
                  <p className="text-2xl font-semibold text-neutral-900">{item.value}</p>
                  <p className="text-sm text-neutral-600 mt-1">{item.label}</p>
                </div>
              ))}
            </div>
          )}
        </section>

        <section>
          <h2 className="text-lg font-semibold text-neutral-900 mb-3">Users</h2>
          {toggleError && (
            <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm mb-4">
              {toggleError}
            </p>
          )}
          {usersQuery.isLoading && <p className="text-neutral-600">Loading users...</p>}
          {usersQuery.isError && (
            <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
              Error loading users: {usersQuery.error.message}
            </p>
          )}
          {usersQuery.data && (
            <>
              <div className="space-y-3 md:hidden">
                {usersQuery.data.items.map((u) => <Card key={u.id} padding="sm"><div className="flex items-start justify-between gap-3"><div><h3 className="font-bold text-ink">{u.firstName} {u.lastName}</h3><p className="text-sm text-muted">{u.email}</p></div><StatusBadge status={u.isActive ? 'Active' : 'Inactive'} /></div><dl className="mt-4 grid grid-cols-2 gap-3 text-sm"><div><dt className="text-muted">Role</dt><dd className="font-semibold text-ink">{u.role}</dd></div><div><dt className="text-muted">Joined</dt><dd className="font-semibold text-ink">{new Date(u.createdAt).toLocaleDateString()}</dd></div></dl><AccountStatusAction user={u} pending={pendingUserId === u.id} onToggle={() => handleToggleStatus(u.id, u.isActive)} /></Card>)}
              </div>
              <Card padding="none" className="hidden overflow-x-auto md:block">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-neutral-200 text-left text-neutral-500">
                      <th className="px-4 py-3 font-medium">Name</th>
                      <th className="px-4 py-3 font-medium">Email</th>
                      <th className="px-4 py-3 font-medium">Role</th>
                      <th className="px-4 py-3 font-medium">Status</th>
                      <th className="px-4 py-3 font-medium">Joined</th>
                      <th className="px-4 py-3"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-200">
                    {usersQuery.data.items.map((u) => (
                      <tr key={u.id}>
                        <td className="px-4 py-3 text-neutral-900">
                          {u.firstName} {u.lastName}
                        </td>
                        <td className="px-4 py-3 text-neutral-600">{u.email}</td>
                        <td className="px-4 py-3 text-neutral-600">{u.role}</td>
                        <td className="px-4 py-3">
                          <span
                            className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${
                              u.isActive
                                ? 'bg-green-50 text-green-700'
                                : 'bg-red-50 text-red-700'
                            }`}
                          >
                            {u.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-neutral-600">
                          {new Date(u.createdAt).toLocaleDateString()}
                        </td>
                        <td className="px-4 py-3 text-right">
                          <AccountStatusAction user={u} pending={pendingUserId === u.id} onToggle={() => handleToggleStatus(u.id, u.isActive)} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </Card>
              <div className="flex items-center justify-center gap-4 mt-6">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                >
                  Previous
                </Button>
                <span className="text-sm text-neutral-600">
                  Page {page} of {totalPages}
                </span>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={page >= totalPages}
                >
                  Next
                </Button>
              </div>
            </>
          )}
        </section>
      </div>
    </div>
  )
}

export default AdminPage
