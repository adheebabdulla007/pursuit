import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchUsers, fetchStats, updateUserStatus } from '../api/admin'

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

  return (
    <div>
      <h1>Admin Panel</h1>

      <section>
        <h2>Stats</h2>
        {statsQuery.isLoading && <p>Loading stats...</p>}
        {statsQuery.isError && <p>Error loading stats: {statsQuery.error.message}</p>}
        {statsQuery.data && (
          <ul>
            <li>Total Users: {statsQuery.data.totalUsers}</li>
            <li>Employers: {statsQuery.data.totalEmployers}</li>
            <li>Job Seekers: {statsQuery.data.totalJobSeekers}</li>
            <li>Total Jobs: {statsQuery.data.totalJobs}</li>
            <li>Total Applications: {statsQuery.data.totalApplications}</li>
          </ul>
        )}
      </section>

      <section>
        <h2>Users</h2>
        {toggleError && <p style={{ color: 'red' }}>{toggleError}</p>}
        {usersQuery.isLoading && <p>Loading users...</p>}
        {usersQuery.isError && <p>Error loading users: {usersQuery.error.message}</p>}
        {usersQuery.data && (
          <>
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th>Status</th>
                  <th>Joined</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {usersQuery.data.items.map((u) => (
                  <tr key={u.id}>
                    <td>{u.firstName} {u.lastName}</td>
                    <td>{u.email}</td>
                    <td>{u.role}</td>
                    <td>{u.isActive ? 'Active' : 'Inactive'}</td>
                    <td>{new Date(u.createdAt).toLocaleDateString()}</td>
                    <td>
                      <button
                        onClick={() => handleToggleStatus(u.id, u.isActive)}
                        disabled={pendingUserId === u.id}
                      >
                        {pendingUserId === u.id ? 'Updating...' : u.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div>
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}>
                Previous
              </button>
              <span> Page {page} of {totalPages} </span>
              <button onClick={() => setPage((p) => p + 1)} disabled={page >= totalPages}>
                Next
              </button>
            </div>
          </>
        )}
      </section>
    </div>
  )
}

export default AdminPage