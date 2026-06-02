# InstantBot — "Личный ИИ-наставник" — Design Spec

**Date:** 2026-06-01  
**Status:** Approved  
**Stack:** C# .NET 9, Telegram.Bot, PostgreSQL, EF Core, Redis, OpenAI API, Docker

---

## 1. Product Overview

Telegram SaaS-продукт "Личный ИИ-наставник" — персональный наставник 24/7, помогающий пользователям разбирать жизненные ситуации: отношения, карьера, деньги, личностный рост, тревога перед решениями.

**Целевая аудитория:** 20–45 лет.  
**Основная ценность:** ощущение персонального наставника, который всегда доступен, знает твой профиль и историю.  
**Монетизация:** Telegram Stars, подписки Free / Premium / VIP.

---

## 2. Solution Structure

```
InstantBot.sln
├── src/
│   ├── InstantBot.Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   └── Events/
│   ├── InstantBot.Application/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── InstantBot.Infrastructure/
│   │   ├── Persistence/
│   │   ├── OpenAi/
│   │   ├── Cache/
│   │   ├── Payments/
│   │   └── Notifications/
│   ├── InstantBot.TelegramBot/
│   │   ├── Handlers/
│   │   ├── StateMachine/
│   │   ├── Keyboards/
│   │   └── Program.cs
│   └── InstantBot.AdminPanel/
│       ├── Controllers/
│       ├── Views/
│       └── Program.cs
└── tests/
    ├── InstantBot.Domain.Tests/
    ├── InstantBot.Application.Tests/
    └── InstantBot.Integration.Tests/
```

**Pattern:** Clean Architecture + CQRS (MediatR) + Domain Events.

---

## 3. Milestones

| # | Milestone | Содержание |
|---|---|---|
| M1 | Core Foundation | Domain entities, EF migrations, User registration FSM, профиль личности, нумерология |
| M2 | AI Mentor | OpenAI GPT-4o-mini, история диалогов, контекстные ответы, rate limiting (Redis) |
| M3 | Monetization | Telegram Stars, подписки Free/Premium/VIP, лимиты, триал, офферы |
| M4 | Engagement | Карта дня, прогноз недели/месяца, ежедневные уведомления, streaks, ачивки |
| M5 | Relationships & Voice | Анализ совместимости, Whisper (input), OpenAI TTS (output, VIP only) |
| M6 | Growth | Реферальная система, шаринг результатов, персональные офферы по триггерам |
| M7 | Admin Panel | Web UI: DAU/MAU/MRR/LTV/Churn, управление пользователями и подписками |

---

## 4. Domain Model

### User
```
Id (Guid)
TelegramUserId (long, unique)
Username, FirstName
BirthDate (DateOnly)
RegistrationDate (DateTime)
SubscriptionTier (Free/Premium/VIP)
SubscriptionExpiresAt (DateTime?)
DailyQuestionsUsed (int)          — сбрасывается в полночь UTC
CurrentStreak (int)
LastActiveDate (DateOnly)
ReferralCode (string, unique)
ReferredByUserId (Guid?)
State (Active/Blocked)
```

### PersonalityProfile
```
UserId (FK)
LifePathNumber (int)
DestinyNumber (int)
SoulNumber (int)
ArchetypeType (Enum: 12 архетипов)
Strengths (string[])
Weaknesses (string[])
Recommendations (string[])
GeneratedAt (DateTime)
```

### Conversation + Message
```
Conversation: Id, UserId, CreatedAt, Summary (text)
Message: Id, ConversationId, Role (User/Assistant), Content, IsVoice, TokensUsed, CreatedAt
```

### RelationshipAnalysis
```
Id, UserId, PartnerName, PartnerBirthDate
Strengths, ConflictZones, CommunicationStyle, Recommendations (text)
CreatedAt
```

### DailyCard
```
UserId, Date (DateOnly)
TarotCard (ValueObject: Name, Arcana, Meaning)
DayAdvice (text), DayFocus (text)
IsViewed (bool)
```

### Subscription
```
Id, UserId
Plan (1mo/3mo/12mo)
Tier (Premium/VIP)
StarsPaid (int)
PurchasedAt, ExpiresAt
IsAutoRenew (bool)
TelegramPaymentChargeId (string)
```

### Referral
```
Id, ReferrerId (FK), ReferredId (FK)
BonusGranted (bool), CreatedAt
```

