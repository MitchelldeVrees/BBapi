# DMA Order Intake

This workspace contains:

- `backend/` — .NET 10 solution, layered as `Domain → Application → Infrastructure/Contracts → Api`.
  See [docs/architecture.md](docs/architecture.md).
- `frontend/` — Angular workspace: the `dma-order-intake` library (real functionality lives here)
  and the `dma-order-intake-demo` app (a thin shell to run the library standalone).
- `tests/` — test projects, mirroring the backend layers.
- `docs/` — architecture notes.
- `docker-compose.yml` — runs the whole stack.

## Status

This step only proves the architecture: Angular can reach the API, and the API can reach SQLite.
No order processing, Bloomberg, IdentityServer, or OpenFIGI integration yet.

## Run everything with Docker

```bash
docker compose up --build
```

- Frontend: http://localhost:4200
- API: http://localhost:5158/api/orders

The API applies its EF Core migrations on startup and stores `orderintake.db` on a named Docker
volume (`api-data`), so data survives container restarts.

## Run locally without Docker

### Backend

```bash
cd backend/Dma.OrderIntake.Api
dotnet run
```

Runs on `http://localhost:5158` (see `Properties/launchSettings.json`).

### Frontend

```bash
cd frontend
npm install
npm start
```

Builds the `dma-order-intake` library, then serves `dma-order-intake-demo` on
`http://localhost:4200`, pointed at `http://localhost:5158`.

## Prerequisites

- .NET SDK 10
- Node.js 22.x and npm
- Docker + Docker Compose (for `docker compose up --build`)
