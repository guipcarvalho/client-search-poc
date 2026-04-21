# client-search-poc

A small client-management proof-of-concept.

| Layer            | Stack                                                                            |
| ---------------- | -------------------------------------------------------------------------------- |
| API (`api/`)     | .NET 10 minimal API, Dapper + Npgsql, MassTransit (RabbitMQ), Elastic.Clients.Elasticsearch, FluentValidation, Serilog, Scalar OpenAPI |
| Web (`web/`)     | React 19 + TypeScript + Vite + MUI + Axios                                       |
| Infra (root)     | Docker Compose — Postgres 17, Elasticsearch 8, Kibana 8, RabbitMQ 4              |

## Architecture at a glance

```
Web (Vite/MUI)  ──►  .NET 10 API  ──┬──►  Postgres  (Dapper, source of truth)
                                    ├──►  RabbitMQ  (MassTransit publish)
                                    │         │
                                    │         └──►  In-process consumers
                                    │                  │
                                    └──►  Elasticsearch◄┘ (indexed on events)
Kibana ─────────────────────────────►  Elasticsearch
```

When a client is created / updated / deleted, the API writes to Postgres and publishes a `ClientCreated` / `ClientUpdated` / `ClientDeleted` event to RabbitMQ via MassTransit. The same API hosts consumers that listen for those events and keep the Elasticsearch index in sync. The `GET /api/clients/search?q=…` endpoint reads from Elasticsearch; everything else reads from Postgres.

## Prerequisites

- Docker & Docker Compose (everything runs in containers)
- *(Only for local dev)* .NET 10 SDK and Node 20+

## Quick start — everything in Docker

```bash
cp .env.example .env            # optional — override default creds
docker compose up -d --build
```

Services:

| Service       | URL                                       |
| ------------- | ----------------------------------------- |
| Web app       | http://localhost:5173                     |
| API           | http://localhost:5078 (Scalar at `/scalar/v1`) |
| Postgres      | `localhost:5432` (user/pass: `postgres`)  |
| Elasticsearch | http://localhost:9200                     |
| Kibana        | http://localhost:5601                     |
| RabbitMQ AMQP | `localhost:5672`                          |
| RabbitMQ UI   | http://localhost:15672 (`guest/guest`)    |

To rebuild just one service after code changes:

```bash
docker compose up -d --build api   # or web
```

The API baked into the image points at the in-network hostnames (`postgres`, `elasticsearch`, `rabbitmq`). The web image bakes `VITE_API_BASE_URL` at build time; override it with `VITE_API_BASE_URL=… docker compose build web` if you deploy behind a different hostname.

## Local development (without rebuilding images)

Start only the infra services and run the API/web on the host:

```bash
docker compose up -d postgres elasticsearch kibana rabbitmq

# API
cd api && dotnet run --project ClientSearch.Api
# → http://localhost:5078  (Scalar: /scalar/v1, health: /health)

# Web (in another terminal)
cd web
cp .env.example .env
npm install
npm run dev
# → http://localhost:5173
```

On startup the API ensures the `clients` table exists in Postgres and the `clients` index exists in Elasticsearch.

## Project layout

```
client-search-poc/
├── docker-compose.yml         # Postgres, ES, Kibana, RabbitMQ
├── api/
│   ├── ClientSearch.slnx
│   └── ClientSearch.Api/
│       ├── Program.cs         # composition root + Serilog + Scalar
│       ├── Domain/            # Client entity
│       ├── Features/Clients/  # endpoints + validators
│       └── Infrastructure/
│           ├── Database/      # Dapper repo, schema init
│           ├── Elasticsearch/ # index/search service
│           └── Messaging/     # MassTransit events + consumers
└── web/
    └── src/
        ├── api/               # axios client + typed client API
        ├── components/        # ClientFormDialog
        ├── pages/             # ClientsPage
        └── theme/             # MUI theme
```

## Endpoints

| Method | Path                         | Source          |
| ------ | ---------------------------- | --------------- |
| GET    | `/api/clients`               | Postgres        |
| GET    | `/api/clients/{id}`          | Postgres        |
| GET    | `/api/clients/search?q=…`    | Elasticsearch   |
| POST   | `/api/clients`               | Postgres + publish `ClientCreated` |
| PUT    | `/api/clients/{id}`          | Postgres + publish `ClientUpdated` |
| DELETE | `/api/clients/{id}`          | Postgres + publish `ClientDeleted` |
| GET    | `/health`                    | —               |
