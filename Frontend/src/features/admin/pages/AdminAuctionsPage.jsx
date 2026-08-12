import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Gavel, Eye, XCircle, Clock } from 'lucide-react'
import { auctionService } from '@/features/auctions/services/auctionService'
import { buildRoute, ROUTES } from '@/constants/routes'
import { formatCurrency, formatDate } from '@/utils/formatters'
import { ListingStatus, ListingStatusLabel } from '@/constants/enums'
import Button from '@/components/common/Button'
import Badge from '@/components/common/Badge'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import Modal from '@/components/common/Modal'
import toast from 'react-hot-toast'

const statusVariant = {
  [ListingStatus.ACTIVE]: 'success',
  [ListingStatus.ENDED]: 'warning',
  [ListingStatus.SOLD]: 'danger',
  [ListingStatus.REMOVED]: 'default',
}

export default function AdminAuctionsPage() {
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState('')
  const [extendModal, setExtendModal] = useState(null)
  const [extendMinutes, setExtendMinutes] = useState(30)
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const { data, isLoading } = useQuery({
    queryKey: ['admin-auctions', page, statusFilter],
    queryFn: () => auctionService.adminGetAuctions(page, 15, statusFilter || undefined),
  })

  const auctions = data?.data?.items ?? []
  const total = data?.data?.totalCount ?? 0

  const cancelMutation = useMutation({
    mutationFn: (id) => auctionService.adminCancel(id),
    onSuccess: () => {
      toast.success('Auction cancelled')
      queryClient.invalidateQueries({ queryKey: ['admin-auctions'] })
    },
    onError: () => toast.error('Failed to cancel auction'),
  })

  const extendMutation = useMutation({
    mutationFn: ({ id, minutes }) => auctionService.adminExtend(id, minutes),
    onSuccess: () => {
      toast.success('Auction extended')
      setExtendModal(null)
      queryClient.invalidateQueries({ queryKey: ['admin-auctions'] })
    },
    onError: () => toast.error('Failed to extend auction'),
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Gavel size={22} className="text-primary" />
          <h1 className="text-2xl font-bold text-gray-900">Auctions</h1>
        </div>
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
          className="border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">All statuses</option>
          <option value="1">Active</option>
          <option value="2">Sold</option>
          <option value="3">Ended</option>
        </select>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : !auctions.length ? (
        <div className="card p-12 text-center text-gray-400">No auctions found</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Listing</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Seller</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">Current Bid</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">Bids</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Ends At</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {auctions.map((a) => (
                <tr key={a.listingId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900 max-w-[200px] truncate">{a.title}</td>
                  <td className="px-4 py-3 text-gray-600">{a.sellerName}</td>
                  <td className="px-4 py-3 text-right font-semibold">
                    {a.currentBidAmount ? formatCurrency(a.currentBidAmount) : a.startingBid ? formatCurrency(a.startingBid) : '—'}
                  </td>
                  <td className="px-4 py-3 text-right text-gray-600">{a.bidCount}</td>
                  <td className="px-4 py-3 text-gray-600 text-xs">
                    {a.auctionEndAt ? formatDate(a.auctionEndAt) : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={statusVariant[a.status] ?? 'default'}>
                      {ListingStatusLabel[a.status]}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1 justify-end">
                      <button
                        onClick={() => navigate(buildRoute(ROUTES.ADMIN_AUCTION_DETAIL, { listingId: a.listingId }))}
                        className="p-1.5 text-gray-400 hover:text-primary rounded"
                        title="View bids"
                      >
                        <Eye size={15} />
                      </button>
                      {a.status === ListingStatus.ACTIVE && (
                        <>
                          <button
                            onClick={() => setExtendModal(a)}
                            className="p-1.5 text-gray-400 hover:text-amber-600 rounded"
                            title="Extend"
                          >
                            <Clock size={15} />
                          </button>
                          <button
                            onClick={() => cancelMutation.mutate(a.listingId)}
                            className="p-1.5 text-gray-400 hover:text-red-600 rounded"
                            title="Cancel"
                          >
                            <XCircle size={15} />
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {total > 15 && (
        <Pagination page={page} pageSize={15} total={total} onPageChange={setPage} />
      )}

      {extendModal && (
        <Modal title="Extend Auction" onClose={() => setExtendModal(null)}>
          <div className="space-y-4">
            <p className="text-sm text-gray-600">Extend <strong>{extendModal.title}</strong></p>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Minutes to add</label>
              <input
                type="number"
                value={extendMinutes}
                onChange={(e) => setExtendMinutes(Number(e.target.value))}
                min={1}
                max={1440}
                className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div className="flex gap-3">
              <Button
                onClick={() => extendMutation.mutate({ id: extendModal.listingId, minutes: extendMinutes })}
                disabled={extendMutation.isPending}
              >
                Extend
              </Button>
              <Button variant="secondary" onClick={() => setExtendModal(null)}>Cancel</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
