import { Link } from 'react-router-dom'
import { ChevronRight, Home } from 'lucide-react'

export default function Breadcrumbs({ items = [] }) {
  return (
    <nav className="flex items-center gap-1 text-sm text-gray-500 flex-wrap">
      <Link to="/" className="flex items-center hover:text-primary transition-colors shrink-0">
        <Home size={14} />
      </Link>
      {items.map((item, idx) => (
        <span key={idx} className="flex items-center gap-1 min-w-0">
          <ChevronRight size={14} className="text-gray-300 shrink-0" />
          {item.to ? (
            <Link to={item.to} className="hover:text-primary transition-colors truncate">
              {item.label}
            </Link>
          ) : (
            <span className="text-gray-900 font-medium truncate">{item.label}</span>
          )}
        </span>
      ))}
    </nav>
  )
}
