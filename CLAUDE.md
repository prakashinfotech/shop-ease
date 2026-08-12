# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Docs

Reference files in [`docs/`](docs/):

| File | Purpose |
|------|---------|
| [docs/requirements.md](docs/requirements.md) | System requirements, feature list, user stories |
| [docs/architecture.md](docs/architecture.md) | Layer structure, backend/frontend architecture overview |
| [docs/api-contract.md](docs/api-contract.md) | API base URL, response envelope, endpoint contracts |
| [docs/database.md](docs/database.md) | DB engine, EF Core setup, migration strategy, schema notes |
| [docs/design.md](docs/design.md) | Tailwind design system, color tokens, component conventions |
| [docs/coding-rules.md](docs/coding-rules.md) | Naming conventions, patterns, do/don't rules for this codebase |
| [docs/task.md](docs/task.md) | Completed and pending work tracker (phased task checklist) |

---

## Commands

### Frontend (`cd Frontend`)
```bash
npm install          # install deps
npm run dev          # dev server at http://localhost:5173
npm run build        # production build
npm run lint         # ESLint
npm run preview      # preview prod build
```

### Backend (`cd Backend`)
```bash
dotnet run --project src/EBayClone.API                    # run API at http://localhost:5000
dotnet build                                               # build all projects
dotnet ef migrations add <Name> \
  --project src/EBayClone.Infrastructure \
  --startup-project src/EBayClone.API                     # add migration
dotnet ef database update \
  --project src/EBayClone.Infrastructure \
  --startup-project src/EBayClone.API                     # apply migrations manually
```

The API **auto-migrates on startup** (`Program.cs` calls `db.Database.MigrateAsync()`), so manual migration is only needed for CI or fresh environments.

**There are no unit or integration tests** in this project (no `.Tests` projects, no frontend spec files).

### Full stack
```bash
docker-compose up -d    # SQL Server + backend + frontend (Nginx on :80)
```

Default URLs: Frontend `:5173` (dev) / `:80` (Docker), Backend API `:5000`, Swagger `/swagger`, SQL Server `:1433`.

**Database**: Azure SQL (`shopease.database.windows.net`, `Initial Catalog=shopease-one`). Connection string is in `appsettings.json`. The API auto-migrates on startup; seed data (admin user ID `00000000-0000-0000-0000-000000000001` + 8 categories) is applied via EF Core `HasData()`.

---

## Architecture

### Backend: Onion Architecture

```
EBayClone.API          → thin controllers, middleware, DI composition
EBayClone.Infrastructure → EF Core, GenericRepository, JwtService, BcryptPasswordHasher
EBayClone.Application  → services (AuthService, ListingService, …), DTOs, FluentValidation validators, interfaces
EBayClone.Domain       → entities, enums, IRepository<T>
```

**Repository pattern (no Unit of Work).** `IRepository<T>` is the only data-access abstraction. It exposes `SaveChangesAsync` directly — services call `repository.SaveChangesAsync(ct)` after mutations. There is no `IUnitOfWork`.

**DI registration:**
- `AddInfrastructure` → DbContext, `IRepository<>` → `GenericRepository<>`, `IJwtService`, `IPasswordHasher`
- `AddApplication` → all service interfaces + `AddValidatorsFromAssembly`
- `AddJwtAuthentication` / `AddCorsPolicy` → in `ServiceExtensions`

### Key backend patterns

- **`BaseEntity`** fields: `Id` (Guid), `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`. No `CreatedBy`.
- **Soft deletes**: `repository.SoftDelete(entity)` sets `IsDeleted = true`. EF query filters in `IEntityTypeConfiguration` classes exclude soft-deleted rows globally.
- **All responses use `ApiResponse<T>`** envelope. Controllers: `return Ok(ApiResponse<T>.Success(data))`.
- **Paginated responses use `PagedResult<T>`** (`Application/Common/PagedResult.cs`) — includes `Items`, `TotalCount`, `Page`, `PageSize`. Wrap it: `ApiResponse<PagedResult<T>>.Success(pagedResult)`.
- **FluentValidation**: validators in `Application/Validators/`, auto-registered via `AddValidatorsFromAssembly`.
- **JWT**: access token 15 min, refresh token 7 days. Claims: `sub` = userId, role = MS role claim URI. `ClockSkew` is 30 s. `MapInboundClaims = false` is set — the `sub` claim is **not** remapped automatically. `NameClaimType = "sub"` is set in `TokenValidationParameters`, so controllers must use `User.FindFirstValue("sub")` (never `ClaimTypes.NameIdentifier`).
- **IPasswordHasher**: `IPasswordHasher` interface lives in Application layer (`Hash`/`Verify`). `BcryptPasswordHasher` implements it in Infrastructure using BCrypt.Net-Next. Application layer never references BCrypt directly — this is the clean boundary pattern.
- **IQueryable**: `IRepository<T>.Query()` returns `IQueryable<T>` for building filterable/pageable queries in services; materialize with `ToListAsync()` at the service layer.
- **Logging**: Serilog is configured in `Program.cs` with Console + rolling file sinks. Inject `ILogger<T>` where needed; do not use `Console.Write*`.
- **Stub services**: `ConsoleEmailService` (implements `IEmailService`) writes to console — not production-ready. `LocalFileStorageService` (implements `IFileStorageService`) writes to local disk. Both are registered in `AddInfrastructure`.
- **Auth token flows**: email verification and password reset use `GenerateSecureToken()` (32-byte random, base64url). Tokens stored on `User` with an expiry field. `ForgotPassword` always returns 200 to prevent email enumeration.
- **Startup seeders**: after `MigrateAsync()`, `CategoryFormSeeder.SeedAsync()` and `ListingAndUserSeeder.SeedAsync()` run. Both are idempotent. `CategoryFormSeeder` uses MD5-deterministic GUIDs (`CreateGuid($"category:{key}")`, `CreateGuid($"attribute:{key}:{name}")`) so re-runs never create duplicates.
- **Dynamic category attributes**: `CategoryAttribute` defines per-category form fields (`DataType`, `IsRequired`, `IsFilterable`, validation constraints, `Placeholder`, `Unit`). `AttributeOption` holds dropdown/multi-select choices. `ListingAttributeValue` stores chosen values per listing. Attributes support conditional visibility via `ConditionAttributeId` + `ConditionOperator` + `ConditionValue`.
- **API versioning**: all routes are prefixed `/api/v1/`. Health check at `/health`.

