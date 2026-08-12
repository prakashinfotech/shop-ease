import { useState, useRef, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Search, X } from 'lucide-react'
import { useDebounce } from '@/hooks/useDebounce'
import { useListingAutocomplete } from '../hooks/useListings'
import { ROUTES } from '@/constants/routes'

export default function SearchBar({ initialValue = '', className = '' }) {
  const navigate = useNavigate()
  const [value, setValue] = useState(initialValue)
  const [open, setOpen] = useState(false)
  const [highlighted, setHighlighted] = useState(-1)
  const inputRef = useRef(null)
  const containerRef = useRef(null)

  const debouncedValue = useDebounce(value, 300)

  const { data: suggestions = [] } = useListingAutocomplete(
    debouncedValue.length >= 2 ? debouncedValue : null,
  )

  useEffect(() => {
    setHighlighted(-1)
    setOpen(suggestions.length > 0 && debouncedValue.length >= 2)
  }, [suggestions, debouncedValue])

  useEffect(() => {
    const handler = (e) => {
      if (containerRef.current && !containerRef.current.contains(e.target)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const doSearch = (q) => {
    const trimmed = q.trim()
    setOpen(false)
    navigate(trimmed ? `${ROUTES.LISTINGS}?q=${encodeURIComponent(trimmed)}` : ROUTES.LISTINGS)
  }

  const handleKeyDown = (e) => {
    if (!open) return
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setHighlighted((h) => Math.min(h + 1, suggestions.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setHighlighted((h) => Math.max(h - 1, -1))
    } else if (e.key === 'Escape') {
      setOpen(false)
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (highlighted >= 0 && suggestions[highlighted]) {
        const chosen = suggestions[highlighted]
        setValue(chosen)
        doSearch(chosen)
      } else {
        doSearch(value)
      }
    }
  }

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <form
        onSubmit={(e) => { e.preventDefault(); doSearch(value) }}
        className="flex"
      >
        <div className="relative flex-1">
          <Search
            size={16}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none"
          />
          <input
            ref={inputRef}
            type="text"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            onFocus={() => {
              if (suggestions.length > 0 && debouncedValue.length >= 2) setOpen(true)
            }}
            onKeyDown={handleKeyDown}
            placeholder="Search for anything..."
            className="w-full pl-9 pr-8 py-2.5 border border-r-0 border-gray-300 rounded-l-xl text-sm focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent"
          />
          {value && (
            <button
              type="button"
              onClick={() => { setValue(''); inputRef.current?.focus() }}
              className="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
            >
              <X size={14} />
            </button>
          )}
        </div>
        <button
          type="submit"
          className="bg-primary hover:bg-blue-700 text-white px-5 py-2.5 rounded-r-xl text-sm font-semibold transition-colors shrink-0"
        >
          Search
        </button>
      </form>

      {open && (
        <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-200 rounded-xl shadow-xl z-50 overflow-hidden">
          {suggestions.map((s, i) => (
            <button
              key={i}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => { setValue(s); doSearch(s) }}
              className={`w-full text-left px-4 py-2.5 text-sm flex items-center gap-2.5 transition-colors ${
                i === highlighted
                  ? 'bg-primary/10 text-primary'
                  : 'text-gray-700 hover:bg-gray-50'
              }`}
            >
              <Search size={13} className="text-gray-400 shrink-0" />
              {s}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
