import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ChevronRight, Shield, Zap, RotateCcw, Gavel } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { useListings, useRecentlyViewedListings } from '../hooks/useListings'
import ListingCard from '../components/ListingCard'
import ListingGrid from '../components/ListingGrid'
import AuctionBadge from '@/features/auctions/components/AuctionBadge'
import { auctionService } from '@/features/auctions/services/auctionService'
import { buildRoute, ROUTES } from '@/constants/routes'
import { formatCurrency } from '@/utils/formatters'
import { assetUrl } from '@/utils/assets'
import { useAuthStore } from '@/store/authStore'

const CATEGORIES = [
  { label: 'Electronics', emoji: '📱', value: 'electronics', bg: 'bg-blue-50 hover:bg-blue-100 border-blue-100' },
  { label: 'Fashion', emoji: '👗', value: 'fashion', bg: 'bg-pink-50 hover:bg-pink-100 border-pink-100' },
  { label: 'Home & Garden', emoji: '🏡', value: 'home-garden', bg: 'bg-green-50 hover:bg-green-100 border-green-100' },
  { label: 'Vehicles', emoji: '🚗', value: 'vehicles', bg: 'bg-yellow-50 hover:bg-yellow-100 border-yellow-100' },
  { label: 'Sports', emoji: '⚽', value: 'sports', bg: 'bg-orange-50 hover:bg-orange-100 border-orange-100' },
  { label: 'Toys', emoji: '🧸', value: 'toys', bg: 'bg-purple-50 hover:bg-purple-100 border-purple-100' },
  { label: 'Books', emoji: '📚', value: 'books', bg: 'bg-amber-50 hover:bg-amber-100 border-amber-100' },
  { label: 'Collectibles', emoji: '🏆', value: 'collectibles', bg: 'bg-red-50 hover:bg-red-100 border-red-100' },
]

const TRUST_BADGES = [
  { icon: Shield, title: 'Buyer Protection', desc: 'Money-back guarantee on every purchase' },
  { icon: Zap, title: 'Fast Delivery', desc: 'Ships within 24 hours on eligible items' },
  { icon: RotateCcw, title: 'Easy Returns', desc: '30-day hassle-free return policy' },
]

