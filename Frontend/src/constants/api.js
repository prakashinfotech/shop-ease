export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''

export const API_ENDPOINTS = {
  // Auth
  AUTH: {
    LOGIN: '/api/v1/auth/login',
    CHECK_EMAIL: '/api/v1/auth/check-email',
    REGISTER: '/api/v1/auth/register',
    REFRESH: '/api/v1/auth/refresh',
    LOGOUT: '/api/v1/auth/logout',
    ME: '/api/v1/auth/me',
    VERIFY_EMAIL: '/api/v1/auth/verify-email',
    RESEND_VERIFICATION: '/api/v1/auth/resend-verification',
    FORGOT_PASSWORD: '/api/v1/auth/forgot-password',
    RESET_PASSWORD: '/api/v1/auth/reset-password',
  },
  // Listings
  LISTINGS: {
    BASE: '/api/v1/listings',
    BY_ID: (id) => `/api/v1/listings/${id}`,
    MY: '/api/v1/listings/my',
    RESTORE: (id) => `/api/v1/listings/${id}/restore`,
    RECENTLY_VIEWED: '/api/v1/listings/recently-viewed',
    VIEW: (id) => `/api/v1/listings/${id}/views`,
    IMAGES: '/api/v1/listings/images',
    AUTOCOMPLETE: '/api/v1/listings/autocomplete',
    FACETS: '/api/v1/listings/facets',
  },
  // Categories
  CATEGORIES: {
    BASE: '/api/v1/categories',
    BY_ID: (id) => `/api/v1/categories/${id}`,
    TREE: '/api/v1/categories/tree',
    METADATA: (id) => `/api/v1/categories/${id}/metadata`,
    ATTRIBUTES: (id) => `/api/v1/categories/${id}/attributes`,
    ATTRIBUTE_BY_ID: (id, attributeId) => `/api/v1/categories/${id}/attributes/${attributeId}`,
  },
  // Cart
  CART: {
    BASE: '/api/v1/cart',
    ADD: '/api/v1/cart/items',
    REMOVE: (itemId) => `/api/v1/cart/items/${itemId}`,
    CHECKOUT: '/api/v1/cart/checkout',
    CLEAR: '/api/v1/cart/clear',
  },
  // Orders
  ORDERS: {
    BASE: '/api/v1/orders',
    BY_ID: (id) => `/api/v1/orders/${id}`,
    CHECKOUT: '/api/v1/orders/checkout',
    CANCEL: (id) => `/api/v1/orders/${id}/cancel`,
    SELLER: '/api/v1/orders/seller',
    SELLER_BY_ID: (id) => `/api/v1/orders/seller/${id}`,
    SELLER_STATUS: (id) => `/api/v1/orders/seller/${id}/status`,
  },
  // Users
  USERS: {
    BASE: '/api/v1/users',
    BY_ID: (id) => `/api/v1/users/${id}`,
    PROFILE: '/api/v1/users/profile',
    SUSPEND: (id) => `/api/v1/users/${id}/suspend`,
    ACTIVATE: (id) => `/api/v1/users/${id}/activate`,
  },
  // Business Profile
  BUSINESS_PROFILE: {
    BASE: '/api/v1/business-profile',
    SUBMIT_FOR_REVIEW: '/api/v1/business-profile/submit-for-review',
    DOCUMENTS: '/api/v1/business-profile/documents',
    DOCUMENT_BY_ID: (id) => `/api/v1/business-profile/documents/${id}`,
  },
  // Admin
  ADMIN: {
    STATS: '/api/v1/admin/stats',
    USERS: '/api/v1/admin/users',
    USER_SUSPEND: (id) => `/api/v1/admin/users/${id}/suspend`,
    USER_ACTIVATE: (id) => `/api/v1/admin/users/${id}/activate`,
    LISTINGS: '/api/v1/admin/listings',
    LISTING_DETAIL: (id) => `/api/v1/admin/listings/${id}`,
    LISTING_APPROVE: (id) => `/api/v1/admin/listings/${id}/approve`,
    LISTING_REJECT: (id) => `/api/v1/admin/listings/${id}/reject`,
    LISTING_DELETE: (id) => `/api/v1/admin/listings/${id}`,
    LISTING_VERSIONS: (id) => `/api/v1/admin/listings/${id}/versions`,
    ORDERS: '/api/v1/admin/orders',
    ORDER_STATUS: (id) => `/api/v1/admin/orders/${id}/status`,
    BUSINESS_PROFILES: '/api/v1/admin/business-profiles',
    BUSINESS_PROFILE_REVIEW: (id) => `/api/v1/admin/business-profiles/${id}/review`,
    EMAIL_TEMPLATES: '/api/v1/admin/email-templates',
    EMAIL_TEMPLATE_BY_ID: (id) => `/api/v1/admin/email-templates/${id}`,
    EMAIL_TEMPLATE_ACTIVATE: (id) => `/api/v1/admin/email-templates/${id}/activate`,
    EMAIL_TEMPLATE_DEACTIVATE: (id) => `/api/v1/admin/email-templates/${id}/deactivate`,
  },
  // Auctions
  AUCTIONS: {
    ACTIVE: '/api/v1/auctions/active',
    STATUS: (listingId) => `/api/v1/auctions/${listingId}/status`,
    BIDS: (listingId) => `/api/v1/auctions/${listingId}/bids`,
    PLACE_BID: (listingId) => `/api/v1/auctions/${listingId}/bids`,
    MY_BIDS: '/api/v1/auctions/my-bids',
    ADMIN_LIST: '/api/v1/admin/auctions',
    ADMIN_BIDS: (listingId) => `/api/v1/admin/auctions/${listingId}/bids`,
    ADMIN_CANCEL: (listingId) => `/api/v1/admin/auctions/${listingId}/cancel`,
    ADMIN_EXTEND: (listingId) => `/api/v1/admin/auctions/${listingId}/extend`,
  },
  // Listing approval (seller-side)
  LISTING_APPROVAL: {
    SUBMIT: (id) => `/api/v1/listings/${id}/submit`,
    SUBMIT_UPDATE: (id) => `/api/v1/listings/${id}/submit-update`,
    VERSIONS: (id) => `/api/v1/listings/${id}/versions`,
  },
}
