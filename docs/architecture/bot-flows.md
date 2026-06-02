# Bot User Flows

## Registration Flow

```
User: /start
Bot: "Привет! Я твой личный ИИ-наставник. Как тебя зовут?"
  → state = AwaitingName

User: "Иван"
Bot: "Приятно познакомиться, Иван! Введи дату рождения ДД.ММ.ГГГГ"
  → state = AwaitingBirthDate
  → session:name = "Иван"

User: "15.05.1990"
  → RegisterUserCommand dispatched
  → NumerologyCalculator: LifePath=3, Destiny=5, Soul=2
  → ArchetypeAnalyzer: LifePath=3 → Творец
  → PersonalityProfile created
  → User saved to DB
Bot: "✨ Твой профиль готов, Иван!
      🔢 Число жизненного пути: 3
      🌟 Архетип: Творец
      💪 Сильные стороны: Креативность, Общительность, Оптимизм
      ..."
  → state = MainMenu
  → Shows MainMenuKeyboard
```

### Referral Registration

```
User: /start REF_ABC123
  → refCode = "ABC123" saved to session:ref_code
  → (same flow as above)
  → On RegisterUserCommandHandler:
      referrer found by code → user.SetReferredBy(referrer.Id)
      referrer.UpdateSubscription(Premium, +7 days)
      → referrer notified: "🎉 Ваш друг присоединился! +7 дней Premium"
```

## AI Mentor Flow

```
User clicks: "🔮 Спросить наставника"
  → CallbackHandler: state = AwaitingMentorQuestion

User: "Стоит ли мне менять работу?"
  → AskMentorCommand dispatched
  → Load user (with PersonalityProfile) from DB
  → Check CanAskQuestion():
      Free: DailyQuestionsUsed < 3 → OK / else → LimitReached

  [If OK]:
  → Load last 5/10/20 messages from Conversation (by tier)
  → Build system prompt with user context
  → GPT-4o-mini → response (300/600/1000 token limit)
  → Save user + assistant messages to DB
  → user.IncrementDailyQuestions()
  → user.UpdateStreak() → CheckStreakAchievements()
  Bot: "[response text]
        _Осталось вопросов сегодня: 2_"  ← Free tier only

  [If LimitReached]:
  Bot: "Ты использовал все 3 бесплатных вопроса на сегодня. 🌙
        🎁 Скидка 20% на Premium — только 24 часа!"  ← first time only
  → Shows SubscriptionKeyboard.GetUpsell()
```

## Relationship Analysis Flow

```
User clicks: "❤️ Анализ отношений"
  → Check tier: Free → upsell message
  → Premium/VIP: state = AwaitingPartnerName

User: "Мария"
  → session:partner_name = "Мария"
  → state = AwaitingPartnerBirthDate

User: "20.03.1992"
  → AnalyzeRelationshipCommand dispatched
  → OpenAI.GenerateRelationshipAnalysisAsync()
      userLifePath + partnerLifePath → compatibility analysis
  → state = MainMenu
  Bot: "❤️ Анализ пары: Иван & Мария

        💪 СИЛЬНЫЕ СТОРОНЫ ПАРЫ: ...
        ⚡ ЗОНЫ КОНФЛИКТОВ: ...
        💬 СТИЛЬ ОБЩЕНИЯ: ...
        🌱 РЕКОМЕНДАЦИИ: ..."
  → ShareKeyboard shown

  [If Premium user, first time]:
  → 2 second delay
  Bot: "💡 Хочешь голосовые ответы и расширенную аналитику? Доступно в VIP!"
  → offer:vip_after_relationship marked (TTL 7d)
```

## Voice Flow (VIP only)

```
VIP user sends voice message
  → VoiceHandler.HandleAsync()
  → user.SubscriptionTier == VIP? No → upsell message
  → bot.GetFile(voice.FileId) → download OGG
  → WhisperService.TranscribeAsync(stream, "voice.ogg") → text
  → AskMentorCommand(uid, transcribedText)
  → TtsService.SynthesizeAsync(responseText) → MP3 bytes
  → bot.SendVoice(uid, mp3Stream, caption: first 200 chars)
```

## Payment Flow

```
User clicks: "Premium — 299 ⭐/мес"
  → CallbackHandler.HandleSubscriptionCallbackAsync("sub_premium_1mo")
  → TelegramStarsService.SendInvoiceAsync(bot, uid, Premium, OneMonth)
  → bot.SendInvoice(currency: "XTR", prices: [299], payload: "Premium:OneMonth:{uid}")

Telegram shows Stars payment screen
  → User confirms payment

Telegram sends PreCheckoutQuery
  → PaymentHandler.HandlePreCheckoutAsync()
  → bot.AnswerPreCheckoutQuery(ok: true)

Telegram sends SuccessfulPayment
  → PaymentHandler.HandleSuccessfulPaymentAsync()
  → Parse payload: tier=Premium, plan=OneMonth
  → PurchaseSubscriptionCommand dispatched
  → user.UpdateSubscription(Premium, now+1month)
  Bot: "🎉 Подписка Premium ⭐ активирована!"
```

## Daily Card Flow

```
[08:00 UTC — DailyCardJob]
  → Load all active users
  → For each (in batches of 50):
      → Random TarotCard from 22 Major Arcana
      → OpenAI.GenerateDailyCardAsync(user, card.Name, card.Meaning)
      → Parse СОВЕТ: and ФОКУС: sections
      → DailyCard.Create() → save to DB
      → cache.SetDailyCardCacheAsync() → Redis TTL 24h
      → bot.SendMessage(user): "🌅 Доброе утро, {name}! 🃏 {cardName}..."
      → 500ms delay between batches

[User clicks "💫 Карта дня"]
  → GetDailyCardQuery dispatched
  → Load from DB (or Redis cache)
  → Show card with ShareKeyboard
```

## Referral Flow

```
User clicks "👥 Пригласить друга"
  → bot.GetMe() → botUsername
  → link = "https://t.me/{botUsername}?start=REF_{user.ReferralCode}"
  Bot: "🔗 Твоя реферальная ссылка: {link}
       Поделись — ты получишь +7 дней Premium, друг — расширенный триал!"

Friend registers via REF link:
  → RegisterUserCommand(refCode: "ABC123")
  → referrer found → referrer gets Premium +7 days
  → achievement FirstReferral added to referrer
  → referrer notified
```

## Subscription Expiry Reminders

```
[Every 6h — SubscriptionExpiryJob]
  → Find users expiring in <3 days AND offer_expiry_3d not shown:
      Bot: "⏰ Твоя подписка истекает через 3 дня..."
      → mark offer:expiry_3d TTL 2d

  → Find users expired today (last 6h):
      Bot: "💔 Твоя подписка истекла. Скидка 10% на продление!"
```

## Inactivity Re-engagement

```
[10:00 UTC — InactivityJob]
  → Users inactive exactly 3 days:
      Bot: "Привет, {name}! 👋 Скучаем по тебе. Скидка 15% на Premium!"
      → mark offer:inactivity_3d TTL 3d

  → Users inactive exactly 7 days:
      Bot: "{name}, прошла неделя! 🌙 Скидка 25% + бонус."
      → mark offer:inactivity_7d TTL 7d
```
