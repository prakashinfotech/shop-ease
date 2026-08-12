import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  Eye,
  Pencil,
  Plus,
  RotateCcw,
  Search,
  SlidersHorizontal,
  Trash2,
} from 'lucide-react'
import { useMyListings, useDeleteListing, useRestoreListing } from '@/features/listings/hooks/useListings'
import Badge from '@/components/common/Badge'
import Button from '@/components/common/Button'
import Spinner from '@/components/common/Spinner'
import Pagination from '@/components/common/Pagination'
import { formatCurrency, formatDate } from '@/utils/formatters'
import { assetUrl } from '@/utils/assets'
import { ListingStatus, ListingStatusLabel, ListingType, ListingTypeLabel } from '@/constants/enums'
import { buildRoute, ROUTES } from '@/constants/routes'
import { useDebounce } from '@/hooks/useDebounce'

const statusVariant = {
  [ListingStatus.DRAFT]: 'default',
  [ListingStatus.ACTIVE]: 'success',
  [ListingStatus.SOLD]: 'primary',
  [ListingStatus.ENDED]: 'warning',
  [ListingStatus.REMOVED]: 'danger',
}

const sortOptions = [
  { value: 'updatedAt_desc', label: 'Recently updated' },
  { value: 'createdAt_desc', label: 'Newest listed' },
  { value: 'createdAt_asc', label: 'Oldest first' },
  { value: 'price_desc', label: 'Price: high to low' },
  { value: 'price_asc', label: 'Price: low to high' },
  { value: 'title_asc', label: 'Title: A to Z' },
  { value: 'title_desc', label: 'Title: Z to A' },
]

const getDiscountPercentage = (listing) => {
  const price = Number(listing.price || 0)
  const discount = Number(listing.discountAmount || 0)
  if (!price || discount <= 0) return 0
  return Math.round((discount / price) * 100)
}

function ListingImage({ listing }) {
  const finalPrice = listing.finalPrice ?? listing.price
  const hasDiscount = Number(listing.discountAmount || 0) > 0 && finalPrice < listing.price
  const discountPercentage = getDiscountPercentage(listing)

  return (
    <div className="relative aspect-square overflow-hidden bg-gray-100">
      {hasDiscount && discountPercentage > 0 && (
        <span className="absolute left-2 top-2 z-10 rounded bg-secondary px-2 py-1 text-xs font-bold leading-none text-white shadow-sm">
          {discountPercentage}% off
        </span>
      )}
      {listing.primaryImageUrl ? (
        <img
          src={assetUrl(listing.primaryImageUrl)}
          alt={listing.title}
          className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
          loading="lazy"
        />
      ) : (
        <div className="flex h-full w-full items-center justify-center text-sm text-gray-400">
          No image
        </div>
      )}
      {listing.freeShipping && (
        <span className="absolute left-2 bottom-2 rounded bg-[#f68b1e] px-1.5 py-0.5 text-[10px] font-bold uppercase leading-none text-white shadow-sm">
          Free Ship
        </span>
      )}
    </div>
  )
}

