# Architecture

## Quick Reference

| Concept | Decision |
|---------|---------|
| Backend pattern | Onion Architecture — Domain → Application → Infrastructure → API |
| Data access | Generic Repository (`IRepository<T>`), no Unit of Work |
| Soft deletes | All entities; EF query filters exclude `IsDeleted=true` globally |
| Auth | JWT access (15 min) + refresh (7 days); claims: `sub`=userId |
| Response envelope | `ApiResponse<T>` always; paginated = `ApiResponse<PagedResult<T>>` |
| Frontend state | React Query (server) + Zustand (client) |
| Forms | React Hook Form + Zod |
| Routing | React Router v6, all routes lazy-loaded |

---

## Layer Structure (Onion)
```
EBayClone.API           → controllers, middleware, DI composition, Program.cs, API-layer models
EBayClone.Application   → services, DTOs, validators, interfaces (no infra deps)
EBayClone.Infrastructure → EF Core, GenericRepository, JwtService, BcryptPasswordHasher
EBayClone.Domain        → entities, enums, IRepository<T> (no deps)
```

### Correct dependency flow
```
HTTP Request
    ↓
Controller (API)
    ↓
Application Service Interface (Application)
    ↓
Application Service Implementation (Application)
    ↓
IRepository<T> Interface (Domain)
    ↓
GenericRepository<T> Implementation (Infrastructure)
    ↓
DbContext (Infrastructure)
```

Dependency direction: API → Application → Domain ← Infrastructure → Domain.  
Application never references Infrastructure directly.

### Architecture rules (enforced)
- **Controllers must not inject or call `IRepository<T>` directly.** All data access must go through an Application service.
- **Controllers must not contain DTO/request/response record or class definitions.** All DTOs live in `Application/DTOs/` or, for HTTP-specific types like `IFormFile` wrappers, in `API/Models/`.
- **Business logic must not be written inside controllers.** Controllers are HTTP adapters only: route attribute, constructor injection, one-line action that calls a service, return result.
- **DbContext must only be used inside Infrastructure repositories.** Application services never reference `DbContext` or EF Core namespaces directly.

## Backend Patterns

### Repository
- `IRepository<T>` (Domain) ← `GenericRepository<T>` (Infrastructure)
- Exposes: `GetByIdAsync`, `GetAllAsync`, `Query()` (IQueryable), `AddAsync`, `UpdateAsync`, `SoftDelete`, `SaveChangesAsync`
- No Unit of Work. Services call `repository.SaveChangesAsync(ct)` after mutations.
- `IQueryable<T>` via `Query()` for composable filters; materialize with `ToListAsync()` in service layer.

### Services
- Registered via `AddApplication()` in `ApplicationExtensions.cs`
- One interface per service in Application layer
- Services never reference EF Core directly — only `IRepository<T>`
- Admin-specific query logic lives in `IAdminService` / `AdminService`

### DI Registration
| Extension | Registers |
|-----------|-----------|
| `AddInfrastructure` | DbContext, `IRepository<>` → `GenericRepository<>`, `IJwtService`, `IPasswordHasher`, `IEmailService`, `IFileStorageService` |
| `AddApplication` | All service interfaces + `AddValidatorsFromAssembly` |
| `AddJwtAuthentication` | JWT bearer + token validation params |
| `AddCorsPolicy` | `"FrontendPolicy"` from `CorsSettings:AllowedOrigins` |
| `AddSwaggerWithJwt` | Swagger + JWT auth button |

