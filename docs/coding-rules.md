# Coding Rules

## Backend

### Naming
| Thing | Convention | Example |
|-------|-----------|---------|
| Controllers | Plural noun + `Controller` | `ListingsController` |
| Request DTOs | `[Action][Entity]Request` | `CreateListingRequest` |
| Response DTOs | `[Entity]Response` | `ListingResponse` |
| Services | `I[Entity]Service` interface | `IListingService` |
| Validators | `[Request]Validator` | `CreateListingRequestValidator` |
| Migrations | PascalCase description | `AddListingVersionTable` |

### Controller Rules
- Thin: one line per action → `return Ok(await _service.DoSomethingAsync(request, ct));`
- Never put business logic in controllers
- Always return `ApiResponse<T>` envelope
- Use `User.FindFirstValue("sub")` for userId (never `ClaimTypes.NameIdentifier`)
- Paginated: return `ApiResponse<PagedResult<T>>`
- **Never inject `IRepository<T>` into controllers.** Controllers inject Application service interfaces only.
- **Never define `public record` or `public class` DTOs inside controller files.** All request/response types live in `Application/DTOs/` (or `API/Models/` for `IFormFile` wrappers — ASP.NET Core types must not enter the Application layer).
- Controllers must only contain: route attributes, constructor injection of services, action methods, return logic.

### Service Rules
- All public methods async, `CancellationToken ct` last param
- Inject `IRepository<T>`, `ILogger<T>`, other services — never `DbContext` directly
- Call `repository.SaveChangesAsync(ct)` after mutations
- Build queries via `repository.Query()` → compose → `ToListAsync()`
- Throw or return `ApiResponse.Failure(...)` on error — no swallowing

### Repository Rules
- Never physical DELETE — use `repository.SoftDelete(entity)`
- No Unit of Work — `SaveChangesAsync` on each repository call
- EF query filters handle `IsDeleted` globally — never manually add `WHERE IsDeleted=false`

### Entity Rules
- All entities extend `BaseEntity` (Id Guid, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
- No `CreatedBy` field on `BaseEntity`
- Navigation properties for all FK relationships
- Enum values: always specify int values explicitly in definition

### Validation
- FluentValidation only — no manual `if` checks in services/controllers
- Validators in `Application/Validators/`
- Auto-registered; no manual `AddValidator<>` calls needed

### Logging
- `ILogger<T>` only — never `Console.Write*`
- Log errors with `Log.Error(ex, "message")` in catch blocks
- Info-level for business events; Debug for diagnostics

### Infrastructure Boundary
- Application layer never references `BCrypt`, `EF Core`, `Jwt` libraries directly
- `IPasswordHasher` interface in Application; `BcryptPasswordHasher` in Infrastructure
- `IEmailService`, `IFileStorageService` interfaces in Application
- Application layer must not reference `Microsoft.AspNetCore.Http` (e.g. `IFormFile`). File upload wrappers go in `API/Models/`.

### Comments
- No comments unless WHY is non-obvious (hidden constraint, workaround, invariant)
- No XML doc comments on obvious methods
- No "added for issue #X" comments

---

## Frontend

### File/Folder Rules
- Features must not import from other features (`features/auth` cannot import from `features/listings`)
- Shared code goes in `components/common/`, `utils/`, `services/`, `store/`
- New feature = new folder under `features/` with `components/`, `hooks/`, `pages/`, `services/`

### Styling
- Tailwind CSS only — no custom CSS files, no `style={{}}` inline styles
- Design tokens:
  - Primary blue: `#0064d2`
  - Secondary red: `#e53238`
  - Accent yellow: `#f5af02`
  - Success: `#86b817`
  - Background: `#f3f3f3`
  - Text: `#111820`
- Cards: `rounded-lg shadow-sm border border-gray-200 bg-white`
- Inputs: `border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500`
- Button variants: `primary` (blue filled), `secondary` (outlined), `danger` (red), `ghost` (no border)

### Data Fetching
- React Query for all server state — `useQuery` / `useMutation`
- No `useEffect + fetch` or `useEffect + axios` patterns
- Custom hooks wrap React Query calls (e.g. `useListings`, `useOrders`)

### Forms
- React Hook Form + Zod always — no manual form state or manual validation
- `zodResolver(schema)` passed to `useForm`
- Wire fields with `register`, errors from `formState.errors`

### API Calls
- Use `services/api.js` Axios instance always — never create new `axios.create()`
- Endpoint strings from `constants/api.js` — never hardcode `/api/v1/...` inline
- Interceptor unwraps `.data` — callers receive payload directly

### Constants/Enums
- All routes in `constants/routes.js` as `ROUTES.X`
- All API paths in `constants/api.js` as `API_ENDPOINTS.X`
- All enum values in `constants/enums.js` mirroring backend int values exactly
- Never hardcode string paths or magic numbers

### Images
- Always use `assetUrl(url)` from `utils/assets.js` for backend image paths
- Handles both relative paths and absolute URLs

### State
- Auth state: `useAuthStore` — never localStorage directly
- Cart: `useCartStore`
- No global state for server data — React Query handles caching

### Route Config
- All routes in `app/Router.jsx` with `React.lazy` + `<Suspense>`
- Admin routes under `AdminLayout`, authenticated routes under `MarketplaceLayout`
- Protected with `PrivateRoute` / `AdminRoute` wrappers

### Checklist Before PR (Architecture)
- [ ] No `IRepository<T>` injected directly into any controller
- [ ] No DTO/record/class defined inside a controller file
- [ ] Business logic in Application service, not controller action
- [ ] `IFormFile` types in `API/Models/`, not `Application/DTOs/`
- [ ] DbContext referenced only in Infrastructure repositories

### Checklist Before PR
- [ ] New API endpoint added to `constants/api.js`
- [ ] New enum added to `constants/enums.js` with correct int values
- [ ] New route added to `constants/routes.js` and `Router.jsx`
- [ ] New service registered in DI (`AddApplication` or `AddInfrastructure`)
- [ ] New entity has EF `IEntityTypeConfiguration` + soft-delete query filter
- [ ] Migration added if schema changed
- [ ] Loading, error, and empty states handled in UI
- [ ] No feature cross-imports
