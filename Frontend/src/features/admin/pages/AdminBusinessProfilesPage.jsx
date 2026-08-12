import { useEffect, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, XCircle } from 'lucide-react'
import toast from 'react-hot-toast'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import Badge from '@/components/common/Badge'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import { formatDate } from '@/utils/formatters'
import { VerificationStatus, VerificationStatusLabel } from '@/constants/enums'
import AdminDataTable from '../components/AdminDataTable'

const statusVariant = {
  [VerificationStatus.PENDING]: 'warning',
  [VerificationStatus.UNDER_REVIEW]: 'primary',
  [VerificationStatus.VERIFIED]: 'success',
  [VerificationStatus.REJECTED]: 'danger',
}

const pageSizeOptions = [15, 30, 50]

function RejectModal({ profile, onClose, onConfirm, isPending }) {
  const [reason, setReason] = useState('')
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
        <h2 className="mb-1 text-lg font-bold text-gray-900">Reject Business Profile</h2>
        <p className="mb-4 text-sm text-gray-500">{profile.companyName} - {profile.userFullName}</p>
        <textarea
          placeholder="Rejection reason (required)"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          className="form-input w-full"
          rows={3}
        />
        <div className="mt-4 flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose} disabled={isPending}>Cancel</Button>
          <Button
            type="button"
            variant="danger"
            disabled={!reason.trim() || isPending}
            onClick={() => onConfirm(reason.trim())}
          >
            {isPending ? 'Rejecting...' : 'Reject'}
          </Button>
        </div>
      </div>
    </div>
  )
}

export default function AdminBusinessProfilesPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [sortKey, setSortKey] = useState('createdAt_desc')
  const [rejectTarget, setRejectTarget] = useState(null)
  const [sortBy, sortDirection] = sortKey.split('_')

  useEffect(() => {
    setPage(1)
  }, [pageSize, search, sortKey, statusFilter])

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'business-profiles', page, pageSize, search, statusFilter, sortKey],
    queryFn: () => api.get(API_ENDPOINTS.ADMIN.BUSINESS_PROFILES, {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: statusFilter || undefined,
        sortBy,
        sortDirection,
      },
    }),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin', 'business-profiles'] })

  const approveMutation = useMutation({
    mutationFn: (id) => api.put(API_ENDPOINTS.ADMIN.BUSINESS_PROFILE_REVIEW(id), { isApproved: true }),
    onSuccess: () => { toast.success('Business profile approved'); invalidate() },
    onError: () => toast.error('Failed to approve'),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }) => api.put(API_ENDPOINTS.ADMIN.BUSINESS_PROFILE_REVIEW(id), { isApproved: false, rejectionReason: reason }),
    onSuccess: () => { toast.success('Business profile rejected'); setRejectTarget(null); invalidate() },
    onError: () => toast.error('Failed to reject'),
  })

  const profiles = data?.items || data?.data?.items || []
  const totalPages = data?.totalPages || data?.data?.totalPages || 1
  const totalCount = data?.totalCount || data?.data?.totalCount || 0
  const canReview = (profile) =>
    profile.verificationStatus === VerificationStatus.PENDING ||
    profile.verificationStatus === VerificationStatus.UNDER_REVIEW

  const columns = [
    {
      key: 'company',
      label: 'Company',
      sortKey: 'company',
      cellClassName: 'font-medium text-gray-900',
      render: (profile) => <span className="break-words">{profile.companyName}</span>,
    },
    {
      key: 'owner',
      label: 'Owner',
      sortKey: 'owner',
      render: (profile) => (
        <div className="min-w-0">
          <div className="font-medium text-gray-900">{profile.userFullName}</div>
          <div className="break-all text-xs text-gray-400">{profile.userEmail}</div>
        </div>
      ),
    },
    {
      key: 'gst',
      label: 'GST / PAN',
      sortKey: 'gst',
      cellClassName: 'text-xs text-gray-500',
      render: (profile) => (
        <div>
          <div>{profile.gstNumber}</div>
          <div>{profile.panNumber}</div>
        </div>
      ),
    },
    {
      key: 'documents',
      label: 'Docs',
      sortKey: 'documents',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (profile) => profile.documentCount,
    },
    {
      key: 'status',
      label: 'Status',
      sortKey: 'status',
      render: (profile) => (
        <Badge variant={statusVariant[profile.verificationStatus]}>
          {VerificationStatusLabel[profile.verificationStatus]}
        </Badge>
      ),
    },
    {
      key: 'submitted',
      label: 'Submitted',
      sortKey: 'createdAt',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (profile) => formatDate(profile.createdAt),
    },
    {
      key: 'actions',
      label: 'Actions',
      mobileLabel: false,
      render: (profile) => {
        if (!canReview(profile)) return <span className="text-xs text-gray-400">No action</span>

        return (
          <div className="flex items-center gap-1">
            <button
              title="Approve"
              onClick={() => approveMutation.mutate(profile.id)}
              disabled={approveMutation.isPending}
              className="rounded p-1.5 text-gray-400 transition-colors hover:bg-green-50 hover:text-green-600 disabled:opacity-50"
            >
              <CheckCircle2 size={16} />
            </button>
            <button
              title="Reject"
              onClick={() => setRejectTarget(profile)}
              disabled={rejectMutation.isPending}
              className="rounded p-1.5 text-gray-400 transition-colors hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
            >
              <XCircle size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  const resetFilters = () => {
    setSearch('')
    setStatusFilter('')
    setSortKey('createdAt_desc')
    setPageSize(15)
  }

  return (
    <div className="space-y-5">
      {rejectTarget && (
        <RejectModal
          profile={rejectTarget}
          isPending={rejectMutation.isPending}
          onClose={() => setRejectTarget(null)}
          onConfirm={(reason) => rejectMutation.mutate({ id: rejectTarget.id, reason })}
        />
      )}

      <div>
        <h1 className="text-2xl font-bold text-gray-900">Business Profiles</h1>
        <p className="text-sm text-gray-500">{totalCount.toLocaleString()} total profiles</p>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-[minmax(220px,1fr)_180px_110px_auto]">
          <input
            type="search"
            placeholder="Search company, GST, or email"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="form-input h-10 min-w-0 text-sm sm:col-span-2 xl:col-span-1"
          />
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="form-input h-10 min-w-0 bg-white text-sm"
          >
            <option value="">All statuses</option>
            {Object.values(VerificationStatus).map((value) => (
              <option key={value} value={value}>{VerificationStatusLabel[value]}</option>
            ))}
          </select>
          <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))} className="form-input h-10 min-w-0 bg-white text-sm">
            {pageSizeOptions.map((size) => <option key={size} value={size}>{size} / page</option>)}
          </select>
          <button type="button" onClick={resetFilters} className="h-10 rounded-md border border-gray-200 px-3 text-sm font-medium text-gray-600 hover:border-primary hover:text-primary">
            Reset
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : (
        <>
          <AdminDataTable
            columns={columns}
            rows={profiles}
            getRowKey={(profile) => profile.id}
            sortKey={sortKey}
            onSortChange={setSortKey}
            emptyMessage="No business profiles found"
          />
          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}
    </div>
  )
}