### Domain entities & enums

Entities (all extend `BaseEntity`): `User`, `BusinessProfile`, `Listing`, `ListingImage`, `Category`, `CategoryAttribute`, `AttributeOption`, `ListingAttributeValue`, `ListingView`, `Cart`, `CartItem`, `Order`, `OrderItem`, `RefreshToken`, `UserDocument`.

Enums: `AccountType` (Personal/Business), `UserRole` (User/Admin — no Buyer/Seller; role is orthogonal to AccountType), `ListingStatus` (Draft/Active/Sold/Ended/Removed), `ListingType` (FixedPrice/Auction), `OrderStatus`, `AttributeDataType` (Text/Number/Decimal/Boolean/Date/Dropdown/MultiSelect), `ConditionalOperator` (Equals/NotEquals/Contains/GreaterThan/LessThan), `DocumentType`, `VerificationStatus`.

### Frontend: Feature-Based

```
src/
  app/Router.jsx          — all routes; all pages lazy-loaded with Suspense
  constants/api.js        — API_ENDPOINTS + API_BASE_URL (from VITE_API_BASE_URL env var)
  constants/routes.js     — ROUTES path constants
  constants/enums.js      — frontend enum mirrors
  services/api.js         — Axios instance (see below)
  store/authStore.js      — Zustand + localStorage persist
  store/cartStore.js      — Zustand cart state
  store/wishlistStore.js  — Zustand + localStorage persist (client-only, not synced to backend)
  utils/assets.js         — `assetUrl(url)`: converts relative backend paths to full URLs using API_BASE_URL
  features/[feature]/     — components, hooks, pages, services per feature
  layouts/                — MarketplaceLayout, AdminLayout
  components/common/      — Button, Input, Select, Modal, Badge, Pagination, Spinner, PrivateRoute, AdminRoute
  utils/formatters.js     — currency, date, and other display formatters
```

**`@` alias** resolves to `./src` (configured in `vite.config.js`).

**Vite dev proxy**: requests to `/api/*` are proxied to `http://localhost:5000` in development, so feature services call `/api/...` paths directly without hardcoding the backend URL.

### Key frontend patterns

- **Axios instance** (`services/api.js`): attaches Bearer token from `authStore`; **response interceptor unwraps `.data`** — callers receive the unwrapped payload, not the raw Axios response. The interceptor also handles `401` with a queued token-refresh flow before retrying.
- **Auth state**: `useAuthStore` (Zustand + `persist`) stores `user`, `accessToken`, `refreshToken`, `isAuthenticated` in `localStorage` under key `auth-storage`.
- **React Query**: use `useQuery` / `useMutation` for all server state. No `useEffect` + fetch.
- **Forms**: React Hook Form (`useForm`) + Zod (`zodResolver`) for schema validation. Define a Zod schema, pass it to `zodResolver`, wire fields with `register`. Do not write manual validation logic.
- **Route guards**: `PrivateRoute` redirects unauthenticated users; `AdminRoute` redirects non-admins to `/forbidden`.
- **Dynamic attributes on forms**: use `DynamicAttributeFields` component (passes `register`, `errors`, `watch` from RHF). Attribute conditional visibility is computed by `isAttributeVisible(attribute, values)` in `features/listings/utils/attributeVisibility.js` — `ConditionalOperator` int values map to Equals=0, NotEquals=1, Contains=2, GreaterThan=3, LessThan=4.
- **Image URLs**: backend serves uploaded files as static assets. Use `assetUrl(url)` from `utils/assets.js` for any image path that may be a relative backend path — it handles both relative and absolute URLs.

---

## Important Constraints

- **Single user table**: `AccountType` = `Personal | Business`. No buyer/seller split.
- **Soft deletes only**: never call a physical DELETE; use `repository.SoftDelete(entity)`.
- **Feature isolation**: frontend features must not import from other features.
- **Tailwind only**: no custom CSS files, no inline styles.
- **Thin controllers**: one line — `return Ok(await _service.DoSomethingAsync(request, ct))`.
- **All list endpoints**: support `page`, `pageSize`, `sortBy`, `sortDirection` query params + feature-specific filters.
- **CORS**: policy named `"FrontendPolicy"`, origins from `CorsSettings:AllowedOrigins` in config.
- **Admin layout**: admin routes (`/admin/*`) use `AdminLayout`; other authenticated routes use `MarketplaceLayout`.

---

## UI Design Tokens

Tailwind custom colors:
- Primary (ShopEase blue): `#0064d2`
- Secondary (ShopEase red): `#e53238`
- Accent (ShopEase yellow): `#f5af02`
- Success: `#86b817`
- Background: `#f3f3f3`
- Text primary: `#111820`

Component conventions:
- Cards: `rounded-lg shadow-sm border border-gray-200 bg-white`
- Buttons: `primary` (blue filled), `secondary` (outlined), `danger` (red), `ghost` (no border)
- Inputs: `border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500`
