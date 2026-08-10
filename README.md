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
docker compose up --build
```

- Web UI: http://localhost:8080
- API / Swagger: http://localhost:5080/swagger
- Default admin: `admin@desfudencify.local` / `Admin@12345`

Property photos persist in `./data/uploads`.

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
