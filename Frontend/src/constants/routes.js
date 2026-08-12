export const ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  REGISTER: '/register',
  FORGOT_PASSWORD: '/forgot-password',
  RESET_PASSWORD: '/reset-password',
  VERIFY_EMAIL: '/verify-email',
  DEALS: '/deals',
  LISTINGS: '/listings',
  LISTING_DETAIL: '/listings/:id',
  CREATE_LISTING: '/listings/new',
  EDIT_LISTING: '/listings/:id/edit',
  CART: '/cart',
  CHECKOUT: '/checkout',
  WISHLIST: '/wishlist',
  ORDERS: '/orders',
  ORDER_DETAIL: '/orders/:id',
  PROFILE: '/profile',
  MY_LISTINGS: '/profile/listings',
  SELLER_ORDERS: '/profile/sales',
  BUSINESS_PROFILE: '/profile/business',
  ADMIN_DASHBOARD: '/admin/dashboard',
  ADMIN_LISTINGS: '/admin/listings',
  ADMIN_USERS: '/admin/users',
  ADMIN_ORDERS: '/admin/orders',
  ADMIN_CATEGORIES: '/admin/categories',
  ADMIN_BUSINESS_PROFILES: '/admin/business-profiles',
  ADMIN_EMAIL_TEMPLATES: '/admin/email-templates',
  ADMIN_REVIEW: '/admin/review',
  ADMIN_AUCTIONS: '/admin/auctions',
  ADMIN_AUCTION_DETAIL: '/admin/auctions/:listingId',
  MY_BIDS: '/profile/bids',
}

export const buildRoute = (route, params = {}) => {
  return Object.entries(params).reduce(
    (path, [key, value]) => path.replace(`:${key}`, value),
    route
  )
}
