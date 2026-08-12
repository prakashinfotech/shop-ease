# ShopEase — Enterprise Marketplace Platform

An enterprise-grade e-commerce marketplace built with React 19, ASP.NET Core 8, and SQL Server.  
Users can buy and sell items through a unified account system.

---

## Tech Stack

| Layer        | Technology                                  |
|--------------|---------------------------------------------|
| Frontend     | React 19, Vite 6, Tailwind CSS 3            |
| Backend      | ASP.NET Core 8 Web API (Onion Architecture) |
| ORM          | Entity Framework Core 8                     |
| Database     | SQL Server 2022                             |
| Auth         | JWT (15 min) + Refresh Tokens (7 days)      |
| Containers   | Docker + Docker Compose                     |
| Email (dev)  | Mailtrap SMTP sandbox                       |

---

## Port Reference

| Service        | Local Dev                          | Docker (Compose)               |
|----------------|-------------------------------------|--------------------------------|
| Frontend       | http://localhost:5173               | http://localhost:80            |
| Backend API    | http://localhost:5000               | http://localhost:5000          |
| Swagger UI     | http://localhost:5000/swagger       | http://localhost:5000/swagger  |
| Health Check   | http://localhost:5000/health        | http://localhost:5000/health   |
| SQL Server     | localhost:1433                      | localhost:1433                 |

> **Note:** The backend runs inside the container on port `8080`; Docker maps it to `5000` externally.

---

## Prerequisites

| Tool           | Version  | Required for     |
|----------------|----------|------------------|
| Node.js        | 20+      | Frontend         |
| .NET SDK       | 8.0+     | Backend          |
| SQL Server     | 2022     | Database (local) |
| Docker Desktop | latest   | Containerized run|

---

## Quick Start

### Option 1: Docker (Recommended — zero local dependencies)

```bash
# 1. Copy environment file
cp .env.example .env

# 2. Start all services (SQL Server + Backend + Frontend)
docker-compose up -d

# 3. Watch logs
docker-compose logs -f backend
```

Services start in order: SQL Server → Backend (auto-migrates) → Frontend.

| URL                              | What                      |
|----------------------------------|---------------------------|
| http://localhost                 | Frontend                  |
| http://localhost:5000/swagger    | API documentation         |
| http://localhost:5000/health     | Health check              |

### Option 2: Local Development

#### 1. Backend

```bash
cd Backend

# Restore packages
dotnet restore

# Run in Development mode (port 5000)
dotnet run --project src/EBayClone.API
```

The API **auto-migrates and seeds** on first startup — no manual `dotnet ef database update` needed.

To apply migrations manually (CI or fresh DB):

```bash
dotnet ef database update \
  --project src/EBayClone.Infrastructure \
  --startup-project src/EBayClone.API
```

#### 2. Frontend

```bash
cd Frontend

# Copy env file
cp .env.example .env    # or use .env.development (auto-loaded by Vite)

# Install dependencies
npm install

# Start dev server (port 5173, proxies /api/* to localhost:5000)
npm run dev
```

Vite's dev proxy forwards all `/api/*` requests to `VITE_API_BASE_URL` (default `http://localhost:5000`),  
so the frontend never hits CORS issues during development.

---

## Environment Variables

### Root `.env` — Docker Compose

Copy `.env.example` to `.env` and fill in values before running Docker Compose.

```env
# Database
DB_SERVER=localhost
DB_PORT=1433
DB_NAME=ShopEaseDb
DB_USER=sa
DB_PASSWORD=YourStrong@Passw0rd

# JWT
JWT_SECRET=replace-with-a-secret-at-least-32-characters-long
JWT_ISSUER=ShopEaseApi
JWT_AUDIENCE=ShopEaseFrontend
JWT_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# URLs
FRONTEND_URL=http://localhost:5173
BACKEND_URL=http://localhost:5000

# Runtime
ASPNETCORE_ENVIRONMENT=Development
NODE_ENV=development
```

### Backend — `appsettings.*.json`

| File                          | Purpose                                      |
|-------------------------------|----------------------------------------------|
| `appsettings.json`            | Base config (CORS, JWT structure, Serilog)   |
| `appsettings.Development.json`| Dev overrides: Debug logging, Mailtrap SMTP  |
| `appsettings.Production.json` | Prod overrides: Warning-level logs, 15-min JWT|

