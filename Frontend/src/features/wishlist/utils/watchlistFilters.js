export const SORT_OPTIONS = [
  { label: 'Recently saved', value: 'saved_desc' },
  { label: 'Oldest saved', value: 'saved_asc' },
  { label: 'Price: Low to High', value: 'price_asc' },
  { label: 'Price: High to Low', value: 'price_desc' },
  { label: 'Title A-Z', value: 'title_asc' },
  { label: 'Title Z-A', value: 'title_desc' },
]

export const PAGE_SIZE_OPTIONS = [8, 12, 24]

const parsePriceFilter = (value) => {
  if (value === '') return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

const getSavedTime = (listing, fallbackIndex) => {
  const savedAt = new Date(listing.savedAt).getTime()
  return Number.isNaN(savedAt) ? fallbackIndex : savedAt
}

const compareText = (a = '', b = '') =>
  a.localeCompare(b, undefined, { sensitivity: 'base', numeric: true })

export const getWatchlistCategories = (items) => {
  const unique = new Set(items.map((item) => item.categoryName).filter(Boolean))
  return [...unique].sort(compareText)
}

export const filterAndSortWatchlistItems = ({
  items,
  query = '',
  category = '',
  minPrice = '',
  maxPrice = '',
  freeShippingOnly = false,
  sortKey = 'saved_desc',
}) => {
  const normalizedQuery = query.trim().toLowerCase()
  const min = parsePriceFilter(minPrice)
  const max = parsePriceFilter(maxPrice)

  return items
    .map((listing, index) => ({ listing, index }))
    .filter(({ listing }) => {
      const searchableText = [
        listing.title,
        listing.categoryName,
        listing.description,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
      const price = Number(listing.finalPrice ?? listing.price ?? 0)

      if (normalizedQuery && !searchableText.includes(normalizedQuery)) return false
      if (category && listing.categoryName !== category) return false
      if (min != null && price < min) return false
      if (max != null && price > max) return false
      if (freeShippingOnly && !listing.freeShipping) return false

      return true
    })
    .sort((a, b) => {
      const priceA = Number(a.listing.finalPrice ?? a.listing.price ?? 0)
      const priceB = Number(b.listing.finalPrice ?? b.listing.price ?? 0)
      const titleA = a.listing.title ?? ''
      const titleB = b.listing.title ?? ''
      const savedA = getSavedTime(a.listing, a.index)
      const savedB = getSavedTime(b.listing, b.index)

      switch (sortKey) {
        case 'saved_asc':
          return savedA - savedB
        case 'price_asc':
          return priceA - priceB
        case 'price_desc':
          return priceB - priceA
        case 'title_asc':
          return compareText(titleA, titleB)
        case 'title_desc':
          return compareText(titleB, titleA)
        case 'saved_desc':
        default:
          return savedB - savedA
      }
    })
    .map(({ listing }) => listing)
}

export const paginateItems = (items, page, pageSize) => {
  const totalPages = Math.max(1, Math.ceil(items.length / pageSize))
  const currentPage = Math.min(Math.max(page, 1), totalPages)
  const visibleItems = items.slice((currentPage - 1) * pageSize, currentPage * pageSize)

  return { currentPage, totalPages, visibleItems }
}
