# Setup Guide

Three ways to run: Local, Docker, Azure. Pick one.

---

## Option 1: Local Development

### Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| Node.js | 20+ | https://nodejs.org |
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| SQL Server | 2022 or Express | https://www.microsoft.com/sql-server |
| EF Core CLI | 8.x | `dotnet tool install -g dotnet-ef` |

### 1. Database

**Azure SQL** (default in `appsettings.json` — already configured):
- No setup needed. App auto-migrates on startup.

**Local SQL Server Express** (swap connection string):
```json
// appsettings.json → ConnectionStrings:DefaultConnection
"Data Source=.\\SQLEXPRESS;Initial Catalog=shopease;Integrated Security=True;TrustServerCertificate=True;"
```
Create the database first:
```sql
CREATE DATABASE [shopease];
```
Apply migrations manually if not using auto-migrate:
```bash
cd Backend
dotnet ef database update \
  --project src/EBayClone.Infrastructure \
  --startup-project src/EBayClone.API
```

### 2. Backend

```bash
cd Backend
dotnet restore
dotnet run --project src/EBayClone.API
```

First startup: auto-migrates DB, seeds admin user + 8 categories + sample listings.

| URL | Purpose |
|-----|---------|
| http://localhost:5000 | API root |
| http://localhost:5000/swagger | Swagger UI |
| http://localhost:5000/health | Health check |

Logs: `Backend/logs/log-<date>.txt`

### 3. Frontend

```bash
cd Frontend
cp .env.example .env          # or edit .env.development directly
npm install
npm run dev
```

Frontend runs at **http://localhost:5173**. All `/api/*` requests are proxied to `http://localhost:5000` by Vite — no CORS issues in dev.

**`.env.development` (default values, works out of the box):**
```env
VITE_API_BASE_URL=http://localhost:5000
VITE_APP_NAME=ShopEase
```

### Default Credentials

| Role | Email | Password |
|------|-------|---------|
| Admin | admin@shopease.com | Admin@123 |
| User | user@shopease.com | User@123 |

### Email (Dev)

Dev uses Mailtrap SMTP sandbox — no real emails sent. Credentials in `appsettings.Development.json`:
```json
"SmtpSettings": {
  "Host": "sandbox.smtp.mailtrap.io",
  "Username": "<mailtrap-key>",
  "Password": "<mailtrap-key>"
}
```
Check inbox at https://mailtrap.io after register/forgot-password flows.

If `SmtpSettings:Host` is empty, email is logged to console via Serilog (`[EMAIL-CONSOLE]`).

---

## Option 2: Docker Compose

### Prerequisites

- Docker Desktop (latest): https://www.docker.com/products/docker-desktop

### Steps

```bash
# 1. Clone and enter repo
git clone <repo-url>
cd shopease-one

# 2. Create env file
cp .env.example .env
# Edit .env — change at minimum DB_PASSWORD and JWT_SECRET

# 3. Start all services
docker-compose up -d

# 4. Watch backend logs (auto-migrates + seeds on first run)
docker-compose logs -f backend
```

Wait for `backend` to log `Application started` before hitting the frontend.

### Services

| Container | Port | URL |
|-----------|------|-----|
| `shopease-sqlserver` | 1433 | — |
| `shopease-backend` | 5000 | http://localhost:5000/swagger |
| `shopease-frontend` | 80 | http://localhost |

### `.env` Reference

```env
# ── Database ────────────────────────────────────────────────
DB_NAME=ShopEaseDb
DB_USER=sa
DB_PASSWORD=YourStrong@Passw0rd         # change this

# ── JWT ─────────────────────────────────────────────────────
JWT_SECRET=replace-with-a-secret-at-least-32-characters-long  # change this
JWT_ISSUER=ShopEaseApi
JWT_AUDIENCE=ShopEaseFrontend
JWT_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# ── URLs ─────────────────────────────────────────────────────
FRONTEND_URL=http://localhost:5173
BACKEND_URL=http://localhost:5000

# ── Runtime ──────────────────────────────────────────────────
ASPNETCORE_ENVIRONMENT=Development
```

### Teardown

