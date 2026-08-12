import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  ArrowRight,
  Heart,
  Search,
  ShoppingCart,
  SlidersHorizontal,
  Trash2,
  X,
} from 'lucide-react'
import { useWishlistStore } from '@/store/wishlistStore'
import { useCartStore } from '@/store/cartStore'
import Button from '@/components/common/Button'
import Pagination from '@/components/common/Pagination'
import { formatCurrency } from '@/utils/formatters'
import { assetUrl } from '@/utils/assets'
import { buildRoute, ROUTES } from '@/constants/routes'
import {
  filterAndSortWatchlistItems,
  getWatchlistCategories,
  paginateItems,
  PAGE_SIZE_OPTIONS,
  SORT_OPTIONS,
} from '../utils/watchlistFilters'
import toast from 'react-hot-toast'

export default function WishlistPage() {
  const { items, removeItem, clearWishlist } = useWishlistStore()
  const addToCart = useCartStore((s) => s.addItem)

  const [query, setQuery] = useState('')
  const [category, setCategory] = useState('')
  const [minPrice, setMinPrice] = useState('')
  const [maxPrice, setMaxPrice] = useState('')
  const [freeShippingOnly, setFreeShippingOnly] = useState(false)
  const [sortKey, setSortKey] = useState('saved_desc')
  const [pageSize, setPageSize] = useState(12)
  const [page, setPage] = useState(1)
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false)

  const categories = useMemo(() => getWatchlistCategories(items), [items])

  const filteredItems = useMemo(() => filterAndSortWatchlistItems({
    items,
    query,
    category,
    minPrice,
    maxPrice,
    freeShippingOnly,
    sortKey,
  }), [category, freeShippingOnly, items, maxPrice, minPrice, query, sortKey])

  const { currentPage, totalPages, visibleItems } = useMemo(
    () => paginateItems(filteredItems, page, pageSize),
    [filteredItems, page, pageSize],
  )

  const activeFilterCount = [
    query.trim(),
    category,
    minPrice,
    maxPrice,
    freeShippingOnly,
  ].filter(Boolean).length

  useEffect(() => {
    setPage(1)
  }, [category, freeShippingOnly, maxPrice, minPrice, pageSize, query, sortKey])

  useEffect(() => {
    if (page > totalPages) setPage(totalPages)
  }, [page, totalPages])

  const handleMoveToCart = (listing) => {
    addToCart(listing)
    removeItem(listing.id)
    toast.success('Moved to cart!')
  }

  const handleRemove = (listingId) => {
    removeItem(listingId)
    toast('Removed from watchlist')
  }

  const handleClearFilters = () => {
    setQuery('')
    setCategory('')
    setMinPrice('')
    setMaxPrice('')
    setFreeShippingOnly(false)
  }

  const handlePageChange = (nextPage) => {
    setPage(nextPage)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  if (!items.length) {
    return (
      <div className="text-center py-24 max-w-sm mx-auto">
        <div className="w-20 h-20 bg-red-50 rounded-full flex items-center justify-center mx-auto mb-5">
          <Heart size={36} className="text-red-300" />
        </div>
        <h2 className="text-xl font-bold text-gray-800 mb-2">Your watchlist is empty</h2>
        <p className="text-gray-500 text-sm mb-8">
          Click the heart icon on any listing to save it here for later.
        </p>
        <Link to={ROUTES.LISTINGS}>
          <Button size="lg" className="gap-2">
            Browse Listings <ArrowRight size={16} />
          </Button>
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-6xl mx-auto space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Watchlist</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            {items.length} saved {items.length === 1 ? 'item' : 'items'}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setMobileFiltersOpen(true)}
            className="lg:hidden flex items-center gap-2 px-3 py-2 border border-gray-300 rounded-md text-sm text-gray-700 bg-white hover:border-primary hover:text-primary transition-colors"
          >
            <SlidersHorizontal size={15} />
            Filters
            {activeFilterCount > 0 && (
              <span className="bg-primary text-white text-xs w-5 h-5 rounded-full flex items-center justify-center font-bold">
                {activeFilterCount}
              </span>
            )}
          </button>
          <button
            onClick={() => { clearWishlist(); toast('Watchlist cleared') }}
            className="text-sm text-red-400 hover:text-red-600 transition-colors"
          >
            Clear All
          </button>
        </div>
      </div>

      <div className="grid lg:grid-cols-[240px_minmax(0,1fr)] gap-6">
        <aside className="hidden lg:block">
          <WatchlistFilters
            categories={categories}
            query={query}
            setQuery={setQuery}
            category={category}
            setCategory={setCategory}
            minPrice={minPrice}
            setMinPrice={setMinPrice}
            maxPrice={maxPrice}
            setMaxPrice={setMaxPrice}
            freeShippingOnly={freeShippingOnly}
            setFreeShippingOnly={setFreeShippingOnly}
            onClear={handleClearFilters}
            activeFilterCount={activeFilterCount}
          />
        </aside>

        <section className="min-w-0 space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3 border border-gray-200 bg-white rounded-lg px-4 py-3">
            <p className="text-sm text-gray-600">
              Showing <span className="font-semibold text-gray-900">{visibleItems.length}</span> of{' '}
              <span className="font-semibold text-gray-900">{filteredItems.length}</span> saved items
            </p>
            <div className="flex flex-wrap items-center gap-3">
              <label className="flex items-center gap-2 text-sm text-gray-600">
                Sort
                <select
                  value={sortKey}
                  onChange={(e) => setSortKey(e.target.value)}
                  className="form-input bg-white w-44 py-1.5"
                >
                  {SORT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>{option.label}</option>
                  ))}
                </select>
              </label>
              <label className="flex items-center gap-2 text-sm text-gray-600">
                Per page
                <select
                  value={pageSize}
                  onChange={(e) => setPageSize(Number(e.target.value))}
                  className="form-input bg-white w-20 py-1.5"
                >
                  {PAGE_SIZE_OPTIONS.map((size) => (
                    <option key={size} value={size}>{size}</option>
                  ))}
                </select>
              </label>
            </div>
          </div>

          {visibleItems.length ? (
            <>
              <div className="grid sm:grid-cols-2 xl:grid-cols-3 gap-4">
                {visibleItems.map((listing) => (
                  <WatchlistCard
                    key={listing.id}
                    listing={listing}
                    onMoveToCart={handleMoveToCart}
                    onRemove={handleRemove}
                  />
                ))}
              </div>
              <Pagination page={currentPage} totalPages={totalPages} onPageChange={handlePageChange} />
            </>
          ) : (
            <div className="border border-dashed border-gray-300 rounded-lg bg-white text-center py-16 px-4">
              <Search size={30} className="mx-auto text-gray-300 mb-3" />
              <h2 className="text-lg font-semibold text-gray-900">No saved items match your filters</h2>
              <p className="text-sm text-gray-500 mt-1 mb-5">Try changing your search, category, or price range.</p>
              <Button variant="secondary" onClick={handleClearFilters}>
                Clear Filters
              </Button>
            </div>
          )}
        </section>
      </div>

      {mobileFiltersOpen && (
        <>
          <div
            className="fixed inset-0 bg-black/50 z-50 lg:hidden"
            onClick={() => setMobileFiltersOpen(false)}
          />
          <div className="fixed bottom-0 inset-x-0 bg-white z-50 rounded-t-2xl shadow-2xl max-h-[88vh] flex flex-col lg:hidden">
            <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100 shrink-0">
              <h3 className="font-semibold text-gray-900 flex items-center gap-2">
                <SlidersHorizontal size={16} /> Filters
              </h3>
              <button
                onClick={() => setMobileFiltersOpen(false)}
                className="p-1 text-gray-400 hover:text-gray-700"
                aria-label="Close filters"
              >
                <X size={20} />
              </button>
            </div>
            <div className="overflow-y-auto flex-1 p-5">
              <WatchlistFilters
                categories={categories}
                query={query}
                setQuery={setQuery}
                category={category}
                setCategory={setCategory}
                minPrice={minPrice}
                setMinPrice={setMinPrice}
                maxPrice={maxPrice}
                setMaxPrice={setMaxPrice}
                freeShippingOnly={freeShippingOnly}
                setFreeShippingOnly={setFreeShippingOnly}
                onClear={handleClearFilters}
                activeFilterCount={activeFilterCount}
              />
            </div>
            <div className="p-4 border-t border-gray-100 shrink-0">
              <button
                onClick={() => setMobileFiltersOpen(false)}
                className="w-full bg-primary text-white py-3 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
              >
                Show {filteredItems.length} {filteredItems.length === 1 ? 'item' : 'items'}
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

function WatchlistFilters({
  categories,
  query,
  setQuery,
  category,
  setCategory,
  minPrice,
  setMinPrice,
  maxPrice,
  setMaxPrice,
  freeShippingOnly,
  setFreeShippingOnly,
  onClear,
  activeFilterCount,
}) {
  return (
    <div className="border border-gray-200 bg-white rounded-lg p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-semibold text-gray-900 flex items-center gap-2">
          <SlidersHorizontal size={15} /> Filters
        </h2>
        {activeFilterCount > 0 && (
          <button
            onClick={onClear}
            className="text-xs text-red-400 hover:text-red-600 transition-colors"
          >
            Reset ({activeFilterCount})
          </button>
        )}
      </div>

      <div>
        <label className="form-label">Search saved items</label>
        <div className="relative">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Title or category"
            className="form-input pl-9"
          />
        </div>
      </div>

      <div>
        <label className="form-label">Category</label>
        <select
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          className="form-input bg-white"
        >
          <option value="">All categories</option>
          {categories.map((name) => (
            <option key={name} value={name}>{name}</option>
          ))}
        </select>
      </div>

      <div>
        <label className="form-label">Price Range</label>
        <div className="flex items-center gap-2">
          <input
            type="number"
            min="0"
            value={minPrice}
            onChange={(e) => setMinPrice(e.target.value)}
            placeholder="Min"
            className="form-input text-sm"
          />
          <span className="text-gray-400">to</span>
          <input
            type="number"
            min="0"
            value={maxPrice}
            onChange={(e) => setMaxPrice(e.target.value)}
            placeholder="Max"
            className="form-input text-sm"
          />
        </div>
      </div>

      <label className="flex items-center gap-2.5 cursor-pointer text-sm text-gray-700 hover:text-primary">
        <input
          type="checkbox"
          checked={freeShippingOnly}
          onChange={(e) => setFreeShippingOnly(e.target.checked)}
          className="rounded border-gray-300 text-primary focus:ring-primary"
        />
        Free shipping only
      </label>

      <button
        onClick={onClear}
        className="w-full text-xs text-gray-400 hover:text-red-500 transition-colors pt-1"
      >
        Clear all filters
      </button>
    </div>
  )
}

function WatchlistCard({ listing, onMoveToCart, onRemove }) {
  const finalPrice = listing.finalPrice ?? listing.price
  const hasDiscount = Number(listing.discountAmount || 0) > 0 && finalPrice < listing.price

  return (
    <div className="card flex flex-col group overflow-hidden">
      <Link
        to={buildRoute(ROUTES.LISTING_DETAIL, { id: listing.id })}
        className="relative aspect-square overflow-hidden bg-gray-100 block"
      >
        {listing.primaryImageUrl ? (
          <img
            src={assetUrl(listing.primaryImageUrl)}
            alt={listing.title}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            loading="lazy"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-sm text-gray-400">
            No image
          </div>
        )}
        <button
          onClick={(e) => { e.preventDefault(); onRemove(listing.id) }}
          className="absolute top-2 right-2 p-1.5 bg-white/90 rounded-full text-secondary shadow-sm hover:bg-red-50 transition-colors"
          title="Remove from watchlist"
          aria-label="Remove from watchlist"
        >
          <Heart size={15} fill="currentColor" />
        </button>
        {listing.freeShipping && (
          <span className="absolute bottom-2 left-2 text-xs bg-success text-white px-1.5 py-0.5 rounded font-medium leading-none">
            Free Ship
          </span>
        )}
      </Link>

      <div className="p-4 flex flex-col flex-1">
        {listing.categoryName && (
          <p className="text-xs text-gray-400 mb-0.5 truncate">{listing.categoryName}</p>
        )}
        <Link
          to={buildRoute(ROUTES.LISTING_DETAIL, { id: listing.id })}
          className="text-sm font-medium text-gray-900 hover:text-primary line-clamp-2 leading-snug mb-2 transition-colors"
        >
          {listing.title}
        </Link>
        <div className="mb-4 flex items-baseline gap-2">
          <p className="text-xl font-bold leading-none text-gray-900">{formatCurrency(finalPrice)}</p>
          {hasDiscount && (
            <p className="text-sm leading-none text-gray-400 line-through">{formatCurrency(listing.price)}</p>
          )}
        </div>

        <div className="mt-auto flex gap-2">
          <button
            onClick={() => onMoveToCart(listing)}
            className="flex-1 flex items-center justify-center gap-1.5 btn-primary text-sm py-2 rounded-md"
          >
            <ShoppingCart size={14} />
            Add to Cart
          </button>
          <button
            onClick={() => onRemove(listing.id)}
            className="p-2 border border-gray-200 rounded-md text-gray-400 hover:text-red-500 hover:border-red-200 transition-colors"
            title="Remove"
            aria-label="Remove"
          >
            <Trash2 size={15} />
          </button>
        </div>
      </div>
    </div>
  )
}
