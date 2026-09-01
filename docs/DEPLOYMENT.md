# VPS Deployment

## How it works

GitHub Actions builds the Docker image, pushes it to GitHub Container
Registry (GHCR), then SSHes to your VPS to pull the image and restart
the container. The only file the VPS needs is `compose.vps.yml` —
the CI pipeline copies it automatically on each deploy.

**You do NOT need to clone the repo on the VPS.**

## One-Time VPS Setup

```bash
# 1. Install Docker
curl -fsSL https://get.docker.com | sh

# 2. Create deploy user (or use existing user with docker access)
sudo useradd -m -s /bin/bash deploy
sudo usermod -aG docker deploy

# 3. Create deploy directory
sudo mkdir -p /opt/seasonended
sudo chown deploy:deploy /opt/seasonended

# 4. Authenticate to GHCR (for pulling private images)
sudo -u deploy bash -c 'echo "YOUR_GHCR_PAT" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin'
```

## GitHub Setup

### 1. Create `production` environment

Settings → Environments → New environment → name it `production`.

### 2. Add repository secrets

Settings → Secrets and variables → Actions → New repository secret.

### Required secrets

| Secret | How to get it |
|--------|---------------|
| `VPS_HOST` | `seasonendednotificator.duckdns.org` (or VPS IP) |
| `VPS_USER` | `deploy` (from step 2 above) |
| `VPS_SSH_KEY` | See "Generate SSH key" below |
| `VPS_DEPLOY_PATH` | `/opt/seasonended` (from step 3 above) |
| `GHCR_PAT` | See "Generate GitHub PAT" below |
| `POSTGRES_DB` | Pick `seasonended` |
| `POSTGRES_USER` | Pick `seasonended` |
| `POSTGRES_PASSWORD` | Run `openssl rand -base64 24` locally |
| `BOOTSTRAP_ADMIN_EMAIL` | Your email — initial admin account |

### Optional secrets (leave empty to disable)

| Secret | How to get it |
|--------|---------------|
| `VPS_PORT` | Only if SSH is not on port 22 |
| `TELEGRAM_BOT_TOKEN` | Talk to @BotFather on Telegram → `/newbot` |
| `TELEGRAM_BOT_USERNAME` | Bot username from BotFather |
| `TELEGRAM_WEBHOOK_SECRET` | `openssl rand -hex 32` — see Telegram setup below |
| `PUSH_PUBLIC_KEY` | Run `npx web-push generate-vapid-keys` |
| `PUSH_PRIVATE_KEY` | Same command as above |
| `PUSH_SUBJECT` | `mailto:your@email.com` |

### Generate SSH key

Run on your **local** machine (not VPS):

```bash
# Generate key (no passphrase — CI can't enter one)
ssh-keygen -t ed25519 -f ~/.ssh/seasonended_deploy -C "github-actions-deploy" -N ""

# Copy public key to VPS
ssh-copy-id -i ~/.ssh/seasonended_deploy.pub deploy@YOUR_VPS_IP

# Test it works
ssh -i ~/.ssh/seasonended_deploy deploy@YOUR_VPS_IP "echo ok"

# The SECRET value is the PRIVATE key — copy entire output:
cat ~/.ssh/seasonended_deploy
```

Paste the full output (including `-----BEGIN...` and `-----END...` lines)
as the `VPS_SSH_KEY` secret.

### Generate GitHub PAT

1. Go to https://github.com/settings/tokens
2. Generate new token (classic)
3. Select scope: `read:packages`
4. Copy token → paste as `GHCR_PAT` secret

## Pipeline Flow

Every push to `main`:

1. **test** — .NET tests + Vue tests + Vue build
2. **compose-smoke** — integration test (HTTP smoke tests)
3. **build-and-push** — build Docker image, push to GHCR with `latest` + commit SHA tags
4. **deploy** — SCP `compose.vps.yml` to VPS, SSH in, `docker compose pull`, `docker compose up -d --wait`. Nginx on VPS host terminates TLS + proxies to API on `localhost:8080`.

## Manual Operations

### First deploy (or after setting up new VPS)

Just push to `main`. CI handles everything.

### Check running containers on VPS

```bash
ssh deploy@YOUR_VPS_IP
cd /opt/seasonended
docker compose -f compose.vps.yml ps
docker compose -f compose.vps.yml logs --tail 50 api
```

### Rollback to previous version

```bash
ssh deploy@YOUR_VPS_IP
cd /opt/seasonended
# Find previous image tag (commit SHA) at:
# https://github.com/YOUR_USER/seasonEndedNotificator/pkgs/container/seasonended-api/versions
export GHCR_OWNER=YOUR_GITHUB_USERNAME
export IMAGE_TAG=PREVIOUS_COMMIT_SHA
export POSTGRES_DB=... POSTGRES_USER=... POSTGRES_PASSWORD=...
docker compose -f compose.vps.yml pull
docker compose -f compose.vps.yml up -d --wait
```

## Telegram Bot Setup (for auth + notifications)

### 1. Create bot

Open Telegram, talk to @BotFather:
```
/newbot
```
Follow prompts. Save:
- **Token** → GitHub Secret `TELEGRAM_BOT_TOKEN`
- **Username** → GitHub Secret `TELEGRAM_BOT_USERNAME`

### 2. Generate webhook secret

```bash
openssl rand -hex 32
# → e.g. a1b2c3d4e5f6...
```
Save as GitHub Secret `TELEGRAM_WEBHOOK_SECRET`.

### 3. Register webhook (after first deploy)

After the app is running on your VPS, register the webhook:

```bash
# Replace YOUR_BOT_TOKEN, YOUR_DOMAIN, and YOUR_WEBHOOK_SECRET
curl -s "https://api.telegram.org/botYOUR_BOT_TOKEN/setWebhook" \
  -d "url=https://YOUR_DOMAIN/api/telegram/webhook" \
  -d "secret_token=YOUR_WEBHOOK_SECRET"

# Verify it's set:
curl -s "https://api.telegram.org/botYOUR_BOT_TOKEN/getWebhookInfo" | jq
```

### 4. How Telegram login works

- User goes to login page, enters email
- If user has Telegram connected → bot sends magic link to their Telegram chat
- User clicks link → browser opens → logged in
- If no Telegram connected → falls back to email

User can also message the bot directly:
```
/login their@email.com
```
Bot sends login link to their chat.

### 5. Connect Telegram to your account

After logging in via magic link:
- Go to Notification Settings → "Connect Telegram"
- Opens `https://t.me/BotUsername?start=TOKEN` on phone
- Bot receives `/start TOKEN` → links chat to account