### Soft Deletes
- All entities extend `BaseEntity` (Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
- `repository.SoftDelete(entity)` sets `IsDeleted=true`, `DeletedAt=DateTime.UtcNow`
- EF query filters in `IEntityTypeConfiguration` classes exclude `IsDeleted=true` globally
- Admin queries can bypass filter for `IncludeDeleted` scenarios

### Response Envelope
```csharp
ApiResponse<T>.Success(data)          // 200
ApiResponse<T>.Failure("message")     // 4xx
ApiResponse<PagedResult<T>>.Success(pagedResult)  // paginated
```
`PagedResult<T>` fields: `Items`, `TotalCount`, `Page`, `PageSize`.

### Validation
- FluentValidation validators in `Application/Validators/`
- Auto-registered via `AddValidatorsFromAssembly`
- Controller action filter runs before service call

### JWT / Auth
- Access token 15 min, refresh token 7 days
- Claims: `sub` = userId, role = MS role claim URI
- `MapInboundClaims = false`, `NameClaimType = "sub"` — always use `User.FindFirstValue("sub")`
- Refresh token stored in `RefreshToken` table (soft-deletable)

### Logging
- Serilog configured from `appsettings.*.json`
- Console + rolling file (`logs/log-.txt`)
- Dev: Debug level + EF SQL at Information
- Prod: Warning level
- Inject `ILogger<T>`; never `Console.Write*`

### Startup Sequence (Program.cs)
1. Serilog bootstrap
2. Service registration (controllers, swagger, health, infra, app, JWT, CORS)
3. Build app
4. Create `wwwroot/uploads/documents` dir
5. `MigrateAsync()` → `CategoryFormSeeder` → `ListingAndUserSeeder` → `EmailTemplateSeeder`
6. Middleware pipeline: ExceptionMiddleware → Swagger → StaticFiles → SerilogRequestLogging → CORS → Auth → Authorization → Controllers → HealthChecks

### Seeder Pattern
- All seeders idempotent (safe to re-run)
- `CategoryFormSeeder` uses MD5-deterministic GUIDs: `CreateGuid($"category:{key}")`, `CreateGuid($"attribute:{key}:{name}")`
- Default admin: `00000000-0000-0000-0000-000000000001`

### Infrastructure Services
| Interface | Implementation | Note |
|-----------|---------------|-------|
| `IEmailService` | `SmtpEmailService` (always) | Console-fallback when `Host` is empty |
| `IFileStorageService` | `LocalFileStorageService` | Saves to `wwwroot/uploads` |

### SMTP Email Service (`SmtpEmailService`)
- Registered as `IEmailService` in `AddInfrastructure` for all environments
- **Never throws to caller** — all SMTP exceptions caught + logged; API never fails due to email
- `SmtpSettings:Host` empty → logs `[EMAIL-CONSOLE]` to Serilog and returns (dev fallback)
- `SmtpSettings:Host` set → connects via MailKit, `StartTls` if `EnableSsl=true`
- Template rendering via `IEmailTemplateService.GetActiveTemplateAsync()` — no active template → logs warning, skips send
- Placeholder syntax: `{{Key}}` replaced from `Dictionary<string,string>` context

Config per environment:
| Setting | Base (`appsettings.json`) | Development | Production |
|---------|--------------------------|-------------|------------|
| Host | `""` (console fallback) | `sandbox.smtp.mailtrap.io` | set via env var |
| Port | `587` | `587` | `587` |
| Username | `""` | Mailtrap key | set via env var |
| Password | `""` | Mailtrap key | set via env var |
| FromEmail | `noreply@shopease.com` | `ayushkale85.33@gmail.com` | set via env var |
| FromName | `"ShopEase"` | `"ShopEase (Dev)"` | `"ShopEase"` |
| EnableSsl | `true` | `true` | `true` |

Production env vars (Docker / hosting):
```
SmtpSettings__Host=smtp.sendgrid.net
SmtpSettings__Username=apikey
SmtpSettings__Password=<api-key>
SmtpSettings__FromEmail=noreply@yourdomain.com
SmtpSettings__FromName=ShopEase
```

> `FromName` must not be empty string — use `IsNullOrWhiteSpace` guard (already applied in service).  
> Empty string bypasses `?? fallback`; only null triggers it.

---

## Frontend Architecture (Feature-Based)

```
src/
  app/Router.jsx          → all routes, lazy-loaded with Suspense
  constants/api.js        → API_ENDPOINTS + API_BASE_URL (VITE_API_BASE_URL)
  constants/routes.js     → ROUTES path constants
  constants/enums.js      → frontend enum mirrors (must match backend int values)
  services/api.js         → Axios instance (see below)
  store/authStore.js      → Zustand + localStorage persist
  store/cartStore.js      → Zustand cart state
  store/wishlistStore.js  → Zustand + localStorage persist (client-only)
  utils/assets.js         → assetUrl(url): relative backend path → full URL
  utils/formatters.js     → currency, date formatters
  features/[feature]/     → components, hooks, pages, services
  layouts/                → MarketplaceLayout, AdminLayout
  components/common/      → Button, Input, Select, Modal, Badge, Pagination, Spinner, PrivateRoute, AdminRoute
```

### Axios Instance (`services/api.js`)
- Attaches `Authorization: Bearer <token>` from `authStore`
- **Response interceptor unwraps `.data`** — callers get payload, not raw Axios response
- 401 → queued token-refresh flow → retry original request

### State Management
| Store | Persistence | Purpose |
|-------|-------------|---------|
| `authStore` | localStorage (`auth-storage`) | user, accessToken, refreshToken, isAuthenticated |
| `cartStore` | Memory | cart items, totals |
| `wishlistStore` | localStorage | wishlist (client-only, not synced) |

### Data Fetching
- React Query (`useQuery` / `useMutation`) for all server state
- No `useEffect + fetch` pattern

### Forms
- React Hook Form (`useForm`) + Zod (`zodResolver`)
- `DynamicAttributeFields` component for category attributes
- `isAttributeVisible(attribute, values)` in `features/listings/utils/attributeVisibility.js`

### Route Guards
- `PrivateRoute` → redirects unauthenticated to `/login`
- `AdminRoute` → redirects non-admin to `/forbidden`
- Admin routes (`/admin/*`) use `AdminLayout`
- Authenticated routes use `MarketplaceLayout`

### Vite Dev Proxy
`/api/*` → `VITE_API_BASE_URL` (default `http://localhost:5000`) — no CORS issues in dev.

### `@` Alias
Resolves to `./src` via `vite.config.js`.

---

## Sequence Diagrams

### Auth: Register → Login → Token Refresh

```
Browser          API (AuthController)      AuthService         DB (Users / RefreshTokens)
  │                       │                     │                         │
  │──POST /register──────►│                     │                         │
  │                       │──RegisterAsync──────►│                         │
  │                       │                     │──check email unique─────►│
  │                       │                     │◄────────────────────────│
  │                       │                     │──BCrypt.Hash(password)  │
  │                       │                     │──AddAsync(user)─────────►│
  │                       │                     │──SaveChangesAsync───────►│
  │                       │                     │──GenerateJwt + refresh  │
  │                       │                     │──AddAsync(refreshToken)─►│
  │◄──200 {accessToken, refreshToken}────────────│                         │
  │                                                                        │
  │  (15 min later — access token expired)                                 │
  │                                                                        │
  │──POST /refresh────────►│                     │                         │
  │                       │──RefreshAsync───────►│                         │
  │                       │                     │──find token in DB───────►│
  │                       │                     │──validate expiry + revoked│
  │                       │                     │──SoftDelete(oldToken)───►│
  │                       │                     │──GenerateJwt (new)      │
  │                       │                     │──AddAsync(newToken)─────►│
  │◄──200 {newAccessToken, newRefreshToken}───────│                         │
```

### Listing: Create → Submit → Admin Approve

```
Seller           API (ListingsController)   ListingService        AdminService
  │                       │                     │                      │
  │──POST /listings───────►│                     │                      │
  │                       │──CreateAsync────────►│                      │
  │                       │                     │──AddAsync(listing)   │
  │                       │                     │  Status=Draft        │
  │◄──200 {listingId}─────│                     │                      │
  │                       │                     │                      │
  │──POST /listings/{id}/submit─►│              │                      │
  │                       │──SubmitAsync────────►│                      │
  │                       │                     │──Status=PendingApproval
  │                       │                     │──SaveChangesAsync    │
  │                       │                     │──SendEmail(pending)  │
  │◄──200─────────────────│                     │                      │
  │                       │                     │                      │
                    Admin ──POST /admin/listings/{id}/approve──────────►│
                          │                                             │
                          │                                  ──Status=Active
                          │                                  ──LogApproval
                          │                                  ──SendEmail(approved)
                    Admin ◄──200────────────────────────────────────────│
```

### Frontend: API Call with Token Refresh Queue

```
React Component    api.js (Axios interceptor)    authStore    Backend API
       │                    │                       │               │
       │──useQuery──────────►│                       │               │
       │                    │──GET /listings────────────────────────►│
       │                    │◄──401 Unauthorized────────────────────│
       │                    │                                        │
       │                    │ (isRefreshing=false → start refresh)   │
       │                    │──POST /auth/refresh───────────────────►│
       │                    │◄──200 {newAccessToken}─────────────────│
       │                    │──setTokens(newTokens)──►│              │
       │                    │                                        │
       │  (retry queued requests with new token)                     │
       │                    │──GET /listings (with new token)───────►│
       │                    │◄──200 {data}──────────────────────────│
       │◄──unwrapped data───│                                        │
```

### Order: Checkout Flow

```
Buyer            API (OrdersController)    OrderService       DB
  │                     │                      │               │
  │──POST /checkout─────►│                      │               │
  │                     │──CheckoutAsync───────►│               │
  │                     │                      │──validate listings active
  │                     │                      │──validate quantity available
  │                     │                      │──compute TotalAmount
  │                     │                      │──AddAsync(order)──────►│
  │                     │                      │──AddRange(orderItems)──►│
  │                     │                      │──decrement quantities──►│
  │                     │                      │──SaveChangesAsync──────►│
  │◄──200 {OrderResponse}│                      │               │
```

---

## Entity Relationship Summary

```
User ──< Listing (SellerId)
User ──< Order (BuyerId)
User ──< RefreshToken
User ──1 Cart
User ──1 BusinessProfile ──< UserDocument
User ──< ListingView

Category ──< Category (self-ref: ParentCategoryId)
Category ──< CategoryAttribute ──< AttributeOption
CategoryAttribute ──< CategoryAttribute (self-ref: ConditionAttributeId)

Listing ──< ListingImage
Listing ──< ListingAttributeValue
Listing ──< OrderItem
Listing ──< CartItem
Listing ──< ListingView
Listing ──< ListingVersion
Listing ──< ListingApprovalLog

Order ──< OrderItem
Cart ──< CartItem

EmailTemplate (standalone, versioned)
```

**Soft-delete behavior:**
- All entities extending `BaseEntity` have `IsDeleted` / `DeletedAt`
- EF global query filter excludes `IsDeleted=true` on every query
- Exception: Admin queries pass `IncludeDeleted=true` to bypass filter

**Entities with unique constraints:**
- `User.Email` — unique index
- `Cart.UserId` — one cart per user
- `BusinessProfile.UserId` — one profile per user
- `Order.OrderNumber` — unique

---

## Data Flow

### Request path (backend)

```
HTTP Request
  ↓
ExceptionMiddleware (catch unhandled → ApiResponse failure)
  ↓
SerilogRequestLogging
  ↓
CORS middleware ("FrontendPolicy")
  ↓
JWT Authentication middleware
  ↓
Authorization middleware
  ↓
Controller action
  │  FluentValidation runs before action body (auto-registered)
  ↓
Application Service method
  │  Uses IRepository<T>.Query() → composes IQueryable<T>
  │  Materializes with .ToListAsync()
  │  Calls .SaveChangesAsync(ct) after mutations
  ↓
GenericRepository<T> (Infrastructure)
  ↓
AppDbContext (EF Core)
  │  Global query filters: WHERE IsDeleted=0
  ↓
SQL Server
```

### Frontend request path

```
React Component (useQuery / useMutation)
  ↓
Feature service function (e.g. listingService.getListings())
  ↓
api.js Axios instance
  │  Request interceptor: attach Bearer token from authStore
  ↓
Vite dev proxy (/api/* → http://localhost:5000)   [dev only]
  ↓
Backend API
  ↓
Response interceptor (api.js)
  │  .data unwrap: caller receives payload, not raw Axios response
  │  401 handling: queue requests → refresh → retry
  ↓
React Query cache
  ↓
Component re-render
```

### Image upload path

```
User selects file
  ↓
POST /api/v1/listings/images (multipart/form-data)
  ↓
ListingsController → IListingService.UploadImageAsync()
  ↓
IFileStorageService.SaveAsync()
  ↓
LocalFileStorageService → wwwroot/uploads/<guid>.<ext>
  ↓
Returns relative URL: /uploads/<guid>.<ext>
  ↓
Frontend: assetUrl(relativeUrl) → API_BASE_URL + relativeUrl
  ↓
Nginx / ASP.NET static files middleware serves the file
```

---

## Key Design Decisions

| Decision | Rationale |
|---------|---------|
| No `IUnitOfWork` | Single `DbContext` per request; `SaveChangesAsync` on `IRepository<T>` is sufficient; avoids over-engineering |
| `MapInboundClaims = false` | Prevents MS claim-type remapping; `sub` stays `sub`; controllers use `User.FindFirstValue("sub")` |
| `ForgotPassword` always 200 | Prevents email enumeration attacks |
| Deterministic GUIDs for seed data | `CreateGuid("category:electronics")` → stable IDs across re-seeds; no duplicate rows |
| `SmtpEmailService` never throws | Email failures are non-fatal; logged via Serilog; API never returns 500 due to email |
| `LocalFileStorageService` | Dev convenience; swap to `AzureBlobStorageService` for production by implementing same `IFileStorageService` |
| Feature-based frontend structure | Co-locates components, hooks, pages, services per feature; prevents cross-feature coupling |
| Axios response interceptor unwraps `.data` | Callers work with payload directly; 401 refresh is centralized in one place |
