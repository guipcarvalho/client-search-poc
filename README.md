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

- .NET 10 SDK
- Node 20+ and npm
- Docker & Docker Compose

## 1. Start the infrastructure

```bash
cp .env.example .env            # optional — override default creds
docker compose up -d
```

Services:

| Service       | URL                                   |
| ------------- | ------------------------------------- |
| Postgres      | `localhost:5432` (user/pass: `postgres`) |
| Elasticsearch | http://localhost:9200                 |
| Kibana        | http://localhost:5601                 |
| RabbitMQ AMQP | `localhost:5672`                      |
| RabbitMQ UI   | http://localhost:15672 (`guest/guest`)|

## 2. Run the API

```bash
cd api
dotnet run --project ClientSearch.Api
```

The API listens on http://localhost:5078.

- OpenAPI spec: http://localhost:5078/openapi/v1.json
- Scalar API reference: http://localhost:5078/scalar/v1
- Health: http://localhost:5078/health

On startup it ensures the `clients` table exists in Postgres and the `clients` index exists in Elasticsearch.

## 3. Run the web app

```bash
cd web
cp .env.example .env            # optional — override API base url
npm install                     # first time only
npm run dev
```

The app runs on http://localhost:5173.

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