### Achievement
```
UserId, Type (Enum), UnlockedAt
Types: FirstQuestion, Streak7, Streak30, ReferFriend, PremiumUser, ...
```

### PostgreSQL Indexes
```sql
CREATE UNIQUE INDEX idx_users_telegram_id ON users(telegram_user_id);
CREATE INDEX idx_messages_conversation_date ON messages(conversation_id, created_at DESC);
CREATE INDEX idx_subscriptions_expires ON subscriptions(expires_at) WHERE is_auto_renew = true;
```

### Redis Keys
```
rate:questions:{userId}       TTL: до полуночи UTC    — счётчик вопросов/день
session:state:{userId}        TTL: 30 min             — FSM состояние
daily:card:{userId}:{date}    TTL: 24h                — карта дня
streak:last:{userId}                                  — дата последнего визита
offer:shown:{userId}:{type}   TTL: 72h                — показан ли оффер
metrics:dashboard             TTL: 1h                 — кеш тяжёлых агрегаций
```

---

## 5. AI Integration

### OpenAI GPT-4o-mini — Контекстные ответы

Каждый запрос строится из трёх слоёв:

**System Prompt:**
- Роль: опытный наставник, эмпатичный
- Тон: тёплый, персональный, конкретный
- Ограничения: только "возможно", "один из вариантов" — никаких гарантий будущего

**User Context Injection (в system prompt):**
- Имя, возраст
- Нумерологический профиль (LifePath, архетип, сильные/слабые стороны)
- `Conversation.Summary` (сводка старых диалогов)
- Последние N сообщений (зависит от тарифа)

**Summarization стратегия:**
- Диалог > 20 сообщений → фоновый job сжимает старые в 3–5 предложений
- Сводка хранится в `Conversation.Summary`

### Token Budget по тарифам

| Тариф | Max tokens/ответ | История |
|---|---|---|
| Free | 300 | 5 последних сообщений |
| Premium | 600 | 10 последних + summary |
| VIP | 1000 | 20 последних + summary + голос |

### Voice Flow (VIP only)
```
Голосовое сообщение OGG
  → Whisper API → транскрипт (text)
  → GPT-4o-mini → текстовый ответ
  → OpenAI TTS (voice: "alloy") → MP3
  → Telegram sendVoice()
```

### Нумерология (детерминированная, без AI)
```
LifePathNumber  = цифровой корень(день + месяц + год)
DestinyNumber   = цифровой корень(сумма букв имени по таблице Пифагора)
SoulNumber      = цифровой корень(только гласные имени)
Архетип         = маппинг LifePathNumber → 1 из 12 архетипов
```

---

## 6. Telegram Bot

### Long Polling
`BackgroundService` с `ITelegramBotClient.StartReceiving()`.  
Переключение на Webhook при необходимости через конфиг.

### FSM состояний
```
Start → AwaitingName → AwaitingBirthDate → ProfileReady → MainMenu
                                                          ├── AskMentor → AwaitingQuestion
                                                          ├── RelationshipAnalysis → AwaitingPartnerName
                                                          │                          → AwaitingPartnerBirthDate
                                                          └── DailyCard
```
Состояние в Redis, TTL 30 минут, сброс командой `/menu`.

### Handlers
- `MessageHandler` — текстовые сообщения, роутинг по FSM состоянию
- `CallbackHandler` — InlineKeyboard нажатия (оплата, подписки, шаринг)
- `VoiceHandler` — голосовые сообщения (Whisper → GPT → TTS), только VIP
- `PaymentHandler` — `PreCheckoutQuery` + `SuccessfulPayment`

---

## 7. Монетизация

### Тарифная матрица

| Функция | Free | Premium | VIP |
|---|---|---|---|
| ИИ-наставник | 3 вопроса/день | Безлимит | Безлимит |
| Голосовые ответы | ✗ | ✗ | ✓ |
| Карта дня | ✓ | ✓ | ✓ |
| Прогноз недели | ✗ | ✓ | ✓ |
| Прогноз месяца | ✗ | ✗ | ✓ |
| Анализ отношений | ✗ | ✓ | ✓ |
| История диалогов | 7 дней | 90 дней | Вечная |
| Расширенная аналитика | ✗ | ✗ | ✓ |

### Цены (Telegram Stars)
```
Premium: 299 ⭐/мес · 749 ⭐/3мес · 2490 ⭐/год
VIP:     599 ⭐/мес · 1499 ⭐/3мес · 4990 ⭐/год
```

