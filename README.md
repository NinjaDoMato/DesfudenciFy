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

## Quick start (Docker — desenvolvimento)

```bash
docker compose -f docker-compose.development.yml up --build -d
```

| Serviço | Porta host |
|---------|------------|
| Web | **8081** |
| API / Swagger | **5081** |
| Postgres | **5434** (`desfudencify_dev`) |

- UI: http://localhost:8081 (tarja amarela “Ambiente de desenvolvimento”)
- Swagger: http://localhost:5081/swagger
- Default admin: `admin@desfudencify.local` / `Admin@12345`

Dev e prod podem rodar ao mesmo tempo (projetos Docker `desfudencify-dev` / `desfudencify-prod`).

Property photos (dev): `./data/uploads-dev`.

### Produção (Raspberry Pi / LAN)

| Serviço | Porta host |
|---------|------------|
| Web | **8080** |
| API | **5080** |
| Postgres | **5433** |

Requirements: Raspberry Pi OS 64-bit (Pi 4/5 recommended), Docker Engine + Compose plugin.

1. Copy the project to the Pi (git clone, `scp`, or USB).
2. On the Pi:

```bash
cp .env.example .env
# Edit .env only if you use the default docker-compose.yml

cp backend/src/DesfudenciFy.Api/appsettings.json.example backend/src/DesfudenciFy.Api/appsettings.json
cp backend/src/DesfudenciFy.Api/appsettings.Production.json.example backend/src/DesfudenciFy.Api/appsettings.Production.json
cp docker-compose.production.yml.example docker-compose.production.yml
# Edit the three files with the same production secrets

mkdir -p data/uploads
docker compose -f docker-compose.production.yml up --build -d
```

3. From any device on the same network open `http://<raspberry-ip>:8080`  
   (find the IP with `hostname -I`). API: `http://<raspberry-ip>:5080`.

The web container proxies `/api` to the API, so the browser only needs the Pi IP and port 8080. First build on the Pi can take 15–40 minutes; later starts are much faster.

To stop: `docker compose -f docker-compose.production.yml down` — data stays in the `postgres_data` volume and `./data/uploads`.

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

Vite proxies `/api` to `http://localhost:5080`. A tarja amarela no topo indica ambiente de desenvolvimento.

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
- Development: `docker compose -f docker-compose.development.yml up --build -d`
- Production: `docker compose -f docker-compose.production.yml up --build -d`