function SellerListingCard({ listing, onDelete, onRestore }) {
  const isDeleted = listing.isDeleted
  const finalPrice = listing.finalPrice ?? listing.price
  const hasDiscount = Number(listing.discountAmount || 0) > 0 && finalPrice < listing.price
  const discountPercentage = getDiscountPercentage(listing)

  return (
    <article className="card group flex flex-col overflow-hidden transition-all duration-200 hover:shadow-md">
      <Link to={buildRoute(ROUTES.LISTING_DETAIL, { id: listing.id })} className="block">
        <ListingImage listing={listing} />
      </Link>

      <div className="flex flex-1 flex-col p-3">
        <div className="mb-2 flex items-start justify-between gap-2">
          <Badge variant={isDeleted ? 'danger' : statusVariant[listing.status]}>
            {isDeleted ? 'Deleted' : ListingStatusLabel[listing.status]}
          </Badge>
          <span className="shrink-0 text-xs text-gray-400">{formatDate(listing.createdAt)}</span>
        </div>

        <Link to={buildRoute(ROUTES.LISTING_DETAIL, { id: listing.id })}>
          <h2 className="line-clamp-2 text-sm font-semibold leading-snug text-gray-900 hover:text-primary">
            {listing.title}
          </h2>
        </Link>
        <p className="mt-1 truncate text-xs text-gray-400">{listing.categoryName || 'Uncategorized'}</p>

        <div className="mt-3 flex items-end justify-between gap-2">
          <div>
            <div className="flex items-baseline gap-2">
              <p className="text-lg font-bold leading-none text-gray-900">{formatCurrency(finalPrice)}</p>
              {hasDiscount && (
                <p className="text-xs leading-none text-gray-400 line-through">{formatCurrency(listing.price)}</p>
              )}
            </div>
            {hasDiscount && discountPercentage > 0 && (
              <p className="mt-1 text-xs font-semibold text-green-700">{discountPercentage}% discount</p>
            )}
            <p className="text-xs text-gray-500">{ListingTypeLabel[listing.listingType]}</p>
          </div>
        </div>

        <div className="mt-4 grid grid-cols-3 gap-2 border-t border-gray-100 pt-3">
          <Link
            to={buildRoute(ROUTES.LISTING_DETAIL, { id: listing.id })}
            className="inline-flex h-9 items-center justify-center rounded-md border border-gray-200 text-gray-500 transition-colors hover:border-primary hover:text-primary"
            title="View listing"
          >
            <Eye size={16} />
          </Link>
          {isDeleted ? (
            <button
              type="button"
              onClick={() => onRestore(listing.id)}
              className="col-span-2 inline-flex h-9 items-center justify-center gap-2 rounded-md border border-gray-200 text-sm font-medium text-gray-600 transition-colors hover:border-primary hover:text-primary"
            >
              <RotateCcw size={15} />
              Restore
            </button>
          ) : (
            <>
              <Link
                to={buildRoute(ROUTES.EDIT_LISTING, { id: listing.id })}
                className="inline-flex h-9 items-center justify-center rounded-md border border-gray-200 text-gray-500 transition-colors hover:border-primary hover:text-primary"
                title="Edit listing"
              >
                <Pencil size={16} />
              </Link>
              <button
                type="button"
                onClick={() => {
                  if (confirm('Delete this listing?')) onDelete(listing.id)
                }}
                className="inline-flex h-9 items-center justify-center rounded-md border border-gray-200 text-gray-500 transition-colors hover:border-red-300 hover:bg-red-50 hover:text-red-600"
                title="Delete listing"
              >
                <Trash2 size={16} />
              </button>
            </>
          )}
        </div>
      </div>
    </article>
  )
}

