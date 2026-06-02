# InstantBot — Личный ИИ-наставник

Telegram SaaS-бот на C# .NET 9. Персональный ИИ-наставник 24/7 с нумерологией, анализом отношений, голосовыми ответами и монетизацией через Telegram Stars.

## Быстрый старт

```bash
cp .env.example .env
# Заполни: TELEGRAM_BOT_TOKEN, OPENAI_API_KEY, POSTGRES_PASSWORD, ADMIN_JWT_SECRET
docker compose up --build
```

Бот запускается, AdminPanel доступна на `http://localhost:8080`.

## Технологии

| Слой | Технология |
|---|---|
| Runtime | .NET 9, C# 13 |
| Bot | Telegram.Bot 22.x (Long Polling) |
| AI | OpenAI GPT-4o-mini + Whisper + TTS |
| DB | PostgreSQL 16 + EF Core 9 + Npgsql |
| Cache / FSM | Redis 7 (StackExchange.Redis) |
| Messaging | MediatR 14 (CQRS) |
| Logging | Serilog → Console + File |
| Admin UI | ASP.NET Core MVC + Charts.js |
| Container | Docker Compose |

## Архитектура

```
InstantBot.Domain          ← Entities, Enums, ValueObjects (нет зависимостей)
InstantBot.Application     ← Commands, Queries, Interfaces, Services (→ Domain)
InstantBot.Infrastructure  ← EF Core, Redis, OpenAI, Payments, Jobs (→ Application)
InstantBot.TelegramBot     ← Long Polling, FSM, Handlers, Program.cs (→ Application + Infrastructure)
InstantBot.AdminPanel      ← ASP.NET Core MVC, JWT, Metrics (→ Application + Infrastructure)
```

## Тарифы и цены

| Функция | Free | Premium | VIP |
|---|---|---|---|
| ИИ-наставник | 3 вопроса/день | Безлимит | Безлимит |
| Голосовые ответы | ✗ | ✗ | ✓ |
| Карта дня | ✓ | ✓ | ✓ |
| Прогноз недели | ✗ | ✓ | ✓ |
| Прогноз месяца | ✗ | ✗ | ✓ |
| Анализ отношений | ✗ | ✓ | ✓ |

**Цены (Telegram Stars):** Premium 299/749/2490 ⭐, VIP 599/1499/4990 ⭐ (1/3/12 мес.)

## Структура репозитория

```
src/
  InstantBot.Domain/          # Чистые доменные сущности
  InstantBot.Application/     # CQRS команды/запросы, интерфейсы
  InstantBot.Infrastructure/  # EF Core, OpenAI, Redis, Jobs
  InstantBot.TelegramBot/     # Точка входа бота
  InstantBot.AdminPanel/      # Веб-панель администратора
tests/
  InstantBot.Domain.Tests/       # Unit-тесты домена
  InstantBot.Application.Tests/  # Unit-тесты команд
docs/
  superpowers/specs/   # Дизайн-документ продукта
  superpowers/plans/   # Детальный план реализации
  architecture/        # Техническая документация
```

## Переменные окружения

| Переменная | Описание |
|---|---|
| `TELEGRAM_BOT_TOKEN` | Токен бота от @BotFather |
| `OPENAI_API_KEY` | API-ключ OpenAI |
| `POSTGRES_USER` | Пользователь PostgreSQL |
| `POSTGRES_PASSWORD` | Пароль PostgreSQL |
| `POSTGRES_DB` | Имя базы данных |
| `ADMIN_JWT_SECRET` | Секрет JWT (минимум 32 символа) |
| `ADMIN_USERNAME` | Логин администратора |
| `ADMIN_PASSWORD` | Пароль администратора |

## Запуск тестов

```bash
dotnet test InstantBot.sln
```

22 теста: 15 domain + 7 application. Все проходят.

## Применение миграций

Миграции применяются автоматически при старте `InstantBot.TelegramBot`. Для ручного применения:

```bash
dotnet ef database update \
  --project src/InstantBot.Infrastructure \
  --startup-project src/InstantBot.TelegramBot
```

## AdminPanel

URL: `http://localhost:8080`  
Логин/пароль: из `.env` переменных `ADMIN_USERNAME` / `ADMIN_PASSWORD`

Метрики: DAU, MAU, Conversion, MRR, ARPU, LTV, Churn Rate, 30-дневные графики.
