import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { FileText } from 'lucide-react'
import { fetchResumeDownloadUrl, updateApplicationStatus } from '../../api/applications'
import type { ApplicationDto, ApplicationStatus } from '../../types/application'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { ConfirmationDialog } from '../ui/ConfirmationDialog'
import { StatusBadge } from '../ui/StatusBadge'

const statuses: ApplicationStatus[] = ['Applied', 'Reviewed', 'Rejected', 'Hired']

export function ApplicationDetail({ application }: { application: ApplicationDto }) {
  const [nextStatus, setNextStatus] = useState<ApplicationStatus>(application.status)
  const queryClient = useQueryClient()
  const statusMutation = useMutation({
    mutationFn: () => updateApplicationStatus(application.id, nextStatus),
    onSuccess: async () => { await Promise.all([queryClient.invalidateQueries({ queryKey: ['application', application.id] }), queryClient.invalidateQueries({ queryKey: ['applications', application.jobId] })]) },
  })
  const resumeMutation = useMutation({
    mutationFn: () => fetchResumeDownloadUrl(application.id),
    onSuccess: ({ downloadUrl }) => {
      const anchor = document.createElement('a')
      anchor.href = downloadUrl
      anchor.target = '_blank'
      anchor.rel = 'noopener noreferrer'
      anchor.click()
    },
  })

  return <div>
    <div className="flex flex-wrap items-start justify-between gap-3 border-b border-line pb-5"><div><p className="text-xs font-bold uppercase tracking-[0.14em] text-action">Candidate</p><h2 className="mt-1 text-2xl font-extrabold text-ink">{application.applicantName}</h2><p className="mt-1 text-sm text-muted">{application.jobTitle}</p></div><StatusBadge status={application.status} /></div>
    {(statusMutation.error || resumeMutation.error) && <Alert className="mt-5" variant="danger">{(statusMutation.error ?? resumeMutation.error)?.message}</Alert>}
    <section className="mt-6"><h3 className="text-sm font-bold text-ink">Resume</h3><p className="mt-1 text-sm text-muted">A secure download link is created only when requested.</p><Button type="button" variant="secondary" className="mt-3" disabled={resumeMutation.isPending} onClick={() => resumeMutation.mutate()}><FileText size={16} />{resumeMutation.isPending ? 'Opening…' : 'Open resume'}</Button></section>
    <section className="mt-7 border-t border-line pt-6"><h3 className="text-sm font-bold text-ink">Decision</h3><div className="mt-3 flex flex-col gap-3 sm:flex-row"><select aria-label="Application status" value={nextStatus} onChange={(e) => setNextStatus(e.target.value as ApplicationStatus)} className="h-10 rounded-md border border-line bg-surface px-3 text-ink">{statuses.map((status) => <option key={status}>{status}</option>)}</select><ConfirmationDialog trigger={<Button type="button" disabled={nextStatus === application.status || statusMutation.isPending}>Update status</Button>} title={`Change status to ${nextStatus}?`} description="This updates the candidate record immediately." confirmLabel="Confirm status" pending={statusMutation.isPending} variant={nextStatus === 'Rejected' ? 'danger' : 'default'} onConfirm={() => statusMutation.mutate()} /></div></section>
  </div>
}
