# VPS Deployment

## Prerequisites on VPS

```bash
# Install Docker + Docker Compose
curl -fsSL https://get.docker.com | sh

# Authenticate to GHCR
echo "<GITHUB_PAT>" | docker login ghcr.io -u <github-username> --password-stdin

# Clone repo
git clone https://github.com/<owner>/seasonEndedNotificator.git
cd seasonEndedNotificator

# Create .env from example
cp .env.example .env
# Edit .env with production values

# Create cert directory for nginx TLS
mkdir -p deploy/certs
# Place cert.pem and key.pem (or use Let's Encrypt/Certbot)
```

## GitHub Repository Secrets

Set these under Settings → Secrets and variables → Actions:

| Secret | Description |
|--------|-------------|
| `VPS_HOST` | Server hostname or IP |
| `VPS_USER` | SSH user with docker access |
| `VPS_SSH_KEY` | Private SSH key |
| `VPS_PORT` | SSH port (optional, default 22) |
| `VPS_DEPLOY_PATH` | Absolute path to repo checkout on VPS |
| `GHCR_PAT` | GitHub PAT with `read:packages` |
| `POSTGRES_DB` | Database name |
| `POSTGRES_USER` | Database user |
| `POSTGRES_PASSWORD` | Database password |
| `BOOTSTRAP_ADMIN_EMAIL` | Initial admin email (optional) |
| `TELEGRAM_BOT_TOKEN` | Telegram bot token (optional) |
| `TELEGRAM_BOT_USERNAME` | Telegram bot username (optional) |
| `PUSH_PUBLIC_KEY` | VAPID public key (optional) |
| `PUSH_PRIVATE_KEY` | VAPID private key (optional) |
| `PUSH_SUBJECT` | VAPID subject, e.g. mailto: (optional) |

## Pipeline Flow

1. **Push to `main`** triggers CI
2. `test` job — .NET tests + Vue tests + Vue build
3. `compose-smoke` job — full stack integration test
4. `build-and-push` job — builds API + Web images, pushes to GHCR with `latest` + commit SHA tags
5. `deploy` job — SSHes to VPS, pulls images, restarts containers

## Manual Deployment

```bash
# On VPS:
cd <VPS_DEPLOY_PATH>
export GHCR_OWNER=<github-username>
export IMAGE_TAG=<commit-sha-or-latest>
# ... export other env vars ...
docker compose -f deploy/compose.vps.yml pull
docker compose -f deploy/compose.vps.yml up -d --wait
```

## Rollback

```bash
# Set IMAGE_TAG to previous commit SHA
export IMAGE_TAG=<previous-sha>
docker compose -f deploy/compose.vps.yml pull
docker compose -f deploy/compose.vps.yml up -d --wait
```
