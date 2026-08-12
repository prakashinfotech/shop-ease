import { useState, useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { SlidersHorizontal, X } from 'lucide-react'
import { useListings, useSearchFacets } from '../hooks/useListings'
import { useDebounce } from '@/hooks/useDebounce'
import ListingGrid from '../components/ListingGrid'
import FilterSidebar from '../components/FilterSidebar'
import CategoryBrowserGrid from '../components/CategoryBrowserGrid'
import Breadcrumbs from '@/components/common/Breadcrumbs'
import { ROUTES } from '@/constants/routes'
import { categoryService } from '@/features/categories/services/categoryService'
import { useAuthStore } from '@/store/authStore'

function findInTree(cats, id) {
  for (const cat of cats) {
    if (cat.id === id) return cat
    if (cat.children?.length) {
      const found = findInTree(cat.children, id)
      if (found) return found
    }
  }
  return null
}

function getCategoryPath(cats, id, path = []) {
  for (const cat of cats) {
    const next = [...path, { id: cat.id, name: cat.name }]
    if (cat.id === id) return next
    if (cat.children?.length) {
      const found = getCategoryPath(cat.children, id, next)
      if (found) return found
    }
  }
  return null
}

export default function ListingsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const user = useAuthStore((state) => state.user)
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)

  const q = searchParams.get('q') || ''
  const categoryId = searchParams.get('categoryId') || ''
  const categoryName = searchParams.get('category') || ''

  const [page, setPage] = useState(1)
  const [minPrice, setMinPrice] = useState('')
  const [maxPrice, setMaxPrice] = useState('')
  const [sortKey, setSortKey] = useState('updatedAt_desc')
  const [freeShipping, setFreeShipping] = useState(false)
  const [listingType, setListingType] = useState('0')
  const [attributeFilters, setAttributeFilters] = useState({})
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false)

  const debouncedQ = useDebounce(q, 400)
  const [sortBy, sortDir] = sortKey.split('_')

  // ── Category tree ─────────────────────────────────────────────────────
  const { data: categoryTree = [] } = useQuery({
    queryKey: ['categories', 'tree'],
    queryFn: categoryService.getTree,
    staleTime: 5 * 60_000,
    select: (res) => res?.data ?? [],
  })

  const selectedNode = categoryId ? findInTree(categoryTree, categoryId) : null
  const subCategories = selectedNode?.children ?? []
  const isBrowsingCategory = !!categoryId && !q && subCategories.length > 0
  const categoryPath = categoryId ? (getCategoryPath(categoryTree, categoryId) ?? []) : []

  // ── Listings hooks (always called — disabled when browsing categories) ─
  const listingParams = {
    page,
    pageSize: 25,
    status: 1,
    search: debouncedQ || undefined,
    categoryId: categoryId || undefined,
    category: !categoryId && categoryName ? categoryName : undefined,
    minPrice: minPrice || undefined,
    maxPrice: maxPrice || undefined,
    freeShipping: freeShipping || undefined,
    listingType: listingType !== '' ? Number(listingType) : undefined,
    excludeSellerId: isAuthenticated ? user?.id : undefined,
    sortBy,
    sortDirection: sortDir,
    attributeFilters: Object.keys(attributeFilters).length > 0 ? attributeFilters : undefined,
  }

  // Pass null when browsing — hook's enabled:false guard skips the fetch
  const { data, isLoading } = useListings(isBrowsingCategory ? null : listingParams)

  const { data: facets } = useSearchFacets({
    categoryId: categoryId || undefined,
    search: debouncedQ || undefined,
  })

  // ── Handlers ──────────────────────────────────────────────────────────
  const handleCategoryChange = useCallback(
    (id, name) => {
      const next = new URLSearchParams(searchParams)
      if (id) {
        next.set('categoryId', id)
        next.set('category', name)
      } else {
        next.delete('categoryId')
        next.delete('category')
      }
      setSearchParams(next)
      setAttributeFilters({})
      setPage(1)
    },
    [searchParams, setSearchParams],
  )

  const handlePageChange = (p) => {
    setPage(p)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const handleReset = () => {
    setMinPrice('')
    setMaxPrice('')
    setSortKey('updatedAt_desc')
    setFreeShipping(false)
    setListingType('')
    setAttributeFilters({})
    setPage(1)
    const next = new URLSearchParams(searchParams)
    next.delete('categoryId')
    next.delete('category')
    setSearchParams(next)
  }

  // ── Shared derived values ──────────────────────────────────────────────
  const breadcrumbs = categoryPath.length > 0
    ? categoryPath.map((cat, i) => {
        const isLast = i === categoryPath.length - 1
        return {
          label: cat.name,
          to: isLast
            ? undefined
            : `${ROUTES.LISTINGS}?categoryId=${cat.id}&category=${encodeURIComponent(cat.name)}`,
        }
      })
    : q
      ? [{ label: 'All Listings', to: ROUTES.LISTINGS }, { label: `"${q}"` }]
      : [{ label: 'All Listings' }]

  const pageTitle = q && selectedNode
    ? `Results for "${q}" in ${selectedNode.name}`
    : selectedNode?.name
      ?? (categoryName ? decodeURIComponent(categoryName) : null)
      ?? (q ? `Results for "${q}"` : 'All Listings')

  // ── Category browser mode ─────────────────────────────────────────────
  if (isBrowsingCategory) {
    return (
      <div>
        <div className="mb-4">
          <Breadcrumbs items={breadcrumbs} />
        </div>
        <div className="mb-6">
          <h1 className="text-xl font-bold text-gray-900">{pageTitle}</h1>
          <p className="text-sm text-gray-500 mt-0.5">Select a category to continue browsing</p>
        </div>
        <CategoryBrowserGrid categories={subCategories} onSelect={handleCategoryChange} />
      </div>
    )
  }

  // ── Listings mode ─────────────────────────────────────────────────────
  const activeFilterCount = [
    minPrice,
    maxPrice,
    freeShipping,
    listingType,
    categoryId,
    ...Object.keys(attributeFilters),
  ].filter(Boolean).length

  const filterProps = {
    categoryId,
    onCategoryChange: handleCategoryChange,
    minPrice, setMinPrice,
    maxPrice, setMaxPrice,
    listingType, setListingType,
    freeShipping, setFreeShipping,
    attributeFilters, setAttributeFilters,
    facets,
    sortKey, setSortKey,
    onReset: handleReset,
    setPage,
  }

  return (
    <div>
      <div className="mb-4">
        <Breadcrumbs items={breadcrumbs} />
      </div>

      <div className="flex flex-wrap items-end gap-3 mb-5">
        <div>
          <h1 className="text-xl font-bold text-gray-900">{pageTitle}</h1>
          {data?.data?.totalCount != null && !isLoading && (
            <p className="text-sm text-gray-500 mt-0.5">
              {data.data.totalCount.toLocaleString()}{' '}
              {data.data.totalCount === 1 ? 'item' : 'items'} found
            </p>
          )}
        </div>

        <button
          onClick={() => setMobileFiltersOpen(true)}
          className="lg:hidden ml-auto flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg text-sm text-gray-700 hover:border-primary hover:text-primary bg-white transition-colors"
        >
          <SlidersHorizontal size={15} />
          Filters
          {activeFilterCount > 0 && (
            <span className="bg-primary text-white text-xs w-5 h-5 rounded-full flex items-center justify-center font-bold">
              {activeFilterCount}
            </span>
          )}
        </button>
      </div>

      <div className="flex gap-6">
        <aside className="hidden lg:block w-56 shrink-0">
          <div className="card p-4 sticky top-[146px] max-h-[calc(100vh-168px)] overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-semibold text-gray-900 flex items-center gap-2 text-sm">
                <SlidersHorizontal size={15} /> Filters
              </h3>
              {activeFilterCount > 0 && (
                <button
                  onClick={handleReset}
                  className="text-xs text-red-400 hover:text-red-600 transition-colors"
                >
                  Reset ({activeFilterCount})
                </button>
              )}
            </div>
            <FilterSidebar {...filterProps} />
          </div>
        </aside>

        <div className="flex-1 min-w-0">
          <ListingGrid
            data={data?.data}
            isLoading={isLoading}
            onPageChange={handlePageChange}
          />
        </div>
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
              >
                <X size={20} />
              </button>
            </div>
            <div className="overflow-y-auto flex-1 p-5">
              <FilterSidebar {...filterProps} />
            </div>
            <div className="p-4 border-t border-gray-100 shrink-0">
              <button
                onClick={() => setMobileFiltersOpen(false)}
                className="w-full bg-primary text-white py-3 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
              >
                {data?.data?.totalCount != null
                  ? `Show ${data.data.totalCount.toLocaleString()} results`
                  : 'Show Results'}
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
