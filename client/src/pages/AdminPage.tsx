import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchUsers, fetchStats, updateUserStatus } from '../api/admin'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'

const PAGE_SIZE = 10

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
    <div className="min-h-screen bg-neutral-50 px-4 py-8">
      <div className="max-w-4xl mx-auto">
        <h1 className="text-2xl font-semibold text-neutral-900 mb-6">Admin Panel</h1>

        <section className="mb-8">
          <h2 className="text-lg font-semibold text-neutral-900 mb-3">Stats</h2>
          {statsQuery.isLoading && <p className="text-neutral-600">Loading stats...</p>}
          {statsQuery.isError && (
            <p className="bg-red-50 border border-red-200 text-red-700 rounded-md p-3 text-sm">
              Error loading stats: {statsQuery.error.message}
            </p>
          )}
          {statsQuery.data && (
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
              {statsItems.map((item) => (
                <Card key={item.label} padding="sm">
                  <p className="text-2xl font-semibold text-neutral-900">{item.value}</p>
                  <p className="text-sm text-neutral-600 mt-1">{item.label}</p>
                </Card>
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
              <Card padding="none" className="overflow-x-auto">
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
                          <Button
                            variant={u.isActive ? 'destructive' : 'secondary'}
                            size="sm"
                            onClick={() => handleToggleStatus(u.id, u.isActive)}
                            disabled={pendingUserId === u.id}
                          >
                            {pendingUserId === u.id
                              ? 'Updating...'
                              : u.isActive
                                ? 'Deactivate'
                                : 'Activate'}
                          </Button>
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