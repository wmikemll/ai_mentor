# Product Roadmap & Next Steps

## Implemented (M1–M7 + M8)

- ✅ User registration with FSM + personality profile (numerology + archetype)
- ✅ AI Mentor (GPT-4o-mini, context injection, conversation history, summarization)
- ✅ Rate limiting: Free=3/day, Premium=∞, VIP=∞
- ✅ Telegram Stars payments: 6 subscription options
- ✅ Daily card (22 Major Arcana, personalized advice)
- ✅ Streak tracking + achievements (Streak7, Streak30, FirstReferral)
- ✅ Relationship analysis (numerology compatibility)
- ✅ Voice I/O: Whisper (input) + TTS (output, VIP)
- ✅ Referral system (+7 days Premium for referrer)
- ✅ Upsell offers (post-limit, post-relationship, inactivity)
- ✅ Background jobs (DailyCard, Inactivity, SubscriptionExpiry, ConversationSummary)
- ✅ Admin panel (Dashboard, Users, JWT auth, health check)
- ✅ Docker Compose

## Not Yet Implemented (from spec)

### High Priority (revenue impact)

- **Weekly Forecast** (Premium) — same pattern as DailyCardJob, runs Monday 08:00 UTC
  - Add `WeeklyForecast` entity to Domain
  - `GetWeeklyForecastQuery`
  - `WeeklyForecastJob` in NotificationScheduler
  - Wire `weekly_forecast` callback (Premium gate)

- **Monthly Forecast** (VIP) — same pattern, runs 1st of month
  - `MonthlyForecast` entity
  - `MonthlyForecastJob`
  - Wire callback (VIP gate)

- **Free Trial (3 days Premium)** — shown on day 2 after registration
  - In `RegisterUserCommandHandler`: set `SubscriptionTier=Premium, ExpiresAt=+3days`
  - Or via `InactivityJob` check on day 2

### Medium Priority (engagement)

- **Persist RelationshipAnalysis to DB** — currently only returned as string, not saved
  - Add `IRelationshipAnalysisRepository`
  - Save in `AnalyzeRelationshipCommandHandler`
  - Query history: "Мои анализы отношений"

- **Share Result — generate beautiful text** — currently just shows generic message
  - Add `GenerateShareableTextAsync()` to `IOpenAiService`
  - Call in `share_result` callback

- **FirstQuestion achievement** — unlock after first successful mentor question
  - Add check in `AskMentorCommandHandler`

### Admin Panel Additions

- `/subscriptions` page — table of all transactions, revenue by day
- `/referrals` page — top referrers, conversion funnel

### Infrastructure

- **Sentry / error tracking** — add `Sentry.AspNetCore` + `Sentry.Extensions.Logging`
- **Metrics export** — Prometheus endpoint for Grafana dashboard
- **Health check UI** — `AspNetCore.HealthChecks.UI`

## Code Quality TODOs

| Location | Issue |
|---|---|
| `AskMentorCommandHandler` | `StreakService` exists but achievements checked inline — could use it instead |
| `RegisterUserCommandHandler` | Referral bonus done inline — could dispatch `ProcessReferralCommand` for consistency |
| `CallbackHandler` | Growing large — could split by feature (SubscriptionCallbackHandler, etc.) |
| `MessageHandler` | Partner FSM methods are `virtual` — was intended for testing, can be made private |

## Feature Ideas (post-launch)

- **Astrology integration** — birth chart via free API (astro.com or similar)
- **Group analysis** — analyze team/family dynamics (3+ people)
- **Journaling** — daily reflection prompts, AI-generated insights over time
- **Affirmations** — morning affirmation push based on archetype
- **Partner Bot** — separate bot for couples to use together
- **Web App** (Telegram Mini App) — full journal + history view
