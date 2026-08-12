import { Link } from 'react-router-dom'
import { ChevronRight, Tag } from 'lucide-react'
import Breadcrumbs from '@/components/common/Breadcrumbs'
import Spinner from '@/components/common/Spinner'
import { useAuthStore } from '@/store/authStore'
import { ROUTES } from '@/constants/routes'
import ListingCard from '../components/ListingCard'
import { useListings } from '../hooks/useListings'

const DEAL_TIERS = [
  { min: 20, max: Infinity, title: '20% off and above' },
  { min: 15, max: 20, title: '15% to 19% off' },
  { min: 10, max: 15, title: '10% to 14% off' },
  { min: 5, max: 10, title: '5% to 9% off' },
]

const getDiscountPercentage = (listing) => {
  const price = Number(listing.price || 0)
  const discount = Number(listing.discountAmount || 0)
  if (!price || discount <= 0) return 0
  return Math.round((discount / price) * 100)
}

const buildDealRows = (listings) =>
  DEAL_TIERS.map((tier) => ({
    ...tier,
    items: listings.filter((listing) => {
      const percentage = getDiscountPercentage(listing)
      return percentage >= tier.min && percentage < tier.max
    }),
  })).filter((row) => row.items.length > 0)

export default function DealsPage() {
  const user = useAuthStore((state) => state.user)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)

  const { data, isLoading } = useListings({
    page: 1,
    pageSize: 100,
    status: 1,
    excludeSellerId: isAuthenticated ? user?.id : undefined,
    sortBy: 'updatedAt',
    sortDirection: 'desc',
  })

  const discountedListings = (data?.data?.items || [])
    .filter((listing) =>
      listing.sellerId !== user?.id &&
      getDiscountPercentage(listing) >= 5
    )
    .sort((a, b) => getDiscountPercentage(b) - getDiscountPercentage(a))
  const dealRows = buildDealRows(discountedListings)

  if (isLoading) {
    return (
      <div className="flex justify-center py-24">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="space-y-7">
      <Breadcrumbs items={[{ label: 'Deals' }]} />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="mb-2 inline-flex items-center gap-2 rounded-full bg-green-50 px-3 py-1 text-xs font-bold uppercase text-green-700">
            <Tag size={13} />
            Discounted listings
          </div>
          <h1 className="text-2xl font-bold text-gray-900">Daily Deals</h1>
          <p className="mt-1 text-sm text-gray-500">
            Browse discounted items grouped by savings.
          </p>
        </div>
        <Link
          to={ROUTES.LISTINGS}
          className="inline-flex items-center gap-1 text-sm font-semibold text-primary hover:text-primary-700"
        >
          View all listings <ChevronRight size={15} />
        </Link>
      </div>

      {dealRows.length === 0 ? (
        <div className="card py-16 text-center">
          <p className="text-lg font-semibold text-gray-800">No deals available right now</p>
          <p className="mt-1 text-sm text-gray-500">Check back soon for discounted listings.</p>
        </div>
      ) : (
        <div className="space-y-8">
          {dealRows.map((row) => (
            <section key={row.title} className="space-y-3">
              <div className="flex items-center justify-between gap-3">
                <h2 className="text-lg font-bold text-gray-900">{row.title}</h2>
                <span className="text-sm text-gray-500">{row.items.length} items</span>
              </div>
              <div className="flex gap-4 overflow-x-auto pb-3">
                {row.items.map((listing) => (
                  <div key={listing.id} className="w-44 shrink-0 sm:w-52">
                    <ListingCard listing={listing} />
                  </div>
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  )
}