export default function MyListingsPage() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(12)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [listingType, setListingType] = useState('')
  const [freeShipping, setFreeShipping] = useState('')
  const [includeDeleted, setIncludeDeleted] = useState(false)
  const [sortKey, setSortKey] = useState('updatedAt_desc')
  const debouncedSearch = useDebounce(search, 350)
  const [sortBy, sortDirection] = sortKey.split('_')

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, status, listingType, freeShipping, includeDeleted, sortKey, pageSize])

  const queryParams = useMemo(() => ({
    page,
    pageSize,
    includeDeleted,
    search: debouncedSearch || undefined,
    status: status !== '' ? Number(status) : undefined,
    listingType: listingType !== '' ? Number(listingType) : undefined,
    freeShipping: freeShipping !== '' ? freeShipping === 'true' : undefined,
    sortBy,
    sortDirection,
  }), [page, pageSize, includeDeleted, debouncedSearch, status, listingType, freeShipping, sortBy, sortDirection])

  const { data, isLoading, isFetching } = useMyListings(queryParams)
  const { mutate: deleteListing } = useDeleteListing()
  const { mutate: restoreListing } = useRestoreListing()

  const listings = data?.data?.items || []
  const totalCount = data?.data?.totalCount ?? 0
  const totalPages = data?.data?.totalPages || 1

  const resetFilters = () => {
    setSearch('')
    setStatus('')
    setListingType('')
    setFreeShipping('')
    setIncludeDeleted(false)
    setSortKey('updatedAt_desc')
    setPageSize(12)
    setPage(1)
  }

  const activeFilterCount = [
    debouncedSearch,
    status,
    listingType,
    freeShipping,
    includeDeleted,
    sortKey !== 'updatedAt_desc',
    pageSize !== 12,
  ].filter(Boolean).length

  return (
    <div className="mx-auto max-w-7xl space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">My Listings</h1>
          <p className="text-sm text-gray-500">
            {isLoading ? 'Loading your listings...' : `${totalCount.toLocaleString()} listing${totalCount === 1 ? '' : 's'}`}
            {isFetching && !isLoading ? ' · Refreshing' : ''}
          </p>
        </div>
        <Link to={ROUTES.CREATE_LISTING}>
          <Button><Plus size={16} /> New Listing</Button>
        </Link>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <div className="mb-3 flex items-center justify-between gap-3">
          <h2 className="flex items-center gap-2 text-sm font-semibold text-gray-900">
            <SlidersHorizontal size={15} />
            Filters
          </h2>
          {activeFilterCount > 0 && (
            <button
              type="button"
              onClick={resetFilters}
              className="text-xs font-medium text-red-500 hover:text-red-600"
            >
              Reset ({activeFilterCount})
            </button>
          )}
        </div>

        <div className="grid gap-3 md:grid-cols-[minmax(220px,1fr)_160px_160px_150px_160px_120px]">
          <label className="relative block">
            <Search size={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search your listings"
              className="form-input h-10 pl-9 text-sm"
            />
          </label>

          <select value={status} onChange={(e) => setStatus(e.target.value)} className="form-input h-10 bg-white text-sm">
            <option value="">All statuses</option>
            {Object.values(ListingStatus).map((value) => (
              <option key={value} value={value}>{ListingStatusLabel[value]}</option>
            ))}
          </select>

          <select value={listingType} onChange={(e) => setListingType(e.target.value)} className="form-input h-10 bg-white text-sm">
            <option value="">All types</option>
            {Object.values(ListingType).map((value) => (
              <option key={value} value={value}>{ListingTypeLabel[value]}</option>
            ))}
          </select>

          <select value={freeShipping} onChange={(e) => setFreeShipping(e.target.value)} className="form-input h-10 bg-white text-sm">
            <option value="">Shipping</option>
            <option value="true">Free shipping</option>
            <option value="false">Paid shipping</option>
          </select>

          <select value={sortKey} onChange={(e) => setSortKey(e.target.value)} className="form-input h-10 bg-white text-sm">
            {sortOptions.map((option) => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </select>

          <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))} className="form-input h-10 bg-white text-sm">
            {[12, 24, 48].map((value) => (
              <option key={value} value={value}>{value} / page</option>
            ))}
          </select>
        </div>

        <label className="mt-3 inline-flex items-center gap-2 text-sm text-gray-600">
          <input
            type="checkbox"
            checked={includeDeleted}
            onChange={(e) => setIncludeDeleted(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-primary"
          />
          Show deleted listings
        </label>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner size="lg" /></div>
      ) : !listings.length ? (
        <div className="rounded-lg border border-gray-200 bg-white p-12 text-center shadow-sm">
          <p className="text-lg font-semibold text-gray-700">No listings found</p>
          <p className="mt-1 text-sm text-gray-500">
            {activeFilterCount > 0 ? 'Try changing your filters.' : 'Start selling by creating your first listing.'}
          </p>
          <div className="mt-5">
            {activeFilterCount > 0 ? (
              <Button variant="secondary" onClick={resetFilters}>Clear Filters</Button>
            ) : (
              <Link to={ROUTES.CREATE_LISTING}><Button>Create Listing</Button></Link>
            )}
          </div>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {listings.map((listing) => (
              <SellerListingCard
                key={listing.id}
                listing={listing}
                onDelete={deleteListing}
                onRestore={restoreListing}
              />
            ))}
          </div>
          <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </>
      )}
    </div>
  )
}
