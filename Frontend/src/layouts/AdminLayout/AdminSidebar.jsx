import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard, Package, Users, ShoppingBag, Tag, X, ShieldCheck, Briefcase, Mail, ClipboardList, Gavel,
} from 'lucide-react'
import { ROUTES } from '@/constants/routes'
import ShopEaseLogo from '@/components/common/ShopEaseLogo'

const navItems = [
  { to: ROUTES.ADMIN_DASHBOARD, icon: LayoutDashboard, label: 'Dashboard' },
  { to: ROUTES.ADMIN_LISTINGS, icon: Package, label: 'Listings' },
  { to: ROUTES.ADMIN_REVIEW, icon: ClipboardList, label: 'Review Queue' },
  { to: ROUTES.ADMIN_USERS, icon: Users, label: 'Users' },
  { to: ROUTES.ADMIN_ORDERS, icon: ShoppingBag, label: 'Orders' },
  { to: ROUTES.ADMIN_CATEGORIES, icon: Tag, label: 'Categories' },
  { to: ROUTES.ADMIN_BUSINESS_PROFILES, icon: Briefcase, label: 'Business Profiles' },
  { to: ROUTES.ADMIN_EMAIL_TEMPLATES, icon: Mail, label: 'Email Templates' },
  { to: ROUTES.ADMIN_AUCTIONS, icon: Gavel, label: 'Auctions' },
]

export default function AdminSidebar({ isOpen, onClose }) {
  return (
    <>
      {/* Mobile overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/40 z-30 lg:hidden"
          onClick={onClose}
        />
      )}

      {/* Sidebar */}
      <aside
        className={`fixed top-0 left-0 h-full w-64 bg-[#071b45] text-white z-40 transform transition-transform duration-300 shadow-2xl shadow-blue-950/30
          ${isOpen ? 'translate-x-0' : '-translate-x-full'} lg:translate-x-0 lg:static lg:z-auto`}
      >
        {/* Header */}
        <div className="h-16 px-5 border-b border-white/10 flex items-center justify-between">
          <div>
            <div className="flex items-center gap-0.5 leading-none">
              <ShopEaseLogo asLink={false} className="text-2xl" />
              <span className="ml-2 text-sm font-bold text-white">Admin</span>
            </div>
            <p className="mt-1 text-[11px] font-medium uppercase tracking-wider text-blue-100/70">Seller Operations</p>
          </div>
          <button onClick={onClose} className="lg:hidden text-blue-100/70 hover:text-white">
            <X size={18} />
          </button>
        </div>

        {/* Navigation */}
        <div className="px-4 py-4">
          <div className="mb-4 rounded-md border border-white/10 bg-white/5 px-3 py-2">
            <div className="flex items-center gap-2 text-xs font-semibold text-white">
              <ShieldCheck size={15} className="text-shopease-green" />
              Marketplace Control
            </div>
          </div>
          <p className="px-2 pb-2 text-[11px] font-bold uppercase tracking-wider text-blue-100/60">Manage</p>
          <nav className="space-y-1">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              onClick={onClose}
              className={({ isActive }) =>
                `group flex items-center gap-3 rounded-md px-3 py-2.5 text-sm font-semibold transition-colors ${
                  isActive
                    ? 'bg-white text-[#071b45] shadow-sm'
                    : 'text-blue-100/80 hover:bg-white/10 hover:text-white'
                }`
              }
            >
              <Icon size={17} />
              {label}
            </NavLink>
          ))}
          </nav>
        </div>

        <div className="absolute bottom-0 left-0 right-0 border-t border-white/10 p-4">
          <div className="grid grid-cols-4 gap-1">
            <span className="h-1 rounded-full bg-shopease-blue" />
            <span className="h-1 rounded-full bg-shopease-red" />
            <span className="h-1 rounded-full bg-shopease-yellow" />
            <span className="h-1 rounded-full bg-shopease-green" />
          </div>
        </div>
      </aside>
    </>
  )
}
