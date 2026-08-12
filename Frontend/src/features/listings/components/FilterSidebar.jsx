import { useState } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'
import { formatCurrency } from '@/utils/formatters'
import CategoryFilter from './CategoryFilter'

const SORT_OPTIONS = [
  { label: 'Recently Updated', value: 'updatedAt_desc' },
  { label: 'Newest Listed', value: 'createdAt_desc' },
  { label: 'Price: Low to High', value: 'price_asc' },
  { label: 'Price: High to Low', value: 'price_desc' },
  { label: 'Title A–Z', value: 'title_asc' },
]

function Section({ title, defaultOpen = true, children }) {
  const [open, setOpen] = useState(defaultOpen)
  return (
    <div className="border-b border-gray-100 pb-4 last:border-0">
      <button
        onClick={() => setOpen((o) => !o)}
        className="w-full flex items-center justify-between py-1.5 text-sm font-semibold text-gray-900"
      >
        {title}
        {open ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
      </button>
      {open && <div className="mt-2">{children}</div>}
    </div>
  )
}

export default function FilterSidebar({
  categoryId,
  onCategoryChange,
  minPrice,
  setMinPrice,
  maxPrice,
  setMaxPrice,
  listingType,
  setListingType,
  freeShipping,
  setFreeShipping,
  attributeFilters,
  setAttributeFilters,
  facets,
  sortKey,
  setSortKey,
  onReset,
  setPage,
}) {
  const handleAttrChange = (attrId, value, checked) => {
    setAttributeFilters((prev) => {
      const next = { ...prev }
      if (checked) next[attrId] = value
      else delete next[attrId]
      return next
    })
    setPage(1)
  }

  return (
    <div className="space-y-4">
      {/* Sort */}
      <Section title="Sort By">
        <div className="space-y-0.5">
          {SORT_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              onClick={() => { setSortKey(opt.value); setPage(1) }}
              className={[
                'w-full text-left px-2 py-1.5 text-sm rounded-lg transition-colors',
                sortKey === opt.value
                  ? 'bg-primary/10 text-primary font-semibold'
                  : 'text-gray-700 hover:bg-gray-50',
              ].join(' ')}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </Section>

      {/* Category */}
      <Section title="Category">
        <CategoryFilter selectedId={categoryId} onSelect={onCategoryChange} />
      </Section>

      {/* Price range */}
      <Section title="Price Range">
        <div className="flex items-center gap-2">
          <input
            type="number"
            placeholder="Min $"
            value={minPrice}
            min="0"
            onChange={(e) => { setMinPrice(e.target.value); setPage(1) }}
            className="form-input text-xs py-2 w-full"
          />
          <span className="text-gray-400 shrink-0">—</span>
          <input
            type="number"
            placeholder="Max $"
            value={maxPrice}
            min="0"
            onChange={(e) => { setMaxPrice(e.target.value); setPage(1) }}
            className="form-input text-xs py-2 w-full"
          />
        </div>
        {facets?.price && (
          <p className="text-xs text-gray-400 mt-1.5">
            Range: {formatCurrency(facets.price.min)} – {formatCurrency(facets.price.max)}
          </p>
        )}
      </Section>

      {/* Listing type */}
      <Section title="Listing Type" defaultOpen={false}>
        <div className="space-y-1.5">
          {[
            { label: 'Fixed Price', value: '0' },
            { label: 'Auction', value: '1' },
          ].map((opt) => (
            <label
              key={opt.value}
              className="flex items-center gap-2.5 cursor-pointer text-sm text-gray-700 hover:text-primary"
            >
              <input
                type="radio"
                name="listingType"
                value={opt.value}
                checked={listingType === opt.value}
                onChange={() => { setListingType(opt.value); setPage(1) }}
                className="text-primary focus:ring-primary"
              />
              {opt.label}
            </label>
          ))}
          {listingType && (
            <button
              onClick={() => { setListingType(''); setPage(1) }}
              className="text-xs text-gray-400 hover:text-red-500 transition-colors mt-0.5"
            >
              Clear
            </button>
          )}
        </div>
      </Section>

      {/* Shipping */}
      <Section title="Shipping" defaultOpen={false}>
        <label className="flex items-center gap-2.5 cursor-pointer text-sm text-gray-700 hover:text-primary">
          <input
            type="checkbox"
            checked={freeShipping}
            onChange={(e) => { setFreeShipping(e.target.checked); setPage(1) }}
            className="rounded border-gray-300 text-primary focus:ring-primary"
          />
          Free shipping only
        </label>
      </Section>

      {/* Dynamic attribute facets */}
      {facets?.attributeFacets?.map((facet) => (
        <Section key={facet.attributeId} title={facet.displayName} defaultOpen={false}>
          <div className="space-y-1.5">
            {facet.options.map((opt) => {
              const checked = attributeFilters[facet.attributeId] === opt.value
              return (
                <label
                  key={opt.value}
                  className="flex items-center justify-between cursor-pointer text-sm text-gray-700 hover:text-primary"
                >
                  <span className="flex items-center gap-2.5">
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(e) =>
                        handleAttrChange(facet.attributeId, opt.value, e.target.checked)
                      }
                      className="rounded border-gray-300 text-primary focus:ring-primary"
                    />
                    {opt.label}
                  </span>
                  <span className="text-xs text-gray-400 ml-1">({opt.count})</span>
                </label>
              )
            })}
          </div>
        </Section>
      ))}

      <button
        onClick={onReset}
        className="w-full text-xs text-gray-400 hover:text-red-500 transition-colors pt-2"
      >
        Clear all filters
      </button>
    </div>
  )
}