### Telegram Stars Payment Flow
```
sendInvoice(payload: {userId, plan, tier})
  → PreCheckoutQuery → AnswerPreCheckoutQuery(ok: true)
  → SuccessfulPayment → CreateSubscriptionCommand
      - Subscription в БД
      - User.SubscriptionTier обновлён
      - Redis rate limit инвалидирован
      - Приветственное сообщение
```

### Триггерные офферы
```
Исчерпал 3 вопроса/день    → скидка 20% Premium, TTL 24ч
Закончил анализ отношений  → "Хотите углублённый разбор?" → Premium
3 дня неактивности         → "Скучаем" + скидка 15%
7 дней неактивности        → скидка 25% + бонус "Карта недели"
Реферал зарегистрировался  → +1 день Premium рефереру
```

### Реферальная система
```
ReferralCode = base62(userId)[0..6]
Ссылка: t.me/bot?start=REF_{code}

При регистрации по реферальной ссылке:
  → Реферер: +7 дней Premium + Achievement "FirstReferral"
  → Новый пользователь: расширенный триал 5 дней (вместо 3)
```

### AutoRenew (эмуляция)
- За 3 дня до истечения → напоминание с кнопкой "Продлить"
- В день истечения → оффер со скидкой 10%
- `BackgroundService` проверяет каждые 6 часов

---

## 8. Background Services

| Job | Расписание | Действие |
|---|---|---|
| DailyCardJob | 08:00 UTC | Генерирует карту дня батчами по 50, кешируем в Redis, push уведомления |
| SubscriptionExpiryJob | Каждые 6ч | Напоминания об истечении, офферы |
| InactivityJob | 10:00 UTC | Офферы пользователям 3/7 дней без активности |
| ConversationSummaryJob | Каждый час | Сжимает диалоги > 20 сообщений |
| ResetDailyLimitsJob | 00:00 UTC | Инвалидирует Redis rate limit ключи (или TTL справится) |

---

## 9. Admin Panel

**Stack:** ASP.NET Core MVC + Razor Pages + Charts.js  
**Auth:** JWT, один admin через `appsettings.json`  
**Port:** 8080

### Страницы
- `/dashboard` — DAU, MAU, Retention D7/D30, Conversion, ARPU, MRR, LTV, Churn Rate (Charts.js, 30 дней)
- `/users` — таблица с фильтрами (тариф, дата регистрации, активность)
- `/subscriptions` — все транзакции, доход по дням
- `/referrals` — топ рефереров, конверсия

### Формулы метрик
```
DAU        = COUNT DISTINCT user_id WHERE last_active = today
MAU        = COUNT DISTINCT user_id WHERE last_active >= -30 days
Conversion = (Premium + VIP users) / Total * 100%
ARPU       = MRR / MAU
MRR        = SUM(stars_paid / plan_months) за месяц
LTV        = ARPU / Churn Rate
Churn Rate = отписки за месяц / подписки на начало месяца * 100%
```

Тяжёлые агрегации кешируются в Redis с TTL 1 час.

---

## 10. Infrastructure

### Docker Compose
```yaml
services:
  bot          # InstantBot.TelegramBot
  adminpanel   # InstantBot.AdminPanel → :8080
  postgres     # PostgreSQL 16
  redis        # Redis 7 Alpine
  migrations   # EF migrations (init container)
```

### Environment (.env)
```
TELEGRAM_BOT_TOKEN=
OPENAI_API_KEY=
POSTGRES_CONNECTION_STRING=
REDIS_CONNECTION_STRING=
ADMIN_JWT_SECRET=
ADMIN_USERNAME=
ADMIN_PASSWORD=
```

### Logging
Serilog → Console (structured JSON) + File sink.  
Логируем каждый OpenAI запрос (userId, tokens_used, response_time_ms) и каждую оплату.

### Health Checks
`GET /health` → PostgreSQL + Redis + Telegram bot connected.

---

## 11. Non-Functional Requirements

- **Disclaimer:** все ответы используют формулировки "возможно", "один из вариантов", "с точки зрения данной системы" — никаких гарантий будущего
- **Security:** секреты только через env, JWT для админки, валидация Telegram webhook signature
- **Scalability:** архитектура позволяет вынести AI-обработку в отдельный сервис при росте нагрузки
- **Resilience:** Polly retry для OpenAI API (3 попытки с exponential backoff)
