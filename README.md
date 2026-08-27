# Season Ended

PWA that notifies users after a TV season finale finishes airing.

## Prerequisites

- .NET SDK `10.0.111` or compatible .NET 10 patch.
- Node.js 22 and npm 10 or later.
- Docker Desktop or Docker Engine with Compose v2.
- Ports `80`, `443`, `1025`, and `8025` available on loopback for the local Compose stack.

## Local Builds

```bash
dotnet test SeasonEnded.sln
npm --prefix src/SeasonEnded.Web ci
npm --prefix src/SeasonEnded.Web test
npm --prefix src/SeasonEnded.Web run build
```

## Docker Compose

Copy `.env.example` to `.env`, then set a non-default PostgreSQL password.

```bash
docker compose --env-file .env -f deploy/compose.yml up --build --wait
```

Open `https://season-ended.localhost`. Caddy uses its local certificate authority for development, so a browser warning is expected until its root certificate is trusted.

Health endpoints through Caddy:

```text
https://season-ended.localhost/health/live
https://season-ended.localhost/health/ready
```

Stop the stack with:

```bash
docker compose --env-file .env -f deploy/compose.yml down
```

For a public domain, set `APP_DOMAIN` and layer production Caddy configuration. It obtains a publicly trusted ACME certificate instead of using the local CA:

```bash
docker compose --env-file .env \
  -f deploy/compose.yml \
  -f deploy/compose.production.yml \
  up --build --wait
```

## Health Semantics

- `/health/live` reports process liveness and never depends on PostgreSQL.
- `/health/ready` runs a PostgreSQL query and returns `503` when the database is unavailable.

## Services

- `web`: Vue static assets served by nginx on the internal network.
- `api`: ASP.NET Core Minimal API on the internal network.
- `postgres`: PostgreSQL with persistent named volume.
- `caddy`: only public entry point, terminating local HTTPS and routing `/health/*` to API.
- `mailpit`: local SMTP capture with inbox at `http://localhost:8025`.

## Local Email

With Compose running, send a multipart test message:

```bash
curl --insecure \
  --header "Content-Type: application/json" \
  --data '{"recipient":"viewer@example.test"}' \
  https://season-ended.localhost/api/dev/email-test
```

Open `http://localhost:8025` to inspect recipient, subject, headers, HTML, and plain-text bodies. Mailpit captures messages locally and sends nothing to the internet. The endpoint and Mailpit service are absent from production configuration.
