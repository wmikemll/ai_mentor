# Development Guide

## Prerequisites

- .NET 9 SDK (`dotnet --version` → 9.x)
- Docker + Docker Compose
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef --version 9.*`

## Local Development (without Docker)

### 1. Start infrastructure

```bash
docker compose up postgres redis -d
```

### 2. Set environment variables

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=instantbot;Username=instantbot;Password=changeme123"
export Redis__ConnectionString="localhost:6379"
export TelegramBot__Token="your_bot_token"
export OpenAI__ApiKey="sk-..."
```

### 3. Run the bot

```bash
dotnet run --project src/InstantBot.TelegramBot
```

### 4. Run the admin panel

```bash
export Admin__JwtSecret="dev-secret-min-32-chars-change-in-production"
export Admin__Username="admin"
export Admin__Password="admin"
dotnet run --project src/InstantBot.AdminPanel
# → http://localhost:5000
```

## Adding Features

### Feature checklist

1. **Domain**: Add entity or extend existing (if needed)
2. **Application**: Add Command or Query + Handler
3. **Infrastructure**: Implement new interface method (if needed)
4. **TelegramBot**: Add FSM state or callback handler
5. **Tests**: Write failing test first, then implement
6. **Migration**: Add EF migration if schema changed

### Example: Adding weekly forecast

```bash
# 1. Add WeeklyForecast entity to Domain/Entities/
# 2. Add IWeeklyForecastRepository to Application/Interfaces/
# 3. Add GetWeeklyForecastQuery to Application/Queries/
# 4. Add WeeklyForecastRepository to Infrastructure/Persistence/Repositories/
# 5. Add WeeklyForecastConfiguration to Infrastructure/Persistence/Configurations/
# 6. Add WeeklyForecastJob to Infrastructure/Notifications/
# 7. Wire into NotificationScheduler (every Monday 08:00)
# 8. Wire into CallbackHandler ("weekly_forecast" callback, gated Premium+)
# 9. Add migration:
dotnet ef migrations add AddWeeklyForecast \
  --project src/InstantBot.Infrastructure \
  --startup-project src/InstantBot.TelegramBot \
  --output-dir Persistence/Migrations
```

## Testing

### Run all tests

```bash
dotnet test InstantBot.sln
```

### Run specific test class

```bash
dotnet test tests/InstantBot.Domain.Tests/ --filter "NumerologyCalculatorTests"
```

### TDD workflow

```bash
# 1. Write failing test
# 2. Verify it fails
dotnet test --filter "MyNewTest" 2>&1 | grep -E "pass|fail"
# 3. Implement minimal code
# 4. Verify it passes
dotnet test --filter "MyNewTest"
# 5. Commit
```

### Test patterns

**Domain tests** — use entity factories directly:
```csharp
var user = User.Create(123L, "Тест", null, new DateOnly(1990, 1, 1));
user.IncrementDailyQuestions();
user.CanAskQuestion().Should().BeTrue();
```

**Application tests** — mock interfaces with Moq:
```csharp
var repo = new Mock<IUserRepository>();
repo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);
var handler = new MyCommandHandler(repo.Object);
var result = await handler.Handle(new MyCommand(1L), default);
result.Should().NotBeNull();
```

## Database Operations

### Create new migration

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add <Name> \
  --project src/InstantBot.Infrastructure \
  --startup-project src/InstantBot.TelegramBot \
  --output-dir Persistence/Migrations \
  --context AppDbContext
```

### Apply migrations manually

```bash
dotnet ef database update \
  --project src/InstantBot.Infrastructure \
  --startup-project src/InstantBot.TelegramBot
```

### Rollback last migration

```bash
dotnet ef migrations remove \
  --project src/InstantBot.Infrastructure \
  --startup-project src/InstantBot.TelegramBot
```

## Docker Development

### Full rebuild

```bash
docker compose down && docker compose up --build
```

### View logs

```bash
docker compose logs bot -f
docker compose logs adminpanel -f
```

### Connect to database

```bash
docker compose exec postgres psql -U instantbot -d instantbot
```

### Flush Redis

```bash
docker compose exec redis redis-cli FLUSHALL
```

## Common Issues

### "Connection refused" to PostgreSQL/Redis
The bot applies EF migrations on startup. If postgres isn't healthy yet, startup may fail.  
**Fix:** Docker Compose `depends_on: condition: service_healthy` handles this — check healthcheck passes.

### "Invalid token" Telegram
**Fix:** Verify `TELEGRAM_BOT_TOKEN` in `.env`. Get fresh token from @BotFather.

### EF migration fails with version error
**Fix:** Run `export PATH="$PATH:$HOME/.dotnet/tools"` before `dotnet ef` commands.

### OpenAI 401 error
**Fix:** Verify `OPENAI_API_KEY` is valid and has sufficient balance.

### Bot doesn't respond to messages
**Fix:** Check `docker compose logs bot` — look for "Bot started polling". If not present, token is wrong.

## Code Style Conventions

- **Entities**: private constructor + static `Create()` factory method
- **Commands/Queries**: `record` types implementing `IRequest<T>`
- **Handlers**: primary constructor injection (C# 12)
- **Async**: all repository/service methods are `async Task<T>` with `CancellationToken ct = default`
- **Namespaces**: match folder structure (`InstantBot.Domain.Entities`, etc.)
- **No comments** unless explaining non-obvious constraint or workaround
- **No `var` for new object creation** — use explicit type when constructing
