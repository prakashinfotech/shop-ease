import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Gavel, Clock } from 'lucide-react'
import { auctionService } from '../services/auctionService'
import { useAuctionHub } from '../hooks/useAuctionHub'
import { useAuctionCountdown } from '../hooks/useAuctionCountdown'
import BidHistory from './BidHistory'
import ReserveIndicator from './ReserveIndicator'
import Button from '@/components/common/Button'
import { formatCurrency } from '@/utils/formatters'
import { useAuthStore } from '@/store/authStore'
import toast from 'react-hot-toast'

export default function BidPanel({ listing }) {
  const [bidAmount, setBidAmount] = useState('')
  const queryClient = useQueryClient()
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const user = useAuthStore((s) => s.user)
  const isSeller = user?.id === listing.sellerId

  const { data: statusData, refetch: refetchStatus } = useQuery({
    queryKey: ['auction-status', listing.id],
    queryFn: () => auctionService.getStatus(listing.id),
    enabled: !!listing.id,
    refetchInterval: 30000,
  })

  const status = statusData?.data ?? {
    currentBidAmount: listing.currentBidAmount,
    startingBid: listing.startingBid,
    reservePrice: listing.reservePrice,
    buyItNowPrice: listing.buyItNowPrice,
    minBidIncrement: listing.minBidIncrement ?? 1,
    bidCount: listing.bidCount ?? 0,
    auctionEndAt: listing.auctionEndAt,
    reserveMet: false,
  }

  const timeLeft = useAuctionCountdown(status.auctionEndAt)

  useAuctionHub(listing.id, {
    onBidPlaced: (evt) => {
      queryClient.invalidateQueries({ queryKey: ['auction-status', listing.id] })
      queryClient.invalidateQueries({ queryKey: ['auction-bids', listing.id] })
      toast(`New bid: ${formatCurrency(evt.amount)}`, { icon: '🔨' })
    },
    onAuctionEnded: () => {
      queryClient.invalidateQueries({ queryKey: ['auction-status', listing.id] })
      toast('Auction has ended!', { icon: '🏁' })
    },
    onTimeExtended: (evt) => {
      queryClient.invalidateQueries({ queryKey: ['auction-status', listing.id] })
      toast('Auction time extended!', { icon: '⏱' })
    },
    onAuctionCancelled: () => {
      toast.error('Auction was cancelled.')
      queryClient.invalidateQueries({ queryKey: ['auction-status', listing.id] })
    },
  })

  const placeBid = useMutation({
    mutationFn: () => auctionService.placeBid(listing.id, Number(bidAmount)),
    onSuccess: () => {
      toast.success('Bid placed!')
      setBidAmount('')
      queryClient.invalidateQueries({ queryKey: ['auction-status', listing.id] })
      queryClient.invalidateQueries({ queryKey: ['auction-bids', listing.id] })
    },
    onError: (err) => toast.error(err?.response?.data?.message ?? 'Failed to place bid'),
  })

  const currentBid = status.currentBidAmount ?? status.startingBid ?? 0
  const minNext = status.currentBidAmount
    ? status.currentBidAmount + (status.minBidIncrement ?? 1)
    : (status.startingBid ?? status.minBidIncrement ?? 1)

  const auctionEnded = !timeLeft || timeLeft.ended
  const isCurrentWinner = status.isCurrentUserWinning ?? false
  const canBid = isAuthenticated && !auctionEnded && !isSeller && !isCurrentWinner

  return (
    <div className="space-y-4">
      {/* Countdown */}
      <div className={`flex items-center gap-2 rounded-lg px-4 py-3 ${
        timeLeft?.isUrgent ? 'bg-red-50 border border-red-200' : 'bg-amber-50 border border-amber-200'
      }`}>
        <Clock size={16} className={timeLeft?.isUrgent ? 'text-red-600' : 'text-amber-600'} />
        <div>
          <p className="text-xs text-gray-500">Time remaining</p>
          <p className={`text-lg font-bold font-mono ${timeLeft?.isUrgent ? 'text-red-600' : 'text-amber-700'}`}>
            {timeLeft?.label ?? 'Loading…'}
          </p>
        </div>
      </div>

      {/* Current bid */}
      <div className="rounded-lg border border-gray-200 bg-white p-4 space-y-2">
        <div className="flex justify-between items-baseline">
          <span className="text-sm text-gray-500">Current bid</span>
          <span className="text-2xl font-extrabold text-gray-900">{formatCurrency(currentBid)}</span>
        </div>
        <div className="flex justify-between items-center text-xs">
          <span className="text-gray-500">{status.bidCount} bid{status.bidCount !== 1 ? 's' : ''}</span>
          <ReserveIndicator reservePrice={status.reservePrice} currentBid={status.currentBidAmount} />
        </div>
        {status.buyItNowPrice && (
          <p className="text-xs text-gray-400">Buy It Now: {formatCurrency(status.buyItNowPrice)}</p>
        )}
      </div>

      {/* Bid input */}
      {isSeller ? (
        <div className="rounded-lg bg-blue-50 border border-blue-200 p-4 text-center">
          <p className="text-sm font-semibold text-blue-700">This is your listing</p>
          <p className="text-xs text-blue-500 mt-0.5">Sellers cannot bid on their own auctions.</p>
        </div>
      ) : isCurrentWinner ? (
        <div className="rounded-lg bg-green-50 border border-green-200 p-4 text-center">
          <p className="text-sm font-semibold text-green-700">🏆 You are the highest bidder!</p>
          <p className="text-xs text-green-600 mt-0.5">You cannot raise your own bid. Wait for another bidder.</p>
        </div>
      ) : canBid ? (
        <div className="space-y-2">
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 font-medium">₹</span>
            <input
              type="number"
              value={bidAmount}
              onChange={(e) => setBidAmount(e.target.value)}
              placeholder={`${minNext} or more`}
              min={minNext}
              step={status.minBidIncrement ?? 1}
              className="w-full border border-gray-300 rounded-md pl-7 pr-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <p className="text-xs text-gray-400">Minimum bid: {formatCurrency(minNext)}</p>
          <Button
            onClick={() => placeBid.mutate()}
            disabled={!bidAmount || Number(bidAmount) < minNext || placeBid.isPending}
            className="w-full gap-2"
            size="lg"
          >
            <Gavel size={16} />
            {placeBid.isPending ? 'Placing…' : 'Place Bid'}
          </Button>
        </div>
      ) : auctionEnded ? (
        <div className="rounded-lg bg-gray-50 border border-gray-200 p-4 text-center">
          <p className="font-semibold text-gray-600">Auction has ended</p>
        </div>
      ) : (
        <p className="text-sm text-center text-gray-500">Sign in to place a bid</p>
      )}

      {/* Bid history */}
      <div>
        <h3 className="text-sm font-semibold text-gray-700 mb-2">Bid History</h3>
        <BidHistory listingId={listing.id} />
      </div>
    </div>
  )
}
