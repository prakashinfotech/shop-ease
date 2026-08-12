# Project Task Tracker — ShopEase

## Phase 1: Project Scaffolding & Infrastructure

- [x] Onion architecture setup (Domain / Application / Infrastructure / API projects)
- [x] EF Core + SQL Server (`AppDbContext`, connection string, auto-migrate on startup)
- [x] `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`)
- [x] `GenericRepository<T>` + `IRepository<T>` with soft-delete support
- [x] Serilog logging (console + rolling file)
- [x] `ApiResponse<T>` envelope + `PagedResult<T>`
- [x] Swagger / OpenAPI setup
- [x] CORS policy (`FrontendPolicy`)
- [x] Global error handling middleware
- [x] Docker Compose (SQL Server + backend + Nginx frontend)
- [x] Vite dev proxy (`/api/*` → `http://localhost:5000`)

---

## Phase 2: Auth & User Management

- [x] `User` entity + `UserRole` enum (User / Admin) + `AccountType` (Personal / Business)
- [x] `AuthService` — register, login, refresh token, logout
- [x] JWT access token (15 min) + refresh token (7 days)
- [x] `BcryptPasswordHasher` (`IPasswordHasher` clean boundary)
- [x] `JwtService` (`IJwtService`)
- [x] `AuthController` — register / login / refresh / logout
- [x] Email verification flow (`GenerateSecureToken`, verify endpoint)
- [x] Forgot password / reset password flow (always returns 200)
- [x] `FluentValidation` validators — Register, Login, ForgotPassword, ResetPassword
- [x] `UsersController` — get profile, update profile, upload avatar
- [x] `UserService` — profile read/update with business verification checks

---

## Phase 3: Business Profile

- [x] `BusinessProfile` entity + `UserDocument` entity
- [x] `VerificationStatus` + `DocumentType` enums
- [x] `BusinessProfileService` + `IBusinessProfileService`
- [x] `BusinessProfileController` — create/update/get profile, upload documents
- [x] `BusinessProfileRequestValidator`

---

## Phase 4: Categories (Dynamic & Hierarchical)

- [x] `Category` entity (parent/child hierarchy)
- [x] `CategoryAttribute` entity (per-category form fields, `DataType`, `IsRequired`, `IsFilterable`, conditional visibility)
- [x] `AttributeOption` entity (dropdown / multi-select choices)
- [x] `AttributeDataType` + `ConditionalOperator` enums
- [x] `CategoryFormSeeder` (MD5-deterministic GUIDs, idempotent)
- [x] `CategoryService` + `CategoriesController`
- [x] `ListingAttributeValue` entity + configuration

---

## Phase 5: Listings

- [x] `Listing` entity + `ListingImage` + `ListingStatus` / `ListingType` enums
- [x] `ListingService` + `IListingService`
- [x] `ListingsController` — CRUD, image upload, pagination, filters, sort
- [x] `LocalFileStorageService` (stub `IFileStorageService`)
- [x] Discount amount field on listings (`DiscountAmount` migration)
- [x] `ListingView` entity + `ListingViewConfiguration` (recent views tracking)
- [x] `DealsPage` frontend + discount calculation
- [x] `DynamicAttributeFields` component + `isAttributeVisible` utility
- [x] `CreateListingPage`, `EditListingPage`, `ListingDetailPage`
- [x] `ListingForm` component with RHF + Zod
- [x] `ListingCard`, `ListingGrid`, `ListingsPage`, `HomePage`
- [x] `FilterSidebar`, `CategoryFilter`, `CategoryBrowserGrid`, `SearchBar`
- [x] My listings page (user sees only own listings)
- [x] Pagination component

---

## Phase 6: Cart & Orders

- [x] `Cart` + `CartItem` entities + `CartConfiguration`
- [x] `CartController`
- [x] `CartPage` + `CheckoutPage` frontend
- [x] `cartStore` (Zustand)
- [x] `Order` + `OrderItem` entities + `OrderStatus` enum
- [x] `OrderService` + `OrdersController`
- [x] `OrdersPage`, `OrderDetailPage`, `SellerOrdersPage` frontend
- [x] Payment + shipping details on orders

---

