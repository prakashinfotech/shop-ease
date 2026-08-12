import { useQuery } from '@tanstack/react-query'
import { auctionService } from '../services/auctionService'
import { formatCurrency, formatDate } from '@/utils/formatters'
import Spinner from '@/components/common/Spinner'

export default function BidHistory({ listingId }) {
  const { data, isLoading } = useQuery({
    queryKey: ['auction-bids', listingId],
    queryFn: () => auctionService.getBids(listingId),
    enabled: !!listingId,
    refetchInterval: 15000,
  })

  const bids = data?.data?.items ?? []

  if (isLoading) return <div className="flex justify-center py-4"><Spinner /></div>
  if (!bids.length) return <p className="text-sm text-gray-400 text-center py-4">No bids yet. Be the first!</p>

  return (
    <div className="space-y-2">
      {bids.map((bid, i) => (
        <div key={bid.id} className={`flex items-center justify-between py-2 px-3 rounded-lg text-sm ${
          i === 0 ? 'bg-green-50 border border-green-200' : 'bg-gray-50'
        }`}>
          <div className="flex items-center gap-2">
            <span className={`font-mono text-xs ${i === 0 ? 'text-green-700 font-bold' : 'text-gray-500'}`}>
              {bid.bidderHandle}
            </span>
            {i === 0 && <span className="text-[10px] bg-green-600 text-white px-1.5 py-0.5 rounded font-bold">WINNING</span>}
          </div>
          <div className="text-right">
            <p className={`font-bold ${i === 0 ? 'text-green-700' : 'text-gray-800'}`}>{formatCurrency(bid.amount)}</p>
            <p className="text-[10px] text-gray-400">{formatDate(bid.placedAt)}</p>
          </div>
        </div>
      ))}
    </div>
  )
}
