import { useState, useRef, useEffect } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Search, ShoppingCart, Bell, ChevronDown, Menu, X,
  Heart, LogOut, User, Package, ShoppingBag, LayoutDashboard, Store, Home,
} from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'
import { useCartStore } from '@/store/cartStore'
import { useWishlistStore } from '@/store/wishlistStore'
import { ROUTES } from '@/constants/routes'
import { useDebounce } from '@/hooks/useDebounce'
import { useListingAutocomplete } from '@/features/listings/hooks/useListings'
import { categoryService } from '@/features/categories/services/categoryService'
import ShopEaseLogo from '@/components/common/ShopEaseLogo'

const EMOJI_MAP = {
  'Electronics': '📱',
  'Fashion': '👗',
  'Home & Garden': '🏡',
  'Vehicles': '🚗',
  'Sports': '⚽',
  'Toys': '🧸',
  'Books': '📚',
  'Collectibles': '🏆',
}

// Fallback strip shown before API resolves
const FALLBACK_CATEGORIES = [
  'Electronics', 'Fashion', 'Home & Garden', 'Vehicles',
  'Sports', 'Toys', 'Books', 'Collectibles',
]

function CartBadge({ count }) {
  if (!count) return null
  return (
    <span className="absolute -top-1 -right-1.5 bg-secondary text-white text-[9px] w-3.5 h-3.5 rounded-full flex items-center justify-center font-bold leading-none">
      {count > 9 ? '9+' : count}
    </span>
  )
}

