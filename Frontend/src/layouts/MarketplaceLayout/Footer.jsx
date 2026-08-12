import { Link } from 'react-router-dom'
import { Facebook, Twitter, Instagram, Youtube } from 'lucide-react'
import { ROUTES } from '@/constants/routes'

const footerLinks = [
  {
    title: 'Buy',
    links: [
      { label: 'Browse Categories', to: ROUTES.LISTINGS },
      { label: 'Daily Deals', to: ROUTES.DEALS },
      { label: 'Track My Orders', to: ROUTES.ORDERS },
    ],
  },
  {
    title: 'Sell',
    links: [
      { label: 'Start Selling', to: ROUTES.CREATE_LISTING },
      { label: 'My Listings', to: ROUTES.MY_LISTINGS },
      { label: 'Business Profile', to: ROUTES.BUSINESS_PROFILE },
    ],
  },
  {
    title: 'Help & Contact',
    links: [
      { label: 'Help Centre', to: '/help' },
      { label: 'Contact Us', to: '/contact' },
      { label: 'Resolution Centre', to: '/resolution' },
    ],
  },
  {
    title: 'Company',
    links: [
      { label: 'About Us', to: '/about' },
      { label: 'Privacy Policy', to: '/privacy' },
      { label: 'Terms of Use', to: '/terms' },
    ],
  },
]

const socials = [
  { icon: Facebook, label: 'Facebook', href: '#' },
  { icon: Twitter, label: 'Twitter', href: '#' },
  { icon: Instagram, label: 'Instagram', href: '#' },
  { icon: Youtube, label: 'YouTube', href: '#' },
]

export default function Footer() {
  return (
    <footer className="bg-white border-t border-gray-200 mt-12">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-10">
        {/* Link columns */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-10">
          {footerLinks.map((section) => (
            <div key={section.title}>
              <h3 className="text-sm font-bold text-gray-900 mb-4 uppercase tracking-wide">
                {section.title}
              </h3>
              <ul className="space-y-2.5">
                {section.links.map((link) => (
                  <li key={link.label}>
                    <Link
                      to={link.to}
                      className="text-sm text-gray-500 hover:text-primary transition-colors"
                    >
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {/* Bottom bar */}
        <div className="border-t border-gray-100 pt-6 flex flex-col md:flex-row items-center justify-between gap-4">
          {/* Logo */}
          <Link to="/" className="flex items-center gap-0.5 select-none">
            <span className="text-shopease-blue font-extrabold text-xl">Shop</span>
            <span className="text-shopease-green font-extrabold text-xl">Ease</span>
          </Link>

          <p className="text-xs text-gray-400 order-last md:order-none">
            © {new Date().getFullYear()} ShopEase. All rights reserved.
          </p>

          {/* Social links */}
          <div className="flex items-center gap-3">
            {socials.map(({ icon: Icon, label, href }) => (
              <a
                key={label}
                href={href}
                aria-label={label}
                className="p-2 text-gray-400 hover:text-primary hover:bg-gray-50 rounded-full transition-colors"
              >
                <Icon size={17} />
              </a>
            ))}
          </div>
        </div>
      </div>
    </footer>
  )
}
