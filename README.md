# DesfudenciFy 2

Personal finance platform rewritten with a Clean Architecture .NET 8 API and Vue 3 frontend.

## Stack

- Backend: ASP.NET Core 8, EF Core, PostgreSQL, JWT, Swagger
- Frontend: Vue 3, TypeScript, Vite, Pinia, Chart.js
- Docker Compose: `postgres`, `api`, `web`

## Features

- Auth with email/password; admin creates users and roles (`Admin` / `User`)
- Admin CRUDs: users, bank accounts, investment types
- Global free balance + reserves (no owners)
- Entries and transfers (free balance or reserve)
- Fixed-income investments with proportional profit liquidation
- Real-estate properties with photo upload, financing and amortizations
- Fixed costs, income sources, installment purchases
- Dashboard with capital history, reserve distribution, upcoming investments and bills

## Quick start (Docker)

```bash
cp .env.example .env   # optional; edit secrets for anything beyond local play
docker compose up --build
```

- Web UI: http://localhost:8080
- API / Swagger: http://localhost:5080/swagger
- Default admin: `admin@desfudencify.local` / `Admin@12345`

Property photos persist in `./data/uploads`.

## Deploy on Raspberry Pi (LAN)

Requirements: Raspberry Pi OS 64-bit (Pi 4/5 recommended), Docker Engine + Compose plugin.

1. Copy the project to the Pi (git clone, `scp`, or USB).
2. On the Pi:

```bash
cp .env.example .env
# Edit .env: strong POSTGRES_PASSWORD, JWT_KEY, SEED_ADMIN_PASSWORD
mkdir -p data/uploads
docker compose up --build -d
```

3. From any device on the same network open `http://<raspberry-ip>:8080`  
   (find the IP with `hostname -I`). Swagger: `http://<raspberry-ip>:5080/swagger`.

The web container proxies `/api` to the API, so the browser only needs the Pi IP and port 8080. First build on the Pi can take 15–40 minutes; later starts are much faster.

To stop: `docker compose down` — data stays in the `postgres_data` volume and `./data/uploads`.

## Local development

### Database

```bash
docker compose up -d postgres
```

### API

```bash
cd backend
dotnet run --project src/DesfudenciFy.Api --urls http://localhost:5080
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Vite proxies `/api` to `http://localhost:5080`.

## Solution layout

```
backend/src/DesfudenciFy.Domain
backend/src/DesfudenciFy.Application
backend/src/DesfudenciFy.Infrastructure
backend/src/DesfudenciFy.Api
frontend/
```

## Notes

- Migrations run automatically on API startup, then seed admin and default investment types.
- Change JWT key and admin password before production use.
