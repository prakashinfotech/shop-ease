# Requirements

## System Purpose
Marketplace platform. Users buy + sell via unified account. No buyer/seller split — single `User` entity with `AccountType` (Personal|Business).

## Architecture Requirement
All features (existing and future) must follow Onion Architecture:
- Controller → Application Service → Repository Interface → Repository Implementation → DbContext
- No direct repository injection into controllers
- No DTO definitions inside controller files
- Business logic lives in Application services only

## User Roles
| Role | Capabilities |
|------|-------------|
| Guest | Browse listings, view categories, search/filter |
| User (auth) | Create/edit/delete listings, checkout, orders, manage profile, wishlist |
| Business (auth) | Same as User + business profile + document upload |
| Admin | All User actions + approve/reject listings, manage users, view all orders, manage categories/email templates |

## Core Features

### Auth
- Register (Personal or Business account type)
- Login → JWT access (15 min) + refresh (7 days)
- Email verification (token-based, expiry)
- Forgot/reset password (32-byte secure token)
- Logout (invalidates refresh token)
- `GET /auth/me` returns current user

### Listings
- CRUD with soft-delete
- Status workflow: `Draft → PendingApproval → Active / Rejected → Sold / Ended / Removed`
- Types: FixedPrice, Auction (with StartingBid, ReservePrice, BuyItNowPrice, AuctionStart/EndAt)
- Discount amount on price (`FinalPrice = Price - DiscountAmount`)
- Image upload (multipart), multiple images per listing
- Dynamic category attributes (per-category form fields)
- Search + autocomplete + faceted filters
- Recently viewed (per user, last N days)
- View tracking (`POST /listings/{id}/views`)
- Paginated list with sort/filter

### Listing Approval Workflow
- New listing → submit → `PendingApproval`
- Admin approves → `Active`; rejects → `Rejected` with reason
- Edits to active listing → `ListingVersion` created (pending update), not overwrite
- Admin reviews version; approve merges, reject discards
- All approval actions logged in `ListingApprovalLog`
- Email notifications sent on approve/reject

### Orders & Cart
- Cart in Zustand store (client-side) + backend `Cart`/`CartItem`
- Checkout: validates quantity, creates `Order` + `OrderItem` records
- Order status: `Pending → Confirmed → Shipped → Delivered → Cancelled / Refunded`
- Buyer can cancel pending orders
- Admin views all orders

### Business Profile
- Users with `AccountType=Business` create business profile
- Fields: CompanyName, GstNumber, PanNumber, contact info
- Document upload (up to 10 MB): GST cert, PAN, business reg, address proof
- Verification workflow: `Pending → UnderReview → Verified / Rejected`
- Admin reviews and approves/rejects with reason

### Categories
- Hierarchical (parent/child via `ParentCategoryId`)
- Per-category dynamic attributes: `CategoryAttribute` defines form fields
- Attribute types: Text, Number, Decimal, Boolean, Date, Dropdown, MultiSelect
- Attributes support conditional visibility (`ConditionAttributeId + ConditionalOperator + ConditionValue`)
- Attribute options for Dropdown/MultiSelect stored in `AttributeOption`
- Admin CRUD on categories and attributes

### Admin Dashboard
- Stats: user count, listing count, order count, revenue
- User management: list, suspend, activate
- Listing management: list, view detail, approve, reject, delete, view versions
- Order management: list, filter by status
- Business profile review
- Email template management (CRUD + activate/deactivate)

### Email Templates
- DB-stored templates with `EmailTemplateType` enum
- Types: EmailVerification, ForgotPassword, PasswordChanged, ListingPendingApproval, ListingApproved, ListingRejected
- Versioned; only one active per type at a time
- Admin can create/edit/activate/deactivate

## Business Rules
- Soft deletes only — no physical DELETE
- Single user table — no separate buyer/seller models
- Listing owner can delete own listings; admin can delete any
- Prices in decimal; discount cannot exceed price
- Quantity must be ≥ 1
- Email verification required (not enforced at login, but tracked)
- JWT `sub` claim = userId; use `User.FindFirstValue("sub")`
- `ForgotPassword` always returns 200 (prevents email enumeration)
- File uploads stored at `wwwroot/uploads`; served as static files
- Business profile: one per user