In production/Docker, secrets are injected via **environment variables** using ASP.NET Core's double-underscore convention:

```
ConnectionStrings__DefaultConnection=Server=...
JwtSettings__Secret=your-secret
CorsSettings__AllowedOrigins=https://your-frontend.com
SmtpSettings__Host=smtp.sendgrid.net
```

Never commit real production secrets — use environment variables or a secrets manager.

### Frontend — `.env.*`

| File                  | Loaded by Vite when                   |
|-----------------------|---------------------------------------|
| `.env`                | All environments (lowest priority)    |
| `.env.development`    | `vite dev` (development mode)         |
| `.env.production`     | `vite build` (production build)       |

```env
VITE_API_BASE_URL=http://localhost:5000   # Backend base URL
VITE_APP_NAME=ShopEase
VITE_APP_VERSION=1.0.0
```

All frontend env vars must be prefixed `VITE_` to be accessible in browser code.

---

## Development Environment Details

### Backend

- **Environment**: `ASPNETCORE_ENVIRONMENT=Development`
- **Port**: `http://localhost:5000`
- **Logging**: Debug level; EF SQL commands logged at Information
- **Email**: Mailtrap SMTP sandbox (no real emails sent)
- **Swagger**: Available at `/swagger`
- **Auto-migrate**: Yes, on every startup
- **Seed data**: Admin user, 8 categories, sample listings

### Frontend

- **Port**: `http://localhost:5173`
- **Hot reload**: Vite HMR enabled
- **API proxy**: `/api/*` → `http://localhost:5000`
- **Source maps**: Enabled

---

## Production Environment Details

### Backend

- **Environment**: `ASPNETCORE_ENVIRONMENT=Production`
- **Port**: Controlled by `ASPNETCORE_URLS` env var (container default: `8080`)
- **Logging**: Warning level; no EF SQL command logging
- **JWT expiry**: 15 min access / 7 day refresh
- **Swagger**: Available at `/swagger` (accessible for API consumers)
- **Static files**: Served from `wwwroot/uploads`

### Frontend

- Built with `npm run build` → static files served by Nginx
- API base URL set at build time via `VITE_API_BASE_URL` Docker build arg
- Nginx proxies `/api/*` to `http://backend:8080/api/` inside Docker network
- All SPA routes fall back to `index.html`

### Docker Production Overrides

To run in production mode with Docker Compose:

```bash
# Create a production .env
ASPNETCORE_ENVIRONMENT=Production
DB_PASSWORD=<strong-password>
JWT_SECRET=<min-32-char-secret>
FRONTEND_URL=https://your-domain.com
BACKEND_URL=https://api.your-domain.com

docker-compose up -d
```

---

## Database

- **Engine**: SQL Server 2022
- **Default DB name**: `ShopEaseDb` (Docker) / `shopease-one` (Azure)
- **Migrations**: Applied automatically on startup via `db.Database.MigrateAsync()`
- **Seed data**:
  - Admin user: `admin@shopease.com` / `Admin@123`
  - Sample user: `user@shopease.com` / `User@123`
  - 8 product categories with dynamic attribute definitions
  - Sample listings

Adding a new migration:

```bash
cd Backend
dotnet ef migrations add <MigrationName> \
  --project src/EBayClone.Infrastructure \
  --startup-project src/EBayClone.API
```

---

## API Documentation

Swagger UI: **`/swagger`** — available in all environments.

Key endpoints:

| Method | Endpoint                       | Auth     | Description                   |
|--------|--------------------------------|----------|-------------------------------|
| POST   | `/api/v1/auth/register`        | No       | Create account                |
| POST   | `/api/v1/auth/login`           | No       | Authenticate, get tokens      |
| POST   | `/api/v1/auth/refresh`         | No       | Refresh access token          |
| GET    | `/api/v1/listings`             | No       | Browse listings (paginated)   |
| POST   | `/api/v1/listings`             | Required | Create listing                |
| GET    | `/api/v1/orders`               | Required | Order history                 |
| POST   | `/api/v1/cart/checkout`        | Required | Checkout cart                 |
| GET    | `/health`                      | No       | Health check                  |

All list endpoints support: `page`, `pageSize`, `sortBy`, `sortDirection`, plus feature-specific filters.  
All responses use `ApiResponse<T>` envelope. Paginated responses use `ApiResponse<PagedResult<T>>`.

---

## Project Structure

