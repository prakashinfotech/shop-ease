import { useEffect, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Eye, Trash2, XCircle } from 'lucide-react'
import toast from 'react-hot-toast'
import api from '@/services/api'
import { API_ENDPOINTS } from '@/constants/api'
import Badge from '@/components/common/Badge'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import { formatCurrency, formatDate } from '@/utils/formatters'
import { ListingStatus, ListingStatusLabel } from '@/constants/enums'
import AdminDataTable from '../components/AdminDataTable'

const statusVariant = {
  [ListingStatus.DRAFT]: 'default',
  [ListingStatus.ACTIVE]: 'success',
  [ListingStatus.SOLD]: 'primary',
  [ListingStatus.ENDED]: 'warning',
  [ListingStatus.REMOVED]: 'danger',
  [ListingStatus.PENDING_APPROVAL]: 'warning',
  [ListingStatus.REJECTED]: 'danger',
}

const pageSizeOptions = [15, 30, 50, 100]

function RejectModal({ listing, onClose, onConfirm, isPending }) {
  const [reason, setReason] = useState('')
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
        <h2 className="mb-1 text-lg font-bold text-gray-900">Reject Listing</h2>
        <p className="mb-4 truncate text-sm text-gray-500">"{listing.title}"</p>
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

function DeleteModal({ listing, onClose, onConfirm, isPending }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-sm rounded-lg bg-white p-6 shadow-2xl">
        <h2 className="mb-2 text-lg font-bold text-gray-900">Delete Listing</h2>
        <p className="mb-4 text-sm text-gray-500">
          Are you sure you want to delete <span className="font-semibold text-gray-700">"{listing.title}"</span>? This action can be reversed later if needed.
        </p>
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose} disabled={isPending}>Cancel</Button>
          <Button
            type="button"
            variant="danger"
            disabled={isPending}
            onClick={() => onConfirm()}
          >
            {isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </div>
      </div>
    </div>
  )
}

function ViewListingModal({ listingId, onClose }) {
  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'listing', listingId],
    queryFn: () => api.get(API_ENDPOINTS.ADMIN.LISTING_DETAIL(listingId)),
    enabled: !!listingId,
  })

  const listing = data?.data

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-lg bg-white p-6 shadow-2xl relative">
        <button onClick={onClose} className="absolute top-4 right-4 text-gray-400 hover:text-gray-600">
          <XCircle size={24} />
        </button>
        <h2 className="mb-4 text-xl font-bold text-gray-900">Listing Details</h2>
        
        {isLoading ? (
          <div className="flex justify-center py-10"><Spinner size="md" /></div>
        ) : listing ? (
          <div className="space-y-4">
            <div>
              <p className="text-sm font-medium text-gray-500">Title</p>
              <p className="text-base text-gray-900">{listing.title}</p>
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500">Description</p>
              <p className="text-sm text-gray-800 bg-gray-50 p-3 rounded mt-1 whitespace-pre-wrap">{listing.description}</p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm font-medium text-gray-500">Seller</p>
                <p className="text-base text-gray-900">{listing.sellerName} ({listing.sellerEmail})</p>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-500">Price</p>
                <p className="text-base text-gray-900">{formatCurrency(listing.finalPrice ?? listing.price)}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-500">Status</p>
                <Badge variant={statusVariant[listing.status]}>{ListingStatusLabel[listing.status]}</Badge>
              </div>
              <div>
                <p className="text-sm font-medium text-gray-500">Created</p>
                <p className="text-base text-gray-900">{formatDate(listing.createdAt)}</p>
              </div>
            </div>
          </div>
        ) : (
          <p className="text-center text-gray-500 py-10">Failed to load listing details.</p>
        )}
      </div>
    </div>
  )
}