```bash
docker-compose down           # stop containers, keep DB volume
docker-compose down -v        # stop containers + delete DB volume (full reset)
```

### Known Issues

- **Port 1433 conflict**: Stop local SQL Server service first (`services.msc` → SQL Server → Stop)
- **Backend exits immediately**: SQL Server healthcheck needs ~30 s — Docker Compose `depends_on: condition: service_healthy` handles this, but low-spec machines may need `start_period` increase in `docker-compose.yml`

---

## Option 3: Azure Deployment

### Prerequisites

- Azure subscription
- Azure SQL Database (already configured: `shopease.database.windows.net`, DB `shopease-one`)
- Azure App Service (or Container Apps) for backend
- Azure Static Web Apps or Blob + CDN for frontend

### Backend — Azure App Service

1. **Connection string** — set in App Service → Configuration → Connection strings:
   ```
   Name: DefaultConnection
   Value: Data Source=shopease.database.windows.net;Initial Catalog=shopease-one;User ID=<user>;Password=<pass>;Encrypt=True;TrustServerCertificate=False;
   Type: SQLServer
   ```

2. **App Settings** — set in App Service → Configuration → Application settings:
   ```
   ASPNETCORE_ENVIRONMENT         = Production
   JwtSettings__Secret            = <min-32-char-secret>
   JwtSettings__Issuer            = ShopEaseApi
   JwtSettings__Audience          = ShopEaseFrontend
   CorsSettings__AllowedOrigins   = https://your-frontend-domain.com
   SmtpSettings__Host             = smtp.sendgrid.net
   SmtpSettings__Username         = apikey
   SmtpSettings__Password         = <sendgrid-api-key>
   SmtpSettings__FromEmail        = noreply@yourdomain.com
   SmtpSettings__FromName         = ShopEase
   ```

3. **Deploy** — publish from Visual Studio, GitHub Actions, or `az webapp deploy`.

4. **Migrations** — auto-applied on startup. No manual `dotnet ef database update` needed.

5. **File uploads** — currently stored at `wwwroot/uploads` (local disk). Azure App Service restarts clear this. For production: replace `LocalFileStorageService` with Azure Blob Storage (implements same `IFileStorageService` interface — no other code changes needed).

### Frontend — Azure Static Web Apps

```bash
cd Frontend
npm install
npm run build   # outputs to dist/
```

Deploy `dist/` to Azure Static Web Apps or upload to Azure Blob Storage with static website enabled.

**Build-time env var (required):**
```env
VITE_API_BASE_URL=https://api.your-backend-domain.com
```

Configure SPA fallback: Static Web Apps handles this automatically. For Blob + CDN, configure error document to `index.html`.

**API routing**: In production the frontend calls `VITE_API_BASE_URL` directly (no Vite proxy). Ensure backend CORS allows the frontend origin.

### SMTP — SendGrid (Recommended for Production)

```
Host:     smtp.sendgrid.net
Port:     587
Username: apikey
Password: <SendGrid API key>
```

SendGrid free tier: 100 emails/day. Enable sender authentication for your domain.

### Firewall

Allow Azure App Service outbound IPs in Azure SQL firewall rules, or enable "Allow Azure services" toggle in Azure SQL → Networking.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| "Cannot open database" | Verify connection string; check SQL Server is running; check firewall rules |
| Backend starts but 500 on all requests | Check `logs/log-<date>.txt`; missing `JwtSettings:Secret` causes crash |
| 401 on all requests | Token expired — call `POST /api/v1/auth/refresh`; verify `VITE_API_BASE_URL` |
| Email not sending | Check Mailtrap inbox (dev) or SMTP credentials; `Host` empty → logged to console |
| Docker "port already in use" | Stop local SQL Server service; `docker-compose down && docker-compose up -d` |
| Frontend blank page on refresh | Nginx SPA fallback must be `try_files $uri /index.html` |
| CORS error in browser | Add frontend origin exactly (no trailing slash) to `CorsSettings:AllowedOrigins` |
| File uploads lost on restart (Azure) | Use Azure Blob Storage instead of `LocalFileStorageService` |
