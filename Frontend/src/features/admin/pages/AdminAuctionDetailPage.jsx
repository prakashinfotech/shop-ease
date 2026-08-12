import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Clock, XCircle } from 'lucide-react'
import { auctionService } from '@/features/auctions/services/auctionService'
import { ROUTES } from '@/constants/routes'
import { ListingStatus, ListingStatusLabel } from '@/constants/enums'
import { formatCurrency, formatDate } from '@/utils/formatters'
import Button from '@/components/common/Button'
import Badge from '@/components/common/Badge'
import Spinner from '@/components/common/Spinner'
import Modal from '@/components/common/Modal'
import Pagination from '@/components/common/Pagination'
import toast from 'react-hot-toast'

export default function AdminAuctionDetailPage() {
  const { listingId } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [extendModal, setExtendModal] = useState(false)
  const [extendMinutes, setExtendMinutes] = useState(30)

  const { data: statusData, isLoading: statusLoading } = useQuery({
    queryKey: ['admin-auction-status', listingId],
    queryFn: () => auctionService.getStatus(listingId),
    enabled: !!listingId,
  })

  const { data: bidsData, isLoading: bidsLoading } = useQuery({
    queryKey: ['admin-auction-bids', listingId, page],
    queryFn: () => auctionService.adminGetBids(listingId, page, 50),
    enabled: !!listingId,
  })

  const status = statusData?.data
  const bids = bidsData?.data?.items ?? []
  const total = bidsData?.data?.totalCount ?? 0

  const cancelMutation = useMutation({
    mutationFn: () => auctionService.adminCancel(listingId),
    onSuccess: () => {
      toast.success('Auction cancelled')
      queryClient.invalidateQueries({ queryKey: ['admin-auction-status', listingId] })
      queryClient.invalidateQueries({ queryKey: ['admin-auctions'] })
    },
    onError: () => toast.error('Failed to cancel'),
  })

  const extendMutation = useMutation({
    mutationFn: () => auctionService.adminExtend(listingId, extendMinutes),
    onSuccess: () => {
      toast.success('Auction extended')
      setExtendModal(false)
      queryClient.invalidateQueries({ queryKey: ['admin-auction-status', listingId] })
    },
    onError: () => toast.error('Failed to extend'),
  })

  if (statusLoading) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>
  if (!status) return <div className="card p-8 text-center text-gray-400">Auction not found</div>

  const isActive = status.status === ListingStatus.ACTIVE

  return (
    <div className="space-y-6">
      <button onClick={() => navigate(ROUTES.ADMIN_AUCTIONS)} className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-800">
        <ArrowLeft size={15} /> Back to Auctions
      </button>

      <div className="card p-6 space-y-4">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h1 className="text-xl font-bold text-gray-900">{status.title}</h1>
            <p className="text-sm text-gray-400 mt-0.5">{listingId}</p>
          </div>
          <Badge variant={status.status === ListingStatus.ACTIVE ? 'success' : 'warning'}>
            {ListingStatusLabel[status.status]}
          </Badge>
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
          <div>
            <p className="text-gray-400">Starting bid</p>
            <p className="font-bold">{status.startingBid ? formatCurrency(status.startingBid) : '—'}</p>
          </div>
          <div>
            <p className="text-gray-400">Current bid</p>
            <p className="font-bold text-lg">{status.currentBidAmount ? formatCurrency(status.currentBidAmount) : '—'}</p>
          </div>
          <div>
            <p className="text-gray-400">Reserve</p>
            <p className={`font-bold ${status.reserveMet ? 'text-green-600' : 'text-amber-600'}`}>
              {status.reservePrice ? `${formatCurrency(status.reservePrice)} (${status.reserveMet ? 'met' : 'not met'})` : 'No reserve'}
            </p>
          </div>
          <div>
            <p className="text-gray-400">Total bids</p>
            <p className="font-bold">{status.bidCount}</p>
          </div>
          <div>
            <p className="text-gray-400">Ends at</p>
            <p className="font-bold">{status.auctionEndAt ? formatDate(status.auctionEndAt) : '—'}</p>
          </div>
        </div>

        {isActive && (
          <div className="flex gap-3 pt-2">
            <Button variant="secondary" onClick={() => setExtendModal(true)} className="gap-1">
              <Clock size={14} /> Extend
            </Button>
            <Button variant="danger" onClick={() => cancelMutation.mutate()} disabled={cancelMutation.isPending} className="gap-1">
              <XCircle size={14} /> Cancel Auction
            </Button>
          </div>
        )}
      </div>

      <div className="card overflow-hidden">
        <div className="px-4 py-3 border-b border-gray-200 font-semibold text-gray-700">
          Bid History ({total})
        </div>
        {bidsLoading ? (
          <div className="flex justify-center py-8"><Spinner /></div>
        ) : !bids.length ? (
          <p className="text-sm text-gray-400 text-center py-8">No bids yet</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-2 text-left font-semibold text-gray-600">Bidder</th>
                <th className="px-4 py-2 text-left font-semibold text-gray-600">Email</th>
                <th className="px-4 py-2 text-right font-semibold text-gray-600">Amount</th>
                <th className="px-4 py-2 text-left font-semibold text-gray-600">Placed At</th>
                <th className="px-4 py-2 text-left font-semibold text-gray-600">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {bids.map((b) => (
                <tr key={b.id} className={b.isWinning ? 'bg-green-50' : 'hover:bg-gray-50'}>
                  <td className="px-4 py-2 font-medium">{b.bidderName || b.bidderId}</td>
                  <td className="px-4 py-2 text-gray-500">{b.bidderEmail}</td>
                  <td className="px-4 py-2 text-right font-bold">{formatCurrency(b.amount)}</td>
                  <td className="px-4 py-2 text-gray-500 text-xs">{formatDate(b.placedAt)}</td>
                  <td className="px-4 py-2">
                    {b.isWinning && <Badge variant="success">Winning</Badge>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {total > 50 && (
          <div className="p-4">
            <Pagination page={page} pageSize={50} total={total} onPageChange={setPage} />
          </div>
        )}
      </div>

      {extendModal && (
        <Modal title="Extend Auction" onClose={() => setExtendModal(false)}>
          <div className="space-y-4">
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
              <Button onClick={() => extendMutation.mutate()} disabled={extendMutation.isPending}>Extend</Button>
              <Button variant="secondary" onClick={() => setExtendModal(false)}>Cancel</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
