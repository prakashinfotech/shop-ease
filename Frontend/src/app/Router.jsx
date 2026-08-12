import { createBrowserRouter, Navigate } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import MarketplaceLayout from '@/layouts/MarketplaceLayout'
import AdminLayout from '@/layouts/AdminLayout'
import PrivateRoute from '@/components/common/PrivateRoute'
import AdminRoute from '@/components/common/AdminRoute'
import Spinner from '@/components/common/Spinner'
import { ROUTES } from '@/constants/routes'

const withSuspense = (Component) => (
  <Suspense fallback={<div className="flex h-screen items-center justify-center"><Spinner size="lg" /></div>}>
    <Component />
  </Suspense>
)

// Auth pages
const LoginPage = lazy(() => import('@/features/auth/pages/LoginPage'))
const RegisterPage = lazy(() => import('@/features/auth/pages/RegisterPage'))
const ForgotPasswordPage = lazy(() => import('@/features/auth/pages/ForgotPasswordPage'))
const ResetPasswordPage = lazy(() => import('@/features/auth/pages/ResetPasswordPage'))
const VerifyEmailPage = lazy(() => import('@/features/auth/pages/VerifyEmailPage'))

// Marketplace pages
const HomePage = lazy(() => import('@/features/listings/pages/HomePage'))
const DealsPage = lazy(() => import('@/features/listings/pages/DealsPage'))
const ListingsPage = lazy(() => import('@/features/listings/pages/ListingsPage'))
const ListingDetailPage = lazy(() => import('@/features/listings/pages/ListingDetailPage'))
const CreateListingPage = lazy(() => import('@/features/listings/pages/CreateListingPage'))
const EditListingPage = lazy(() => import('@/features/listings/pages/EditListingPage'))
const CartPage = lazy(() => import('@/features/cart/pages/CartPage'))
const CheckoutPage = lazy(() => import('@/features/cart/pages/CheckoutPage'))
const WishlistPage = lazy(() => import('@/features/wishlist/pages/WishlistPage'))
const OrdersPage = lazy(() => import('@/features/orders/pages/OrdersPage'))
const OrderDetailPage = lazy(() => import('@/features/orders/pages/OrderDetailPage'))
const ProfilePage = lazy(() => import('@/features/profile/pages/ProfilePage'))
const MyListingsPage = lazy(() => import('@/features/profile/pages/MyListingsPage'))
const SellerOrdersPage = lazy(() => import('@/features/profile/pages/SellerOrdersPage'))
const BusinessProfilePage = lazy(() => import('@/features/profile/pages/BusinessProfilePage'))

// Admin pages
const AdminDashboardPage = lazy(() => import('@/features/admin/pages/AdminDashboardPage'))
const AdminListingsPage = lazy(() => import('@/features/admin/pages/AdminListingsPage'))
const AdminUsersPage = lazy(() => import('@/features/admin/pages/AdminUsersPage'))
const AdminOrdersPage = lazy(() => import('@/features/admin/pages/AdminOrdersPage'))
const AdminCategoriesPage = lazy(() => import('@/features/admin/pages/AdminCategoriesPage'))
const AdminBusinessProfilesPage = lazy(() => import('@/features/admin/pages/AdminBusinessProfilesPage'))
const AdminEmailTemplatesPage = lazy(() => import('@/features/admin/pages/AdminEmailTemplatesPage'))
const AdminReviewPage = lazy(() => import('@/features/admin/pages/AdminReviewPage'))

// Profile extras
const MyBidsPage = lazy(() => import('@/features/profile/pages/MyBidsPage'))

// Admin auction pages
const AdminAuctionsPage = lazy(() => import('@/features/admin/pages/AdminAuctionsPage'))
const AdminAuctionDetailPage = lazy(() => import('@/features/admin/pages/AdminAuctionDetailPage'))

// Error pages
const NotFoundPage = lazy(() => import('@/features/errors/NotFoundPage'))
const ForbiddenPage = lazy(() => import('@/features/errors/ForbiddenPage'))

export const router = createBrowserRouter([
  // ===== Public auth routes =====
  { path: ROUTES.LOGIN, element: withSuspense(LoginPage) },
  { path: ROUTES.REGISTER, element: withSuspense(RegisterPage) },
  { path: ROUTES.FORGOT_PASSWORD, element: withSuspense(ForgotPasswordPage) },
  { path: ROUTES.RESET_PASSWORD, element: withSuspense(ResetPasswordPage) },
  { path: ROUTES.VERIFY_EMAIL, element: withSuspense(VerifyEmailPage) },

  // ===== Marketplace routes =====
  {
    path: '/',
    element: <MarketplaceLayout />,
    children: [
      { index: true, element: withSuspense(HomePage) },
      { path: ROUTES.DEALS, element: withSuspense(DealsPage) },
      { path: ROUTES.LISTINGS, element: withSuspense(ListingsPage) },
      { path: ROUTES.LISTING_DETAIL, element: withSuspense(ListingDetailPage) },
      {
        element: <PrivateRoute />,
        children: [
          { path: ROUTES.CREATE_LISTING, element: withSuspense(CreateListingPage) },
          { path: ROUTES.EDIT_LISTING, element: withSuspense(EditListingPage) },
          { path: ROUTES.CART, element: withSuspense(CartPage) },
          { path: ROUTES.CHECKOUT, element: withSuspense(CheckoutPage) },
          { path: ROUTES.WISHLIST, element: withSuspense(WishlistPage) },
          { path: ROUTES.ORDERS, element: withSuspense(OrdersPage) },
          { path: ROUTES.ORDER_DETAIL, element: withSuspense(OrderDetailPage) },
          { path: ROUTES.PROFILE, element: withSuspense(ProfilePage) },
          { path: ROUTES.MY_LISTINGS, element: withSuspense(MyListingsPage) },
          { path: ROUTES.SELLER_ORDERS, element: withSuspense(SellerOrdersPage) },
          { path: ROUTES.BUSINESS_PROFILE, element: withSuspense(BusinessProfilePage) },
          { path: ROUTES.MY_BIDS, element: withSuspense(MyBidsPage) },
        ],
      },
    ],
  },

  // ===== Admin routes =====
  {
    path: '/admin',
    element: (
      <AdminRoute>
        <AdminLayout />
      </AdminRoute>
    ),
    children: [
      { index: true, element: <Navigate to={ROUTES.ADMIN_DASHBOARD} replace /> },
      { path: 'dashboard', element: withSuspense(AdminDashboardPage) },
      { path: 'listings', element: withSuspense(AdminListingsPage) },
      { path: 'users', element: withSuspense(AdminUsersPage) },
      { path: 'orders', element: withSuspense(AdminOrdersPage) },
      { path: 'categories', element: withSuspense(AdminCategoriesPage) },
      { path: 'business-profiles', element: withSuspense(AdminBusinessProfilesPage) },
      { path: 'email-templates', element: withSuspense(AdminEmailTemplatesPage) },
      { path: 'review', element: withSuspense(AdminReviewPage) },
      { path: 'auctions', element: withSuspense(AdminAuctionsPage) },
      { path: 'auctions/:listingId', element: withSuspense(AdminAuctionDetailPage) },
    ],
  },

  // ===== Error routes =====
  { path: '/forbidden', element: withSuspense(ForbiddenPage) },
  { path: '*', element: withSuspense(NotFoundPage) },
])