export default function Navbar() {
  const { isAuthenticated, user, isAdmin, logout } = useAuth()
  const totalItems = useCartStore((s) => s.totalItems)
  const wishlistCount = useWishlistStore((s) => s.items.length)
  const navigate = useNavigate()

  const [searchQuery, setSearchQuery] = useState('')
  const [selectedCategoryId, setSelectedCategoryId] = useState('')
  const [autocompleteOpen, setAutocompleteOpen] = useState(false)
  const [highlighted, setHighlighted] = useState(-1)
  const [shopByCatOpen, setShopByCatOpen] = useState(false)
  const [hiUserOpen, setHiUserOpen] = useState(false)
  const [myShopEaseOpen, setMyShopEaseOpen] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)

  const searchRef = useRef(null)
  const hiUserRef = useRef(null)
  const shopByCatRef = useRef(null)
  const myShopEaseRef = useRef(null)

  const debouncedQuery = useDebounce(searchQuery, 300)
  const { data: suggestions = [] } = useListingAutocomplete(
    debouncedQuery.length >= 2 ? debouncedQuery : null,
  )

  const { data: categoryTree = [] } = useQuery({
    queryKey: ['categories', 'tree'],
    queryFn: categoryService.getTree,
    staleTime: 5 * 60_000,
    select: (res) => res?.data ?? [],
  })

  // Navigate to the category — ListingsPage handles the browse-vs-listings split
  const handleCategoryClick = (category) => {
    navigate(`${ROUTES.LISTINGS}?categoryId=${category.id}&category=${encodeURIComponent(category.name)}`)
    setShopByCatOpen(false)
    setMobileOpen(false)
  }

  // Displayed strip: real tree when loaded, fallback labels otherwise
  const stripCategories = categoryTree.length > 0
    ? categoryTree
    : FALLBACK_CATEGORIES.map((name) => ({ id: name, name, children: [] }))

  useEffect(() => {
    setHighlighted(-1)
    setAutocompleteOpen(suggestions.length > 0 && debouncedQuery.length >= 2)
  }, [suggestions, debouncedQuery])

  useEffect(() => {
    const handler = (e) => {
      if (searchRef.current && !searchRef.current.contains(e.target)) setAutocompleteOpen(false)
      if (hiUserRef.current && !hiUserRef.current.contains(e.target)) setHiUserOpen(false)
      if (shopByCatRef.current && !shopByCatRef.current.contains(e.target)) setShopByCatOpen(false)
      if (myShopEaseRef.current && !myShopEaseRef.current.contains(e.target)) setMyShopEaseOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  useEffect(() => { setMobileOpen(false) }, [navigate])

  const doSearch = (q) => {
    const trimmed = q.trim()
    setAutocompleteOpen(false)
    const params = new URLSearchParams()
    if (trimmed) params.set('q', trimmed)
    if (selectedCategoryId) {
      const selectedCategory = categoryTree.find((cat) => cat.id === selectedCategoryId)
      params.set('categoryId', selectedCategoryId)
      if (selectedCategory) params.set('category', selectedCategory.name)
    }
    navigate(`${ROUTES.LISTINGS}${params.toString() ? '?' + params.toString() : ''}`)
    setMobileOpen(false)
  }

  const handleSearch = (e) => {
    e.preventDefault()
    doSearch(searchQuery)
  }

  const handleKeyDown = (e) => {
    if (!autocompleteOpen) return
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setHighlighted((h) => Math.min(h + 1, suggestions.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setHighlighted((h) => Math.max(h - 1, -1))
    } else if (e.key === 'Escape') {
      setAutocompleteOpen(false)
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (highlighted >= 0 && suggestions[highlighted]) {
        const chosen = suggestions[highlighted]
        setSearchQuery(chosen)
        doSearch(chosen)
      } else {
        doSearch(searchQuery)
      }
    }
  }

  const handleLogout = () => {
    logout()
    navigate(ROUTES.HOME)
    setMobileOpen(false)
    setHiUserOpen(false)
    setMyShopEaseOpen(false)
  }

  const myShopEaseLinks = [
    ...(isAdmin ? [{ to: ROUTES.ADMIN_DASHBOARD, icon: LayoutDashboard, label: 'Admin Panel' }] : []),
    { to: ROUTES.MY_LISTINGS, icon: Package, label: 'My Listings' },
    { to: ROUTES.SELLER_ORDERS, icon: Store, label: 'Seller Orders' },
    { to: ROUTES.ORDERS, icon: ShoppingBag, label: 'My Orders' },
    { to: ROUTES.WISHLIST, icon: Heart, label: 'Wishlist' },
  ]

  return (
    <header className="sticky top-0 z-40 shadow-sm">

      {/* ── Top utility bar ── */}
      <div className="hidden sm:block bg-[#f3f3f3] border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-9 flex items-center justify-between">

          {/* Left */}
          <div className="flex items-center gap-1 text-xs text-gray-600">
            {isAuthenticated ? (
              <div className="relative" ref={hiUserRef}>
                <button
                  type="button"
                  onClick={() => setHiUserOpen((v) => !v)}
                  className="flex items-center gap-0.5 hover:text-primary hover:underline"
                >
                  <span>Hi <span className="font-semibold text-primary">{user?.firstName}!</span></span>
                  <ChevronDown size={10} className={`transition-transform ${hiUserOpen ? 'rotate-180' : ''}`} />
                </button>
                {hiUserOpen && (
                  <div className="absolute left-0 top-full mt-1 w-48 bg-white border border-gray-200 rounded-lg shadow-xl z-50 py-1 text-sm">
                    <Link
                      to={ROUTES.PROFILE}
                      onClick={() => setHiUserOpen(false)}
                      className="flex items-center gap-2.5 px-4 py-2.5 text-gray-700 hover:bg-gray-50 transition-colors"
                    >
                      <User size={14} className="text-gray-400" />
                      My Profile
                    </Link>
                    <button
                      type="button"
                      onClick={handleLogout}
                      className="flex w-full items-center gap-2.5 px-4 py-2.5 text-red-600 hover:bg-red-50 transition-colors"
                    >
                      <LogOut size={14} />
                      Sign Out
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <span>
                Hi!{' '}
                <Link to={ROUTES.LOGIN} className="text-primary hover:underline font-medium">Sign in</Link>
                {' or '}
                <Link to={ROUTES.REGISTER} className="text-primary hover:underline font-medium">register</Link>
              </span>
            )}
            <div className="hidden md:flex items-center gap-3 ml-3 pl-3 border-l border-gray-300">
              <Link to={ROUTES.DEALS} className="hover:text-primary hover:underline">Deals</Link>
            </div>
          </div>

          {/* Right */}
          <div className="flex items-center gap-2.5 text-xs text-gray-600">
            <span className="hidden lg:flex items-center gap-1 cursor-pointer hover:text-primary hover:underline">
              🇺🇸 Ship to
            </span>
            <span className="hidden lg:block text-gray-300">|</span>
            <Link to={ROUTES.WISHLIST} className="flex items-center gap-0.5 hover:text-primary hover:underline">
              Watchlist <ChevronDown size={10} />
            </Link>
            <span className="text-gray-300">|</span>

            {isAuthenticated ? (
              <div className="relative" ref={myShopEaseRef}>
                <button
                  onClick={() => setMyShopEaseOpen((v) => !v)}
                  className="flex items-center gap-0.5 font-medium hover:text-primary hover:underline"
                >
                  My ShopEase <ChevronDown size={10} className={`transition-transform ${myShopEaseOpen ? 'rotate-180' : ''}`} />
                </button>
                {myShopEaseOpen && (
                  <div className="absolute right-0 top-full mt-1 w-52 bg-white border border-gray-200 rounded-lg shadow-xl z-50 py-1 text-sm">
                    <div className="px-4 py-2.5 border-b border-gray-100">
                      <p className="font-semibold text-gray-900 truncate">{user?.firstName} {user?.lastName}</p>
                      <p className="text-xs text-gray-500 truncate mt-0.5">{user?.email}</p>
                    </div>
                    {myShopEaseLinks.map(({ to, icon: Icon, label }) => (
                      <Link
                        key={to}
                        to={to}
                        onClick={() => setMyShopEaseOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2.5 text-gray-700 hover:bg-gray-50 transition-colors"
                      >
                        <Icon size={14} className="text-gray-400" />
                        {label}
                      </Link>
                    ))}
                  </div>
                )}
              </div>
            ) : (
              <Link to={ROUTES.LOGIN} className="hover:text-primary hover:underline">My ShopEase</Link>
            )}

            <span className="text-gray-300">|</span>
            <button className="hover:text-primary p-0.5">
              <Bell size={14} />
            </button>
            <Link to={ROUTES.CART} className="relative hover:text-primary p-0.5">
              <ShoppingCart size={14} />
              <CartBadge count={totalItems} />
            </Link>
          </div>
        </div>
      </div>

      {/* ── Main header ── */}
      <div className="bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="flex items-center gap-3 lg:gap-4 h-14 sm:h-[68px]">

            {/* Hamburger (mobile/tablet) */}
            <button
              className="lg:hidden p-1.5 -ml-1 text-gray-600 hover:text-primary shrink-0"
              onClick={() => setMobileOpen(true)}
              aria-label="Open menu"
            >
              <Menu size={22} />
            </button>

            {/* Logo */}
            <ShopEaseLogo className="shrink-0 text-[2rem] sm:text-[2.2rem]" />

            {/* Shop by category (desktop only) */}
            <div className="hidden lg:block relative shrink-0" ref={shopByCatRef}>
              <button
                onClick={() => setShopByCatOpen((v) => !v)}
                className="flex items-end gap-1 text-[13px] font-medium text-gray-800 hover:text-primary transition-colors leading-tight"
              >
                <span className="text-left leading-tight">
                  Shop by<br />category
                </span>
                <ChevronDown size={13} className={`mb-0.5 transition-transform ${shopByCatOpen ? 'rotate-180' : ''}`} />
              </button>
              {shopByCatOpen && (
                <div className="absolute top-full left-0 mt-2 w-52 bg-white border border-gray-200 rounded-lg shadow-2xl z-50 py-1.5">
                  {categoryTree.map((cat) => (
                    <button
                      key={cat.id}
                      onClick={() => handleCategoryClick(cat)}
                      className="w-full text-left flex items-center gap-2.5 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 hover:text-primary transition-colors"
                    >
                      <span className="text-base">{EMOJI_MAP[cat.name] ?? '🏷️'}</span>
                      {cat.name}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Search bar (sm+) */}
            <div ref={searchRef} className="hidden sm:flex flex-1 min-w-0 items-center gap-2 relative">
              <form onSubmit={handleSearch} className="flex flex-1">
                <div className="flex flex-1 border-[2.5px] border-gray-900 rounded-full overflow-hidden focus-within:border-primary transition-colors">
                  <input
                    type="text"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    onFocus={() => {
                      if (suggestions.length > 0 && debouncedQuery.length >= 2) setAutocompleteOpen(true)
                    }}
                    onKeyDown={handleKeyDown}
                    placeholder="Search for anything"
                    className="flex-1 px-4 py-2 sm:py-2.5 text-sm focus:outline-none min-w-0"
                  />
                  <div className="hidden md:flex items-center border-l border-gray-300 bg-gray-50 pr-1">
                    <select
                      value={selectedCategoryId}
                      onChange={(e) => setSelectedCategoryId(e.target.value)}
                      className="text-xs text-gray-700 bg-transparent border-none focus:outline-none pl-3 pr-7 py-2 cursor-pointer appearance-none"
                      style={{
                        backgroundImage: `url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e")`,
                        backgroundRepeat: 'no-repeat',
                        backgroundPosition: 'right 4px center',
                        backgroundSize: '16px',
                      }}
                    >
                      <option value="">All Categories</option>
                      {categoryTree.map((cat) => (
                        <option key={cat.id} value={cat.id}>{cat.name}</option>
                      ))}
                    </select>
                  </div>
                </div>
                <button
                  type="submit"
                  className="bg-primary hover:bg-blue-700 text-white px-5 sm:px-6 py-2 sm:py-2.5 text-sm font-semibold rounded-full ml-2 transition-colors shrink-0"
                >
                  Search
                </button>
              </form>
              <Link
                to={ROUTES.LISTINGS}
                className="hidden xl:block text-xs text-gray-500 hover:underline hover:text-gray-700 shrink-0 whitespace-nowrap"
              >
                Advanced
              </Link>

              {/* Autocomplete dropdown */}
              {autocompleteOpen && (
                <div className="absolute top-full left-0 right-[90px] xl:right-[104px] mt-1 bg-white border border-gray-200 rounded-xl shadow-xl z-50 overflow-hidden">
                  {suggestions.map((s, i) => (
                    <button
                      key={i}
                      type="button"
                      onMouseDown={(e) => e.preventDefault()}
                      onClick={() => { setSearchQuery(s); doSearch(s) }}
                      className={`w-full text-left px-4 py-2.5 text-sm flex items-center gap-2.5 transition-colors ${
                        i === highlighted ? 'bg-primary/10 text-primary' : 'text-gray-700 hover:bg-gray-50'
                      }`}
                    >
                      <Search size={13} className="text-gray-400 shrink-0" />
                      {s}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Mobile right icons */}
            <div className="sm:hidden flex items-center gap-1 ml-auto">
              <Link to={ROUTES.WISHLIST} className="relative p-2 text-gray-600 hover:text-secondary">
                <Heart size={20} />
                {wishlistCount > 0 && (
                  <span className="absolute -top-0.5 -right-0.5 bg-secondary text-white text-[9px] w-3.5 h-3.5 rounded-full flex items-center justify-center font-bold">
                    {wishlistCount > 9 ? '9+' : wishlistCount}
                  </span>
                )}
              </Link>
              <Link to={ROUTES.CART} className="relative p-2 text-gray-600 hover:text-primary">
                <ShoppingCart size={20} />
                <CartBadge count={totalItems} />
              </Link>
            </div>
          </div>

          {/* Mobile search row */}
          <div className="sm:hidden pb-3">
            <form onSubmit={handleSearch} className="flex">
              <div className="flex flex-1 border-2 border-gray-900 rounded-full overflow-hidden focus-within:border-primary transition-colors">
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Search for anything"
                  className="flex-1 px-4 py-2 text-sm focus:outline-none min-w-0"
                />
              </div>
              <button
                type="submit"
                className="bg-primary hover:bg-blue-700 text-white px-5 py-2 text-sm font-semibold rounded-full ml-2 transition-colors"
              >
                Search
              </button>
            </form>
          </div>
        </div>

        {/* ── Category strip ── */}
        <div className="border-t border-gray-100">
          <div className="max-w-7xl mx-auto px-4 sm:px-6">
            <div className="flex items-center gap-0.5 overflow-x-auto scrollbar-hide py-1">
              <NavLink
                to={ROUTES.LISTINGS}
                end
                className={({ isActive }) =>
                  `shrink-0 px-3 py-1.5 text-xs font-medium rounded-full transition-colors whitespace-nowrap ${
                    isActive ? 'bg-primary text-white' : 'text-gray-600 hover:text-primary hover:bg-gray-50'
                  }`
                }
              >
                All Categories
              </NavLink>
              {stripCategories.map((cat) => (
                <button
                  key={cat.id}
                  onClick={() => handleCategoryClick(cat)}
                  className="shrink-0 flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-gray-600 hover:text-primary hover:bg-gray-50 rounded-full transition-colors whitespace-nowrap"
                >
                  <span>{EMOJI_MAP[cat.name] ?? '🏷️'}</span>
                  <span>{cat.name}</span>
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* ── Mobile drawer ── */}
      {mobileOpen && (
        <>
          <div className="fixed inset-0 bg-black/50 z-50" onClick={() => setMobileOpen(false)} />
          <div className="fixed inset-y-0 left-0 w-72 bg-white z-50 shadow-2xl flex flex-col overflow-hidden">
            <div className="flex items-center justify-between h-14 px-4 border-b border-gray-200 shrink-0">
              <ShopEaseLogo className="text-xl" onClick={() => setMobileOpen(false)} />
              <button onClick={() => setMobileOpen(false)} className="p-2 text-gray-500 hover:text-gray-800 rounded-md">
                <X size={20} />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto">
              {isAuthenticated ? (
                <>
                  <div className="flex items-center gap-3 p-4 bg-primary/5 border-b border-gray-100">
                    <div className="w-11 h-11 bg-primary rounded-full flex items-center justify-center text-white text-lg font-bold shrink-0">
                      {user?.firstName?.[0]?.toUpperCase()}
                    </div>
                    <div className="min-w-0">
                      <p className="font-semibold text-gray-900 text-sm truncate">{user?.firstName} {user?.lastName}</p>
                      <p className="text-xs text-gray-500 truncate">{user?.email}</p>
                    </div>
                  </div>
                  <nav className="p-3 space-y-0.5">
                    {[
                      { to: ROUTES.HOME, icon: Home, label: 'Home' },
                      ...(isAdmin ? [{ to: ROUTES.ADMIN_DASHBOARD, icon: LayoutDashboard, label: 'Admin Panel' }] : []),
                      { to: ROUTES.PROFILE, icon: User, label: 'My Profile' },
                      { to: ROUTES.MY_LISTINGS, icon: Package, label: 'My Listings' },
                      { to: ROUTES.SELLER_ORDERS, icon: Store, label: 'Seller Orders' },
                      { to: ROUTES.ORDERS, icon: ShoppingBag, label: 'My Orders' },
                      { to: ROUTES.WISHLIST, icon: Heart, label: `Wishlist${wishlistCount ? ` (${wishlistCount})` : ''}` },
                      { to: ROUTES.CART, icon: ShoppingCart, label: `Cart${totalItems ? ` (${totalItems})` : ''}` },
                      { to: ROUTES.CREATE_LISTING, icon: Store, label: 'Sell an Item' },
                    ].map(({ to, icon: Icon, label }) => (
                      <Link
                        key={to}
                        to={to}
                        onClick={() => setMobileOpen(false)}
                        className="flex items-center gap-3 px-3 py-2.5 text-sm text-gray-700 hover:bg-gray-50 hover:text-primary rounded-lg transition-colors"
                      >
                        <Icon size={17} className="text-gray-400" />
                        {label}
                      </Link>
                    ))}
                  </nav>
                  <hr className="border-gray-100 mx-3" />
                  <div className="p-3">
                    <button
                      onClick={handleLogout}
                      className="flex w-full items-center gap-3 px-3 py-2.5 text-sm text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    >
                      <LogOut size={17} />
                      Sign Out
                    </button>
                  </div>
                </>
              ) : (
                <div className="p-4 space-y-3">
                  <Link
                    to={ROUTES.LOGIN}
                    onClick={() => setMobileOpen(false)}
                    className="block w-full btn-secondary text-center py-3 rounded-xl"
                  >
                    Sign In
                  </Link>
                  <Link
                    to={ROUTES.REGISTER}
                    onClick={() => setMobileOpen(false)}
                    className="block w-full btn-primary text-center py-3 rounded-xl"
                  >
                    Register
                  </Link>
                </div>
              )}

              <div className="p-3 border-t border-gray-100">
                <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider px-3 py-2">Categories</p>
                {stripCategories.map((cat) => (
                  <button
                    key={cat.id}
                    onClick={() => handleCategoryClick(cat)}
                    className="w-full text-left flex items-center gap-3 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 hover:text-primary rounded-lg transition-colors"
                  >
                    <span className="text-lg">{EMOJI_MAP[cat.name] ?? '🏷️'}</span>
                    {cat.name}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </>
      )}
    </header>
  )
}