```
shopease-one/
├── .env.example                  # Docker Compose environment template
├── docker-compose.yml            # Multi-service container setup
├── CLAUDE.md                     # AI assistant context
│
├── Frontend/                     # React + Vite SPA
│   ├── .env.example              # Frontend env template
│   ├── .env.development          # Dev env (auto-loaded by Vite)
│   ├── .env.production           # Prod env (auto-loaded by Vite build)
│   ├── Dockerfile                # Multi-stage: Node build → Nginx serve
│   ├── nginx.conf                # SPA fallback + /api proxy
│   └── src/
│       ├── app/Router.jsx        # All routes (lazy-loaded)
│       ├── constants/            # API endpoints, routes, enums
│       ├── services/api.js       # Axios instance with auth interceptor
│       ├── store/                # Zustand stores (auth, cart, wishlist)
│       ├── features/             # Feature modules (listings, orders, admin…)
│       ├── layouts/              # MarketplaceLayout, AdminLayout
│       ├── components/common/    # Shared UI components
│       └── utils/                # Formatters, asset URL helpers
│
└── Backend/
    ├── Dockerfile                # Multi-stage: .NET build → ASP.NET runtime
    └── src/
        ├── EBayClone.API/        # Controllers, middleware, Program.cs, DI wiring
        │   ├── appsettings.json             # Base configuration
        │   ├── appsettings.Development.json # Dev overrides
        │   ├── appsettings.Production.json  # Prod overrides
        │   └── Properties/launchSettings.json
        ├── EBayClone.Application/  # Services, DTOs, validators, interfaces
        ├── EBayClone.Infrastructure/ # EF Core, repositories, JWT, email
        └── EBayClone.Domain/       # Entities, enums, IRepository<T>
```

---

## Default Credentials

| Role  | Email                   | Password   |
|-------|-------------------------|------------|
| Admin | admin@shopease.com    | Admin@123  |
| User  | user@shopease.com     | User@123   |

---

## Documentation

| File | Purpose |
|------|---------|
| [docs/setup.md](docs/setup.md) | Local, Docker, and Azure setup in detail |
| [docs/architecture.md](docs/architecture.md) | Layer structure, sequence diagrams, ER summary, data flow |
| [docs/api-contract.md](docs/api-contract.md) | All endpoints, DTOs, request/response shapes |
| [docs/database.md](docs/database.md) | Entity schemas, enum values, EF configuration |
| [docs/design.md](docs/design.md) | Tailwind tokens, component library, layout patterns |
| [docs/requirements.md](docs/requirements.md) | Feature list, business rules, user roles |
| [docs/coding-rules.md](docs/coding-rules.md) | Naming conventions, do/don't rules |

### Postman Collection

Import `postman/shopease.postman_collection.json` into Postman or Insomnia.

Collection variables (`baseUrl`, `accessToken`, `refreshToken`, `userId`, `listingId`, `orderId`, `categoryId`) auto-populate from login/register responses via test scripts.

---

## Troubleshooting

### Backend won't start — "Cannot open database"
- Verify SQL Server is running and the connection string is correct
- Docker: wait for `sqlserver` container to be healthy before starting `backend`
- Check `logs/log-<date>.txt` for detailed error

### Frontend shows blank page / 404 on refresh
- If running behind Nginx: ensure `nginx.conf` has the SPA fallback (`try_files $uri /index.html`)
- Local dev: `npm run dev` serves correctly; issue only occurs with static deployments

### API returns 401 on all requests
- Token may be expired — call `POST /api/v1/auth/refresh` with your refresh token
- Verify `VITE_API_BASE_URL` points to the correct backend URL

### Swagger not loading
- Navigate to `http://localhost:5000/swagger` (not `/swagger/index.html`)
- Ensure backend is running and `ASPNETCORE_ENVIRONMENT` is set

### Docker Compose: "port already in use"
- Stop local SQL Server service if running (`services.msc` on Windows)
- `docker-compose down` then `docker-compose up -d`

### CORS errors in browser
- Ensure `CorsSettings__AllowedOrigins` includes your frontend URL exactly (no trailing slash)
- Docker Compose sets this via `FRONTEND_URL` in `.env`

### Email not sending (dev)
- `appsettings.Development.json` uses Mailtrap — check Mailtrap inbox at https://mailtrap.io
- Missing credentials: add `SmtpSettings__Username` and `SmtpSettings__Password` env vars
