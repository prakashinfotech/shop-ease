import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Gavel } from 'lucide-react'
import { auctionService } from '@/features/auctions/services/auctionService'
import { buildRoute, ROUTES } from '@/constants/routes'
import { formatCurrency, formatDate } from '@/utils/formatters'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import Badge from '@/components/common/Badge'
import { useState } from 'react'

export default function MyBidsPage() {
  const [page, setPage] = useState(1)
  const pageSize = 20

  const { data, isLoading } = useQuery({
    queryKey: ['my-bids', page],
    queryFn: () => auctionService.getMyBids(page, pageSize),
  })

  const bids = data?.data?.items ?? []
  const total = data?.data?.totalCount ?? 0

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Gavel size={22} className="text-primary" />
        <h1 className="text-2xl font-bold text-gray-900">My Bids</h1>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : !bids.length ? (
        <div className="card p-12 text-center">
          <Gavel size={48} className="mx-auto text-gray-200 mb-4" />
          <p className="text-gray-500 font-medium">No bids placed yet</p>
          <Link to={ROUTES.LISTINGS} className="mt-3 inline-block text-sm text-primary">Browse auctions</Link>
        </div>
      ) : (
        <div className="card divide-y divide-gray-100">
          {bids.map((bid) => (
            <div key={bid.id} className="flex items-center justify-between px-4 py-3 hover:bg-gray-50">
              <div>
                <Link
                  to={buildRoute(ROUTES.LISTING_DETAIL, { id: bid.listingId })}
                  className="text-sm font-semibold text-primary hover:underline"
                >
                  View listing
                </Link>
                <p className="text-xs text-gray-400 mt-0.5">{formatDate(bid.placedAt)}</p>
              </div>
              <div className="text-right flex items-center gap-3">
                {bid.isWinning && <Badge variant="success">Winning</Badge>}
                <p className="text-base font-bold text-gray-900">{formatCurrency(bid.amount)}</p>
              </div>
            </div>
          ))}
        </div>
      )}

      {total > pageSize && (
        <Pagination page={page} pageSize={pageSize} total={total} onPageChange={setPage} />
      )}
    </div>
  )
}