export default function HomePage() {
  const [page, setPage] = useState(1)
  const user = useAuthStore((state) => state.user)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const { data, isLoading } = useListings({
    page,
    pageSize: 20,
    status: 1,
    listingType: 0,
    excludeSellerId: isAuthenticated ? user?.id : undefined,
    sortBy: 'updatedAt',
    sortDirection: 'desc',
  })

  const { data: auctionsData } = useQuery({
    queryKey: ['home-auctions'],
    queryFn: () => auctionService.getActive(1, 12),
    staleTime: 30_000,
  })
  const liveAuctions = auctionsData?.data?.items ?? []
  const { data: recentViewedData } = useRecentlyViewedListings(
    { days: 3, take: 12 },
    { enabled: isAuthenticated },
  )
  const recentViewedItems = recentViewedData?.data || []

  return (
    <div className="space-y-10">
      {/* ── Hero Banner ── */}
      <div className="relative bg-gradient-to-br from-primary-700 via-primary to-blue-400 rounded-2xl overflow-hidden min-h-[220px] flex items-center">
        {/* Decorative circles */}
        <div className="absolute -right-16 -top-16 w-72 h-72 rounded-full bg-white/10 pointer-events-none" />
        <div className="absolute -left-10 -bottom-10 w-64 h-64 rounded-full bg-white/10 pointer-events-none" />
        <div className="absolute right-8 bottom-0 text-[120px] leading-none opacity-20 select-none hidden md:block">
          🛍️
        </div>

        <div className="relative px-8 py-12 max-w-lg">
          <span className="inline-block bg-accent text-gray-900 text-xs font-bold px-3 py-1 rounded-full mb-4 uppercase tracking-widest">
            Millions of items
          </span>
          <h1 className="text-3xl md:text-5xl font-extrabold text-white leading-tight mb-3 drop-shadow-sm">
            Buy &amp; Sell<br />
            <span className="text-accent">Anything</span>
          </h1>
          <p className="text-blue-100 text-sm md:text-base mb-8 max-w-xs">
            Great prices. Trusted sellers. Your next great find is just a search away.
          </p>
          <div className="flex flex-wrap gap-3">
            <Link
              to={ROUTES.LISTINGS}
              className="inline-flex items-center gap-2 bg-white text-primary font-bold px-5 py-2.5 rounded-xl hover:bg-blue-50 transition-colors shadow text-sm"
            >
              Start Shopping <ChevronRight size={15} />
            </Link>
            <Link
              to={ROUTES.CREATE_LISTING}
              className="inline-flex items-center gap-2 bg-white/20 border border-white/40 text-white font-semibold px-5 py-2.5 rounded-xl hover:bg-white/30 transition-colors text-sm"
            >
              Start Selling
            </Link>
          </div>
        </div>
      </div>

      {/* ── Trust Badges ── */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {TRUST_BADGES.map(({ icon: Icon, title, desc }) => (
          <div key={title} className="card p-4 flex items-center gap-4 hover:shadow-md transition-shadow">
            <div className="p-3 bg-primary/10 rounded-xl shrink-0">
              <Icon size={22} className="text-primary" />
            </div>
            <div>
              <p className="text-sm font-bold text-gray-900">{title}</p>
              <p className="text-xs text-gray-500 mt-0.5">{desc}</p>
            </div>
          </div>
        ))}
      </div>

      {/* ── Shop by Category ── */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-bold text-gray-900">Shop by Category</h2>
          <Link
            to={ROUTES.LISTINGS}
            className="text-sm text-primary hover:text-primary-700 font-medium flex items-center gap-1 transition-colors"
          >
            See all <ChevronRight size={14} />
          </Link>
        </div>

        <div className="grid grid-cols-4 sm:grid-cols-8 gap-2.5">
          {CATEGORIES.map((cat) => (
            <Link
              key={cat.value}
              to={`${ROUTES.LISTINGS}?category=${cat.value}`}
              className={`card p-3 flex flex-col items-center gap-2 hover:shadow-md border transition-all text-center ${cat.bg}`}
            >
              <span className="text-3xl">{cat.emoji}</span>
              <span className="text-xs font-semibold text-gray-700 leading-tight">{cat.label}</span>
            </Link>
          ))}
        </div>
      </section>

      {isAuthenticated && recentViewedItems.length > 0 && (
        <section>
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-xl font-bold text-gray-900">Recently Viewed</h2>
              <p className="text-sm text-gray-500 mt-0.5">Items you opened recently</p>
            </div>
            <Link
              to={ROUTES.LISTINGS}
              className="text-sm text-primary hover:text-primary-700 font-medium flex items-center gap-1 transition-colors"
            >
              See all <ChevronRight size={14} />
            </Link>
          </div>

          <div className="flex gap-4 overflow-x-auto pb-2">
            {recentViewedItems.map((listing) => (
              <div key={listing.id} className="w-[180px] shrink-0 sm:w-[210px]">
                <ListingCard listing={listing} />
              </div>
            ))}
          </div>
        </section>
      )}

      {/* ── Live Auctions ── */}
      {liveAuctions.length > 0 && (
        <section>
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <Gavel size={20} className="text-amber-600" />
              <div>
                <h2 className="text-xl font-bold text-gray-900">Live Auctions</h2>
                <p className="text-sm text-gray-500 mt-0.5">Bid now before time runs out</p>
              </div>
            </div>
          </div>

          <div className="flex gap-4 overflow-x-auto pb-2 scrollbar-thin">
            {liveAuctions.map((auction) => (
              <Link
                key={auction.listingId}
                to={buildRoute(ROUTES.LISTING_DETAIL, { id: auction.listingId })}
                className="w-[180px] shrink-0 sm:w-[210px] card hover:shadow-md transition-all duration-200 flex flex-col overflow-hidden group"
              >
                <div className="relative aspect-square bg-gray-100 overflow-hidden">
                  <div className="w-full h-full flex items-center justify-center text-gray-300">
                    <Gavel size={36} strokeWidth={1.25} />
                  </div>
                  <div className="absolute top-2 left-2">
                    <AuctionBadge listing={{ listingType: 1, auctionEndAt: auction.auctionEndAt, status: auction.status }} />
                  </div>
                </div>
                <div className="px-3 pb-3 pt-2 flex flex-col flex-1">
                  <p className="text-xs text-gray-400 mb-0.5 truncate">{auction.bidCount} bid{auction.bidCount !== 1 ? 's' : ''}</p>
                  <p className="text-sm text-gray-800 font-medium line-clamp-2 leading-snug mb-auto">{auction.title}</p>
                  <div className="mt-2">
                    <p className="text-lg font-bold leading-none text-gray-900">
                      {auction.currentBidAmount
                        ? formatCurrency(auction.currentBidAmount)
                        : auction.startingBid
                        ? `Start ${formatCurrency(auction.startingBid)}`
                        : 'No bids'}
                    </p>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* ── Featured Listings ── */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-xl font-bold text-gray-900">Featured Items</h2>
            <p className="text-sm text-gray-500 mt-0.5">Handpicked deals just for you</p>
          </div>
          <Link
            to={ROUTES.LISTINGS}
            className="text-sm text-primary hover:text-primary-700 font-medium flex items-center gap-1 transition-colors"
          >
            See all <ChevronRight size={14} />
          </Link>
        </div>
        <ListingGrid data={data?.data} isLoading={isLoading} onPageChange={setPage} />
      </section>

      {/* ── Promo Banner ── */}
      <div className="rounded-2xl bg-gradient-to-r from-secondary to-red-400 p-8 flex flex-col sm:flex-row items-center justify-between gap-6 overflow-hidden relative">
        <div className="absolute -right-8 -top-8 w-40 h-40 rounded-full bg-white/10 pointer-events-none" />
        <div>
          <p className="text-white/80 text-sm font-medium mb-1 uppercase tracking-wide">Limited Time</p>
          <h3 className="text-2xl font-extrabold text-white">Today's Hot Deals</h3>
          <p className="text-red-100 text-sm mt-1">Fresh listings added every hour</p>
        </div>
        <Link
          to={`${ROUTES.LISTINGS}?sortBy=updatedAt&sortDirection=desc`}
          className="shrink-0 bg-white text-secondary font-bold px-6 py-3 rounded-xl hover:bg-red-50 transition-colors shadow text-sm"
        >
          Shop Now →
        </Link>
      </div>
    </div>
  )
}
