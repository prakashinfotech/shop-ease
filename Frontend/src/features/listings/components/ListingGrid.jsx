import ListingCard from './ListingCard'
import Pagination from '@/components/common/Pagination'

function SkeletonCard() {
  return (
    <div className="card overflow-hidden animate-pulse">
      <div className="aspect-square bg-gray-200" />
      <div className="p-3 space-y-2">
        <div className="h-2.5 bg-gray-200 rounded w-1/2" />
        <div className="h-3.5 bg-gray-200 rounded" />
        <div className="h-3.5 bg-gray-200 rounded w-3/4" />
        <div className="h-5 bg-gray-200 rounded w-1/3 mt-2" />
      </div>
    </div>
  )
}

export default function ListingGrid({ data, isLoading, onPageChange }) {
  if (isLoading) {
    return (
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5 gap-4">
        {Array.from({ length: 15 }).map((_, i) => (
          <SkeletonCard key={i} />
        ))}
      </div>
    )
  }

  if (!data?.items?.length) {
    return (
      <div className="text-center py-20">
        <p className="text-5xl mb-4">🔍</p>
        <p className="text-lg font-semibold text-gray-700">No listings found</p>
        <p className="text-sm text-gray-500 mt-1">Try adjusting your filters or search terms</p>
      </div>
    )
  }

  return (
    <>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5 gap-4">
        {data.items.map((listing) => (
          <ListingCard key={listing.id} listing={listing} />
        ))}
      </div>
      <Pagination
        page={data.page}
        totalPages={data.totalPages}
        onPageChange={onPageChange}
      />
    </>
  )
}