export default function AdminListingsPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [visibility, setVisibility] = useState('')
  const [sortKey, setSortKey] = useState('createdAt_desc')
  const [rejectTarget, setRejectTarget] = useState(null)
  const [viewTarget, setViewTarget] = useState(null)
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [sortBy, sortDirection] = sortKey.split('_')

  useEffect(() => {
    setPage(1)
  }, [pageSize, search, sortKey, status, visibility])

  const { data, isLoading } = useQuery({
    queryKey: ['admin', 'listings', page, pageSize, search, status, visibility, sortKey],
    queryFn: () => api.get(API_ENDPOINTS.ADMIN.LISTINGS, {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: status !== '' ? Number(status) : undefined,
        visibility: visibility || undefined,
        sortBy,
        sortDirection,
      },
    }),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin', 'listings'] })

  const approveMutation = useMutation({
    mutationFn: (id) => api.post(API_ENDPOINTS.ADMIN.LISTING_APPROVE(id), {}),
    onSuccess: () => { toast.success('Listing approved'); invalidate() },
    onError: () => toast.error('Failed to approve listing'),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }) => api.post(API_ENDPOINTS.ADMIN.LISTING_REJECT(id), { reason }),
    onSuccess: () => { toast.success('Listing rejected'); setRejectTarget(null); invalidate() },
    onError: () => toast.error('Failed to reject listing'),
  })

  const deleteMutation = useMutation({
    mutationFn: (id) => api.delete(API_ENDPOINTS.ADMIN.LISTING_DELETE(id)),
    onSuccess: () => { toast.success('Listing deleted'); setDeleteTarget(null); invalidate() },
    onError: () => toast.error('Failed to delete listing'),
  })

  const listings = data?.items || data?.data?.items || []
  const totalPages = data?.totalPages || data?.data?.totalPages || 1
  const totalCount = data?.totalCount || data?.data?.totalCount || 0

  const columns = [
    {
      key: 'title',
      label: 'Title',
      sortKey: 'title',
      cellClassName: 'font-medium text-gray-900',
      render: (listing) => (
        <div className="min-w-0">
          <div className="truncate">{listing.title}</div>
          {listing.hasPendingVersion && (
            <span className="mt-0.5 inline-block rounded bg-amber-100 px-1.5 py-0.5 text-[11px] font-medium text-amber-700">
              Pending update
            </span>
          )}
        </div>
      ),
    },
    {
      key: 'seller',
      label: 'Seller',
      sortKey: 'seller',
      cellClassName: 'text-gray-500',
      render: (listing) => <span className="break-words">{listing.sellerName}</span>,
    },
    {
      key: 'price',
      label: 'Price',
      sortKey: 'price',
      defaultDirection: 'desc',
      cellClassName: 'font-semibold',
      render: (listing) => (
        <div className="flex flex-wrap items-baseline gap-2">
          <span>{formatCurrency(listing.finalPrice ?? listing.price)}</span>
          {Number(listing.discountAmount || 0) > 0 && (
            <span className="text-xs font-normal text-gray-400 line-through">{formatCurrency(listing.price)}</span>
          )}
        </div>
      ),
    },
    {
      key: 'status',
      label: 'Status',
      sortKey: 'status',
      render: (listing) => (
        <Badge variant={statusVariant[listing.status]}>{ListingStatusLabel[listing.status]}</Badge>
      ),
    },
    {
      key: 'created',
      label: 'Created',
      sortKey: 'createdAt',
      defaultDirection: 'desc',
      cellClassName: 'text-gray-500',
      render: (listing) => formatDate(listing.createdAt),
    },
    {
      key: 'actions',
      label: 'Actions',
      mobileLabel: false,
      render: (listing) => {
        return (
          <div className="flex items-center gap-2">
            <button
              title="View Details"
              onClick={() => setViewTarget(listing.id)}
              className="rounded p-1.5 text-gray-400 transition-colors hover:bg-blue-50 hover:text-blue-600"
            >
              <Eye size={16} />
            </button>
            <button
              title="Delete"
              onClick={() => setDeleteTarget(listing)}
              disabled={deleteMutation.isPending}
              className="rounded p-1.5 text-gray-400 transition-colors hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  const resetFilters = () => {
    setSearch('')
    setStatus('')
    setVisibility('')
    setSortKey('createdAt_desc')
    setPageSize(15)
  }

  return (
    <div className="space-y-5">
      {rejectTarget && (
        <RejectModal
          listing={rejectTarget}
          isPending={rejectMutation.isPending}
          onClose={() => setRejectTarget(null)}
          onConfirm={(reason) => rejectMutation.mutate({ id: rejectTarget.id, reason })}
        />
      )}
      
      {viewTarget && (
        <ViewListingModal
          listingId={viewTarget}
          onClose={() => setViewTarget(null)}
        />
      )}

      {deleteTarget && (
        <DeleteModal
          listing={deleteTarget}
          isPending={deleteMutation.isPending}
          onClose={() => setDeleteTarget(null)}
          onConfirm={() => deleteMutation.mutate(deleteTarget.id)}
        />
      )}

      <div>
        <h1 className="text-2xl font-bold text-gray-900">Listings</h1>
        <p className="text-sm text-gray-500">{totalCount.toLocaleString()} total listings</p>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-[minmax(220px,1fr)_150px_140px_110px_auto]">
          <input
            type="search"
            placeholder="Search title or seller"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="form-input h-10 min-w-0 text-sm sm:col-span-2 xl:col-span-1"
          />
          <select value={status} onChange={(e) => setStatus(e.target.value)} className="form-input h-10 min-w-0 bg-white text-sm">
            <option value="">All statuses</option>
            {Object.values(ListingStatus)
              .filter(value => value !== ListingStatus.PENDING_APPROVAL)
              .map((value) => (
              <option key={value} value={value}>{ListingStatusLabel[value]}</option>
            ))}
          </select>
          <select value={visibility} onChange={(e) => setVisibility(e.target.value)} className="form-input h-10 min-w-0 bg-white text-sm">
            <option value="">All visibility</option>
            <option value="active">Visible</option>
            <option value="deleted">Deleted</option>
          </select>
          <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))} className="form-input h-10 min-w-0 bg-white text-sm">
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>{size} / page</option>
            ))}
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
            rows={listings}
            getRowKey={(listing) => listing.id}
            sortKey={sortKey}
            onSortChange={setSortKey}
            emptyMessage="No listings found"
          />
          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}
    </div>
  )
}
