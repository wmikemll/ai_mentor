# InstantBot — AI Instructions

## Project Identity

Telegram SaaS-бот "Личный ИИ-наставник". C# .NET 9, Clean Architecture, CQRS, PostgreSQL, Redis, OpenAI GPT-4o-mini. Монетизация через Telegram Stars.

**Build:** `dotnet build InstantBot.sln` → 0 errors, 0 warnings  
**Tests:** `dotnet test InstantBot.sln` → 22/22 pass  
**Run:** `docker compose up --build` (нужен `.env` с токенами)

## Critical Rules

1. **Никогда не менять** уровни зависимостей: Domain ← Application ← Infrastructure ← TelegramBot/AdminPanel. Domain не должен зависеть ни от чего кроме .NET BCL.
2. **Всегда запускать** `dotnet build InstantBot.sln` и `dotnet test InstantBot.sln` после изменений перед коммитом.
3. **EF Core версии**: везде 9.0.5. AdminPanel имеет явный pin `Microsoft.EntityFrameworkCore.Design 9.0.5` — не удалять.
4. **Никогда** не добавлять бизнес-логику в Infrastructure — только реализация интерфейсов Application.
5. **Секреты** только в `.env` (git-ignored). `appsettings.json` — только dev-дефолты без реальных ключей.

## Architecture Map

```
Domain/Entities/         User, PersonalityProfile, Conversation, Message,
                         DailyCard, Subscription, RelationshipAnalysis,
                         Referral, Achievement
Domain/Enums/            SubscriptionTier, SubscriptionPlan, MessageRole,
                         ArchetypeType, UserBotState, AchievementType
Domain/ValueObjects/     TarotCard

Application/Commands/    RegisterUser, AskMentor, PurchaseSubscription,
                         AnalyzeRelationship, ProcessReferral
Application/Queries/     GetDailyCard
Application/Interfaces/  IUserRepository, IConversationRepository,
                         IDailyCardRepository, ICacheService, IOpenAiService,
                         IWhisperService, ITtsService, INotificationService
Application/Services/    NumerologyCalculator, ArchetypeAnalyzer,
                         PersonalityProfileBuilder, StreakService

Infrastructure/Persistence/   AppDbContext, Configurations/, Repositories/,
                               Migrations/
Infrastructure/OpenAi/         OpenAiService, WhisperService, TtsService
Infrastructure/Cache/          RedisCacheService
Infrastructure/Payments/       TelegramStarsService (static helper)
Infrastructure/Notifications/  DailyCardJob, SubscriptionExpiryJob,
                               InactivityJob, ConversationSummaryJob,
                               NotificationScheduler (BackgroundService),
                               TelegramNotificationService

TelegramBot/Handlers/    UpdateRouter, MessageHandler, CallbackHandler,
                         PaymentHandler, VoiceHandler
TelegramBot/StateMachine/ UserStateManager (Redis-backed FSM)
TelegramBot/Keyboards/   MainMenuKeyboard, SubscriptionKeyboard, ShareKeyboard
TelegramBot/             BotPollingService (BackgroundService), Program.cs

AdminPanel/Controllers/  AuthController (JWT cookie), DashboardController,
                         UsersController
AdminPanel/Views/        Dashboard/Index.cshtml (Charts.js), Users/Index.cshtml
AdminPanel/Models/       DashboardViewModel, DailyStats
```

## Key Patterns

### Adding a new Command (CQRS)
1. `src/InstantBot.Application/Commands/MyFeature/MyFeatureCommand.cs` — record implements `IRequest<T>`
2. `src/InstantBot.Application/Commands/MyFeature/MyFeatureCommandHandler.cs` — implements `IRequestHandler<MyFeatureCommand, T>`
3. MediatR регистрирует автоматически через `RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly)`
4. Вызов: `await mediator.Send(new MyFeatureCommand(...), ct)`

### Adding a new Bot State (FSM)
1. Добавить значение в `Domain/Enums/UserBotState.cs`
2. Обработать в `TelegramBot/Handlers/MessageHandler.cs` в switch-выражении
3. Установить состояние: `await stateManager.SetStateAsync(uid, UserBotState.NewState)`

### Adding a new Background Job
1. Создать `Infrastructure/Notifications/MyJob.cs` с методом `RunAsync(CancellationToken ct)`
2. Зарегистрировать: `builder.Services.AddScoped<MyJob>()` в `TelegramBot/Program.cs`
3. Добавить вызов в `NotificationScheduler.ExecuteAsync()` по нужному расписанию

### Adding a new Entity
1. Создать `Domain/Entities/MyEntity.cs` с private constructor + static `Create()` factory
2. Добавить `DbSet<MyEntity>` в `AppDbContext`
3. Создать `Infrastructure/Persistence/Configurations/MyEntityConfiguration.cs`
4. Добавить EF-миграцию: `dotnet ef migrations add AddMyEntity --project src/InstantBot.Infrastructure --startup-project src/InstantBot.TelegramBot --output-dir Persistence/Migrations`

