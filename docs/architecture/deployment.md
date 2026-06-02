# Deployment Guide

## Production Deployment via Docker Compose

### 1. Prepare server

```bash
# Install Docker
curl -fsSL https://get.docker.com | sh
# Install Docker Compose
sudo apt install docker-compose-plugin
```

### 2. Clone repository

```bash
git clone <repo-url> /opt/instant_bot
cd /opt/instant_bot
```

### 3. Configure environment

```bash
cp .env.example .env
nano .env
```

Required values:
```dotenv
TELEGRAM_BOT_TOKEN=<from @BotFather>
OPENAI_API_KEY=<from platform.openai.com>
POSTGRES_USER=instantbot
POSTGRES_PASSWORD=<strong password>
POSTGRES_DB=instantbot
ADMIN_JWT_SECRET=<min 32 random characters>
ADMIN_USERNAME=<your admin login>
ADMIN_PASSWORD=<strong password>
```

### 4. Start

```bash
docker compose up -d --build
```

### 5. Verify

```bash
docker compose ps          # all containers should be "healthy" or "running"
docker compose logs bot    # should see "Bot started polling"
curl http://localhost:8080/health  # {"status":"healthy",...}
```

## Nginx Reverse Proxy (AdminPanel)

```nginx
server {
    listen 80;
    server_name admin.yourdomain.com;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

For HTTPS:
```bash
certbot --nginx -d admin.yourdomain.com
```

## Data Backups

### PostgreSQL backup

```bash
docker compose exec postgres pg_dump -U instantbot instantbot > backup_$(date +%Y%m%d).sql
```

### PostgreSQL restore

```bash
docker compose exec -T postgres psql -U instantbot instantbot < backup_20260601.sql
```

### Automated daily backup (cron)

```bash
crontab -e
# Add:
0 2 * * * cd /opt/instant_bot && docker compose exec -T postgres pg_dump -U instantbot instantbot > /opt/backups/instant_bot_$(date +\%Y\%m\%d).sql 2>&1
# Keep 30 days
0 3 * * * find /opt/backups -name "instant_bot_*.sql" -mtime +30 -delete
```

## Updates

```bash
cd /opt/instant_bot
git pull
docker compose up -d --build
```

EF migrations are applied automatically on `bot` container start.

## Monitoring

### Check health

```bash
curl http://localhost:8080/health
# {"status":"healthy","timestamp":"2026-06-01T..."}
```

### View logs

```bash
docker compose logs bot -f --tail 100
docker compose logs adminpanel -f --tail 100
```

### Log files (inside containers)

Bot logs are written to `logs/bot-YYYYMMDD.log` via Serilog.

### Redis monitoring

```bash
docker compose exec redis redis-cli monitor  # real-time commands
docker compose exec redis redis-cli info memory
```

## Scaling Considerations

Current architecture: single monolith. When traffic grows:

1. **AI processing bottleneck** → extract `OpenAiService` calls to a separate `InstantBot.AiWorker` worker service with a message queue (RabbitMQ/Azure Service Bus)
2. **Notification volume** → run `DailyCardJob` as standalone container with Redis distributed lock
3. **Read scaling** → add PostgreSQL read replica; route `GetDailyCard` queries to replica
4. **Bot scaling** → switch from Long Polling to Webhook with load balancer

## Environment Variables Reference

| Variable | Required | Description |
|---|---|---|
| `TELEGRAM_BOT_TOKEN` | ✅ | Bot token from @BotFather |
| `OPENAI_API_KEY` | ✅ | OpenAI API key |
| `POSTGRES_USER` | ✅ | PostgreSQL user |
| `POSTGRES_PASSWORD` | ✅ | PostgreSQL password |
| `POSTGRES_DB` | ✅ | Database name |
| `ADMIN_JWT_SECRET` | ✅ | JWT signing secret (min 32 chars) |
| `ADMIN_USERNAME` | ✅ | Admin panel login |
| `ADMIN_PASSWORD` | ✅ | Admin panel password |