## Phase 7: Wishlist

- [x] `wishlistStore` (Zustand + localStorage, client-only)
- [x] `WishlistPage` frontend

---

## Phase 8: Admin Panel

- [x] `AdminService` — dashboard stats, user/listing/order management queries
- [x] `AdminController` — users, listings, orders management endpoints
- [x] Listing approval workflow (`ListingVersion`, `ListingApprovalLog`, `ListingApprovalService`)
- [x] `ListingVersionStatus` + `ApprovalAction` enums
- [x] `AdminLayout` (sidebar + navbar)
- [x] `AdminDashboardPage`
- [x] `AdminUsersPage`
- [x] `AdminListingsPage` + `AdminReviewPage`
- [x] `AdminOrdersPage`
- [x] `AdminBusinessProfilesPage`
- [x] `AdminCategoriesPage`
- [x] `AdminDataTable` reusable component

---

## Phase 9: Email Templates

- [x] `EmailTemplate` entity + `EmailTemplateType` enum
- [x] `EmailTemplateService` + `IEmailTemplateService`
- [x] `ConsoleEmailService` (stub `IEmailService`)
- [x] `SmtpEmailService` (production-ready SMTP)
- [x] Background email service
- [x] `EmailTemplatesController`
- [x] `AdminEmailTemplatesPage` frontend

---

## Phase 10: Frontend Infrastructure

- [x] React Router with lazy-loaded routes + Suspense
- [x] `authStore` (Zustand + localStorage persist)
- [x] Axios instance — Bearer token attach, `.data` unwrap interceptor, queued 401 refresh
- [x] `PrivateRoute` + `AdminRoute` guards
- [x] `MarketplaceLayout` (Navbar + Footer)
- [x] `LoginPage`, `RegisterPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `VerifyEmailPage`
- [x] `ProfilePage`, `BusinessProfilePage`
- [x] `NotFoundPage`, `ForbiddenPage`
- [x] `API_ENDPOINTS` constants + `ROUTES` constants + frontend enum mirrors
- [x] `assetUrl` utility for backend image paths
- [x] `formatters.js` (currency, date)
- [x] Shared branding component
- [x] Common components: `Button`, `Input`, `Select`, `Modal`, `Badge`, `Pagination`, `Spinner`

---

## Pending / Not Yet Done

- [ ] Real payment gateway integration (Stripe / PayPal)
- [ ] Real file storage (S3 / Azure Blob) — currently local disk stub
- [ ] Push notifications
- [ ] Buyer / seller messaging system
- [x] Auction / bidding engine (Phase 11)
- [ ] Search with Elasticsearch or full-text SQL
- [ ] Rate limiting / throttling on API
- [ ] Unit & integration tests
- [ ] CI/CD pipeline
- [ ] Production deployment & secrets management

---

## Phase 11: Live Auction

- [x] `Bid` + `AuctionResult` entities + `AuctionEndReason` enum
- [x] Extend `Listing`: `CurrentBidAmount`, `CurrentBidderId`, `BidCount`, `MinBidIncrement`, `AutoExtendOnBid`
- [x] Migration: `AddAuctionEntities`
- [x] `IAuctionService` / `AuctionService` — place bid, bid history, status, cancel, extend, finalize
- [x] `AuctionEndingBackgroundService` — polls every 10 s, finalizes expired auctions
- [x] `AuctionHub` (SignalR) — `bid-placed`, `auction-ended`, `time-extended`, `auction-cancelled` events
- [x] `AuctionsController` — place bid, bid history, status, active list, my bids
- [x] Extend `AdminController` — list/cancel/extend auctions, view full bid log
- [x] Frontend: `BidPanel`, `BidHistory`, `AuctionBadge`, `ReserveIndicator`, `useAuctionHub`, `useAuctionCountdown`
- [x] Update `ListingDetailPage` — render bid UI for auction listings
- [x] Update `ListingCard` — show badge + current bid for auction listings
- [x] `MyBidsPage` — user bid history (won / outbid / active)
- [x] `AdminAuctionsPage` + `AdminAuctionDetailPage` — manage + full bid log
- [ ] Email notifications — outbid, won, ended (no winner), reserve not met
