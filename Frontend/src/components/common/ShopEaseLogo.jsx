import { Link } from 'react-router-dom'
import { ROUTES } from '@/constants/routes'

export default function ShopEaseLogo({ className = '', asLink = true, onClick }) {
  const content = (
    <span className={`inline-flex items-center select-none tracking-tight font-extrabold ${className}`}>
      <span className="text-shopease-blue leading-none">Shop</span>
      <span className="text-shopease-green leading-none">Ease</span>
    </span>
  )

  if (asLink) {
    return (
      <Link to={ROUTES.HOME} aria-label="Go to home page" onClick={onClick}>
        {content}
      </Link>
    )
  }

  return content
}