## Redis Key Schema

| Ключ | TTL | Назначение |
|---|---|---|
| `state:{userId}` | 30 мин | FSM состояние пользователя |
| `session:{userId}:{key}` | 30 мин | Временные данные FSM (имя, дата партнёра) |
| `rate:{userId}` | до полуночи UTC | Счётчик вопросов в день (Free tier) |
| `offer:{userId}:{type}` | 24ч-7д | Флаг показанного оффера |
| `card:{userId}:{date}` | 24 ч | Кеш карты дня (JSON) |
| `admin:dashboard` | 1 ч | Кеш метрик AdminPanel |

## Database Schema (key indexes)

```sql
CREATE UNIQUE INDEX idx_users_telegram_id ON users(telegram_user_id);
CREATE UNIQUE INDEX idx_users_referral_code ON users(referral_code);
CREATE INDEX idx_conversations_user_last ON conversations(user_id, last_message_at);
CREATE INDEX idx_messages_conv_date ON messages(conversation_id, created_at);
CREATE INDEX idx_subscriptions_expires ON subscriptions(expires_at);
CREATE UNIQUE INDEX idx_daily_cards_user_date ON daily_cards(user_id, date);
CREATE UNIQUE INDEX idx_achievements_user_type ON achievements(user_id, type);
```

## Subscription Plans & Prices

```csharp
// src/InstantBot.Infrastructure/Payments/TelegramStarsService.cs
(Premium, OneMonth)     → 299 Stars
(Premium, ThreeMonths)  → 749 Stars
(Premium, TwelveMonths) → 2490 Stars
(Vip, OneMonth)         → 599 Stars
(Vip, ThreeMonths)      → 1499 Stars
(Vip, TwelveMonths)     → 4990 Stars
```

## Token Budgets (GPT-4o-mini)

| Tier | Max tokens/response | History messages |
|---|---|---|
| Free | 300 | 5 |
| Premium | 600 | 10 + summary |
| VIP | 1000 | 20 + summary |

## Telegram Payment Flow

```
User clicks subscription button
→ CallbackHandler → TelegramStarsService.SendInvoiceAsync()
→ bot.SendInvoice(currency: "XTR", payload: "{tier}:{plan}:{userId}")
→ Telegram shows Stars payment screen
→ PreCheckoutQuery → PaymentHandler.HandlePreCheckoutAsync() → AnswerPreCheckoutQuery(ok: true)
→ SuccessfulPayment → PaymentHandler.HandleSuccessfulPaymentAsync()
→ PurchaseSubscriptionCommand → user.UpdateSubscription(tier, expiresAt)
```

## Voice Flow (VIP only)

```
User sends voice OGG
→ VoiceHandler.HandleAsync()
→ bot.DownloadFile() → MemoryStream
→ WhisperService.TranscribeAsync() → text
→ AskMentorCommand → GPT-4o-mini → response text
→ TtsService.SynthesizeAsync() → MP3 bytes
→ bot.SendVoice() → user
```

## Numerology Calculation

```csharp
// src/InstantBot.Application/Services/NumerologyCalculator.cs
LifePathNumber = DigitalRoot(day + month + year)  // Master numbers 11, 22 preserved
DestinyNumber  = DigitalRoot(sum of Pythagorean values of all letters in name)
SoulNumber     = DigitalRoot(sum of vowels only)
```

## Things NOT to Do

- ❌ Не добавлять ASP.NET Core зависимости в `InstantBot.Infrastructure` (только `Serilog` base)
- ❌ Не вызывать `DbContext` напрямую из TelegramBot handlers — только через MediatR команды
- ❌ Не хранить секреты в `appsettings.json` — только в `.env`
- ❌ Не создавать миграции без `--project` и `--startup-project` флагов
- ❌ Не менять `TelegramBot.csproj` EF пакет с `9.0.5` — нужен для `dotnet ef`
- ❌ Не убирать явный `Microsoft.EntityFrameworkCore.Design 9.0.5` из AdminPanel.csproj

## Running Migrations

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add <MigrationName> \
  --project src/InstantBot.Infrastructure \
  --startup-project src/InstantBot.TelegramBot \
  --output-dir Persistence/Migrations \
  --context AppDbContext
```

## Test Conventions

- Domain tests: `tests/InstantBot.Domain.Tests/` — unit tests, no mocks, pure C#
- Application tests: `tests/InstantBot.Application.Tests/` — mock repositories/services with Moq
- TDD: write failing test → run (fail) → implement → run (pass)
- Never mock `Domain` entities — use real instances via `Create()` factories
