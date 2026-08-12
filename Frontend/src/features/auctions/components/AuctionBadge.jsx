import { useAuctionCountdown } from '../hooks/useAuctionCountdown'
import { ListingStatus } from '@/constants/enums'

export default function AuctionBadge({ listing, className = '' }) {
  const timeLeft = useAuctionCountdown(listing?.auctionEndAt)

  if (listing?.listingType !== 1) return null

  if (listing.status === ListingStatus.SOLD) {
    return (
      <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-bold bg-gray-100 text-gray-600 ${className}`}>
        SOLD
      </span>
    )
  }

  if (!timeLeft || timeLeft.ended || listing.status !== ListingStatus.ACTIVE) {
    return (
      <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-bold bg-gray-100 text-gray-500 ${className}`}>
        ENDED
      </span>
    )
  }

  return (
    <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-bold ${
      timeLeft.isUrgent ? 'bg-red-100 text-red-700 animate-pulse' : 'bg-amber-100 text-amber-700'
    } ${className}`}>
      <span className="h-1.5 w-1.5 rounded-full bg-current" />
      {timeLeft.isUrgent ? 'ENDING SOON' : 'LIVE'} · {timeLeft.label}
    </span>
  )
}
