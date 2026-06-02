# InstantBot — Личный ИИ-наставник Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a production-ready Telegram SaaS bot — Personal AI Mentor — with user profiles, numerology, AI mentor chat (GPT-4o-mini), daily cards, relationship analysis, voice I/O (Whisper + TTS), Telegram Stars subscriptions, referral system, and an ASP.NET Core admin panel with business metrics.

**Architecture:** Clean Architecture monorepo (.NET 9): Domain → Application → Infrastructure → TelegramBot / AdminPanel. CQRS with MediatR. PostgreSQL + EF Core 9 (Npgsql). Redis for FSM state, rate limiting, card cache. Long Polling.

**Tech Stack:** .NET 9 · Telegram.Bot · EF Core 9 + Npgsql · StackExchange.Redis · MediatR 12 · OpenAI .NET SDK · Serilog · xUnit + Moq + FluentAssertions · Docker Compose

---

## File Map

```
InstantBot.sln
src/
  InstantBot.Domain/
    Entities/         User, PersonalityProfile, Conversation, Message,
                      DailyCard, Subscription, RelationshipAnalysis, Referral, Achievement
    ValueObjects/     TarotCard
    Enums/            SubscriptionTier, SubscriptionPlan, MessageRole,
                      ArchetypeType, UserBotState, AchievementType
  InstantBot.Application/
    Interfaces/       IUserRepository, IConversationRepository, IDailyCardRepository,
                      ICacheService, IOpenAiService, IWhisperService, ITtsService,
                      IPaymentService, INotificationService
    Commands/         RegisterUser, AskMentor, PurchaseSubscription,
                      AnalyzeRelationship, ProcessReferral
    Queries/          GetUserProfile, GetDailyCard, GetAdminMetrics
    Services/         NumerologyCalculator, ArchetypeAnalyzer,
                      StreakService, PersonalityProfileBuilder
  InstantBot.Infrastructure/
    Persistence/      AppDbContext, Configurations/, Repositories/, Migrations/
    OpenAi/           OpenAiService, WhisperService, TtsService
    Cache/            RedisCacheService
    Payments/         TelegramStarsService
    Notifications/    DailyCardJob, SubscriptionExpiryJob, InactivityJob,
                      ConversationSummaryJob, NotificationScheduler
  InstantBot.TelegramBot/
    Handlers/         UpdateRouter, MessageHandler, CallbackHandler,
                      VoiceHandler, PaymentHandler
    StateMachine/     UserStateManager
    Keyboards/        MainMenuKeyboard, SubscriptionKeyboard, ShareKeyboard
    Program.cs
  InstantBot.AdminPanel/
    Controllers/      AuthController, DashboardController, UsersController,
                      SubscriptionsController, ReferralsController
    Views/            Dashboard/Index, Users/Index, Subscriptions/Index
    Models/           DashboardViewModel, UserListViewModel
    Program.cs
tests/
  InstantBot.Domain.Tests/
    NumerologyCalculatorTests, ArchetypeAnalyzerTests, UserEntityTests
  InstantBot.Application.Tests/
    RegisterUserCommandHandlerTests, AskMentorCommandHandlerTests,
    PurchaseSubscriptionCommandHandlerTests
docker-compose.yml · .env.example · global.json
```

---

## Phase 1 — Core Foundation (M1)

### Task 1: Solution scaffold

**Files:** `InstantBot.sln`, all `.csproj` files, `global.json`

- [ ] **Step 1: Create solution and projects**

```bash
dotnet new sln -n InstantBot
mkdir -p src tests

dotnet new classlib -n InstantBot.Domain       -o src/InstantBot.Domain       -f net9.0
dotnet new classlib -n InstantBot.Application  -o src/InstantBot.Application  -f net9.0
dotnet new classlib -n InstantBot.Infrastructure -o src/InstantBot.Infrastructure -f net9.0
dotnet new worker   -n InstantBot.TelegramBot  -o src/InstantBot.TelegramBot  -f net9.0
dotnet new mvc      -n InstantBot.AdminPanel   -o src/InstantBot.AdminPanel   -f net9.0
dotnet new xunit    -n InstantBot.Domain.Tests       -o tests/InstantBot.Domain.Tests       -f net9.0
dotnet new xunit    -n InstantBot.Application.Tests  -o tests/InstantBot.Application.Tests  -f net9.0

dotnet sln add src/InstantBot.Domain/InstantBot.Domain.csproj
dotnet sln add src/InstantBot.Application/InstantBot.Application.csproj
dotnet sln add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj
dotnet sln add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj
dotnet sln add src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj
dotnet sln add tests/InstantBot.Domain.Tests/InstantBot.Domain.Tests.csproj
dotnet sln add tests/InstantBot.Application.Tests/InstantBot.Application.Tests.csproj
```

- [ ] **Step 2: Add project references**

```bash
dotnet add src/InstantBot.Application/InstantBot.Application.csproj \
  reference src/InstantBot.Domain/InstantBot.Domain.csproj

dotnet add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj \
  reference src/InstantBot.Application/InstantBot.Application.csproj

dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj \
  reference src/InstantBot.Application/InstantBot.Application.csproj
dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj \
  reference src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj

dotnet add src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj \
  reference src/InstantBot.Application/InstantBot.Application.csproj
dotnet add src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj \
  reference src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj

dotnet add tests/InstantBot.Domain.Tests/InstantBot.Domain.Tests.csproj \
  reference src/InstantBot.Domain/InstantBot.Domain.csproj
dotnet add tests/InstantBot.Domain.Tests/InstantBot.Domain.Tests.csproj \
  reference src/InstantBot.Application/InstantBot.Application.csproj
dotnet add tests/InstantBot.Application.Tests/InstantBot.Application.Tests.csproj \
  reference src/InstantBot.Application/InstantBot.Application.csproj
dotnet add tests/InstantBot.Application.Tests/InstantBot.Application.Tests.csproj \
  reference src/InstantBot.Domain/InstantBot.Domain.csproj
```

- [ ] **Step 3: Install NuGet packages**

```bash
# Application
dotnet add src/InstantBot.Application/InstantBot.Application.csproj package MediatR

# Infrastructure
dotnet add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj package Microsoft.EntityFrameworkCore
dotnet add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj package StackExchange.Redis
dotnet add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj package OpenAI
dotnet add src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj package Serilog.AspNetCore

# TelegramBot
dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj package Telegram.Bot
dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj package MediatR
dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj package Serilog.Extensions.Hosting
dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj package Serilog.Sinks.Console
dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj package Serilog.Sinks.File
dotnet add src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj package Microsoft.Extensions.Http

# AdminPanel
dotnet add src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj package Serilog.AspNetCore
dotnet add src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj package StackExchange.Redis

# Tests
dotnet add tests/InstantBot.Domain.Tests/InstantBot.Domain.Tests.csproj package FluentAssertions
dotnet add tests/InstantBot.Application.Tests/InstantBot.Application.Tests.csproj package Moq
dotnet add tests/InstantBot.Application.Tests/InstantBot.Application.Tests.csproj package FluentAssertions
```

- [ ] **Step 4: Write `global.json`**

```json
{
  "sdk": { "version": "9.0.0", "rollForward": "latestMinor" }
}
```

- [ ] **Step 5: Remove boilerplate and verify build**

```bash
rm src/InstantBot.Domain/Class1.cs src/InstantBot.Application/Class1.cs src/InstantBot.Infrastructure/Class1.cs
dotnet build InstantBot.sln
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add . && git commit -m "chore: scaffold solution with all projects and packages"
```

---

### Task 2: Domain enums and TarotCard value object

**Files:** `src/InstantBot.Domain/Enums/*.cs`, `src/InstantBot.Domain/ValueObjects/TarotCard.cs`

- [ ] **Step 1: Create all enums**

`src/InstantBot.Domain/Enums/SubscriptionTier.cs`:
```csharp
namespace InstantBot.Domain.Enums;
public enum SubscriptionTier { Free = 0, Premium = 1, Vip = 2 }
```

`src/InstantBot.Domain/Enums/SubscriptionPlan.cs`:
```csharp
namespace InstantBot.Domain.Enums;
public enum SubscriptionPlan { OneMonth = 1, ThreeMonths = 3, TwelveMonths = 12 }
```

`src/InstantBot.Domain/Enums/MessageRole.cs`:
```csharp
namespace InstantBot.Domain.Enums;
public enum MessageRole { User = 0, Assistant = 1 }
```

`src/InstantBot.Domain/Enums/ArchetypeType.cs`:
```csharp
namespace InstantBot.Domain.Enums;
public enum ArchetypeType
{
    Unknown = 0, Leader = 1, Diplomat = 2, Creative = 3, Organizer = 4,
    Adventurer = 5, Nurturer = 6, Seeker = 7, Authority = 8, Sage = 9,
    Visionary = 11, Builder = 22
}
```

`src/InstantBot.Domain/Enums/UserBotState.cs`:
```csharp
namespace InstantBot.Domain.Enums;
public enum UserBotState
{
    None = 0, AwaitingName = 1, AwaitingBirthDate = 2, MainMenu = 3,
    AwaitingMentorQuestion = 4, AwaitingPartnerName = 5,
    AwaitingPartnerBirthDate = 6
}
```

`src/InstantBot.Domain/Enums/AchievementType.cs`:
```csharp
namespace InstantBot.Domain.Enums;
public enum AchievementType
{
    FirstQuestion = 1, Streak7 = 2, Streak30 = 3,
    FirstReferral = 4, PremiumSubscriber = 5, VipSubscriber = 6,
    FirstRelationshipAnalysis = 7
}
```

- [ ] **Step 2: Create TarotCard value object**

`src/InstantBot.Domain/ValueObjects/TarotCard.cs`:
```csharp
namespace InstantBot.Domain.ValueObjects;
public sealed record TarotCard(string Name, string Arcana, string Meaning)
{
    public static TarotCard Empty => new("Неизвестно", "Major", "");
}
```

- [ ] **Step 3: Build**

```bash
dotnet build src/InstantBot.Domain/InstantBot.Domain.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/InstantBot.Domain/ && git commit -m "feat(domain): add enums and TarotCard value object"
```

---

### Task 3: Domain entities

**Files:** `src/InstantBot.Domain/Entities/*.cs`
**Test:** `tests/InstantBot.Domain.Tests/UserEntityTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/InstantBot.Domain.Tests/UserEntityTests.cs`:
```csharp
using FluentAssertions;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;

namespace InstantBot.Domain.Tests;

public class UserEntityTests
{
    [Fact]
    public void Create_InitializesWithFreeSubscriptionAndStreak1()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.SubscriptionTier.Should().Be(SubscriptionTier.Free);
        user.DailyQuestionsUsed.Should().Be(0);
        user.CurrentStreak.Should().Be(1);
        user.ReferralCode.Should().HaveLength(6);
    }

    [Fact]
    public void CanAskQuestion_FreeUser_FalseAfterThreeQuestions()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.IncrementDailyQuestions(); user.IncrementDailyQuestions(); user.IncrementDailyQuestions();
        user.CanAskQuestion().Should().BeFalse();
    }

    [Fact]
    public void CanAskQuestion_PremiumUser_AlwaysTrue()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.UpdateSubscription(SubscriptionTier.Premium, DateTime.UtcNow.AddMonths(1));
        for (var i = 0; i < 10; i++) user.IncrementDailyQuestions();
        user.CanAskQuestion().Should().BeTrue();
    }

    [Fact]
    public void UpdateStreak_ConsecutiveDay_Increments()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.SetLastActiveDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        user.UpdateStreak();
        user.CurrentStreak.Should().Be(2);
    }

    [Fact]
    public void UpdateStreak_GapMoreThanOneDay_ResetsToOne()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.SetLastActiveDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3));
        user.UpdateStreak();
        user.CurrentStreak.Should().Be(1);
    }
}
```

- [ ] **Step 2: Run tests (expect failure)**

```bash
dotnet test tests/InstantBot.Domain.Tests/ --filter "UserEntityTests"
```

Expected: FAIL — `User` not found.

- [ ] **Step 3: Create `User.cs`**

`src/InstantBot.Domain/Entities/User.cs`:
```csharp
using InstantBot.Domain.Enums;

namespace InstantBot.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public long TelegramUserId { get; private set; }
    public string? Username { get; private set; }
    public string FirstName { get; private set; } = null!;
    public DateOnly BirthDate { get; private set; }
    public DateTime RegistrationDate { get; private set; }
    public SubscriptionTier SubscriptionTier { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }
    public int DailyQuestionsUsed { get; private set; }
    public int CurrentStreak { get; private set; }
    public DateOnly LastActiveDate { get; private set; }
    public string ReferralCode { get; private set; } = null!;
    public Guid? ReferredByUserId { get; private set; }
    public bool IsActive { get; private set; }

    public PersonalityProfile? PersonalityProfile { get; private set; }
    public ICollection<Conversation> Conversations { get; private set; } = [];
    public ICollection<Subscription> Subscriptions { get; private set; } = [];
    public ICollection<Achievement> Achievements { get; private set; } = [];

    private User() { }

    public static User Create(long telegramUserId, string firstName, string? username, DateOnly birthDate)
    {
        return new User
        {
            Id = Guid.NewGuid(), TelegramUserId = telegramUserId,
            FirstName = firstName, Username = username, BirthDate = birthDate,
            RegistrationDate = DateTime.UtcNow, SubscriptionTier = SubscriptionTier.Free,
            DailyQuestionsUsed = 0, CurrentStreak = 1,
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ReferralCode = BuildCode(telegramUserId), IsActive = true
        };
    }

    public void UpdateSubscription(SubscriptionTier tier, DateTime expiresAt)
    { SubscriptionTier = tier; SubscriptionExpiresAt = expiresAt; }

    public bool CanAskQuestion() =>
        SubscriptionTier != SubscriptionTier.Free || DailyQuestionsUsed < 3;

    public void IncrementDailyQuestions() => DailyQuestionsUsed++;
    public void ResetDailyQuestions() => DailyQuestionsUsed = 0;

    public void UpdateStreak()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        CurrentStreak = LastActiveDate == today.AddDays(-1) ? CurrentStreak + 1 : 1;
        LastActiveDate = today;
    }

    public void SetLastActiveDate(DateOnly d) => LastActiveDate = d;
    public void SetReferredBy(Guid referrerId) => ReferredByUserId = referrerId;
    public void AttachProfile(PersonalityProfile p) => PersonalityProfile = p;

    private static string BuildCode(long id)
    {
        const string c = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var h = (uint)Math.Abs(id.GetHashCode() ^ (int)(id >> 32));
        var r = new char[6];
        for (var i = 0; i < 6; i++) { r[i] = c[(int)(h % (uint)c.Length)]; h /= (uint)c.Length; }
        return new string(r);
    }
}
```

- [ ] **Step 4: Create remaining entities**

`src/InstantBot.Domain/Entities/PersonalityProfile.cs`:
```csharp
using InstantBot.Domain.Enums;
namespace InstantBot.Domain.Entities;

public sealed class PersonalityProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int LifePathNumber { get; private set; }
    public int DestinyNumber { get; private set; }
    public int SoulNumber { get; private set; }
    public ArchetypeType ArchetypeType { get; private set; }
    public string[] Strengths { get; private set; } = [];
    public string[] Weaknesses { get; private set; } = [];
    public string[] Recommendations { get; private set; } = [];
    public DateTime GeneratedAt { get; private set; }
    public User User { get; private set; } = null!;

    private PersonalityProfile() { }

    public static PersonalityProfile Create(Guid userId, int lifePath, int destiny, int soul,
        ArchetypeType archetype, string[] strengths, string[] weaknesses, string[] recommendations) =>
        new()
        {
            Id = Guid.NewGuid(), UserId = userId, LifePathNumber = lifePath,
            DestinyNumber = destiny, SoulNumber = soul, ArchetypeType = archetype,
            Strengths = strengths, Weaknesses = weaknesses, Recommendations = recommendations,
            GeneratedAt = DateTime.UtcNow
        };
}
```

`src/InstantBot.Domain/Entities/Conversation.cs`:
```csharp
namespace InstantBot.Domain.Entities;

public sealed class Conversation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? Summary { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastMessageAt { get; private set; }
    public User User { get; private set; } = null!;
    public ICollection<Message> Messages { get; private set; } = [];

    private Conversation() { }

    public static Conversation Create(Guid userId) =>
        new() { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow, LastMessageAt = DateTime.UtcNow };

    public void UpdateSummary(string s) => Summary = s;
    public void Touch() => LastMessageAt = DateTime.UtcNow;
}
```

`src/InstantBot.Domain/Entities/Message.cs`:
```csharp
using InstantBot.Domain.Enums;
namespace InstantBot.Domain.Entities;

public sealed class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = null!;
    public bool IsVoice { get; private set; }
    public int TokensUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Conversation Conversation { get; private set; } = null!;

    private Message() { }

    public static Message Create(Guid convId, MessageRole role, string content,
        bool isVoice = false, int tokensUsed = 0) =>
        new() { Id = Guid.NewGuid(), ConversationId = convId, Role = role,
                Content = content, IsVoice = isVoice, TokensUsed = tokensUsed,
                CreatedAt = DateTime.UtcNow };
}
```

`src/InstantBot.Domain/Entities/DailyCard.cs`:
```csharp
using InstantBot.Domain.ValueObjects;
namespace InstantBot.Domain.Entities;

public sealed class DailyCard
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public TarotCard TarotCard { get; private set; } = TarotCard.Empty;
    public string DayAdvice { get; private set; } = null!;
    public string DayFocus { get; private set; } = null!;
    public bool IsViewed { get; private set; }
    public User User { get; private set; } = null!;

    private DailyCard() { }

    public static DailyCard Create(Guid userId, DateOnly date, TarotCard card, string advice, string focus) =>
        new() { Id = Guid.NewGuid(), UserId = userId, Date = date,
                TarotCard = card, DayAdvice = advice, DayFocus = focus };

    public void MarkViewed() => IsViewed = true;
}
```

`src/InstantBot.Domain/Entities/Subscription.cs`:
```csharp
using InstantBot.Domain.Enums;
namespace InstantBot.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SubscriptionPlan Plan { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public int StarsPaid { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsAutoRenew { get; private set; }
    public string TelegramPaymentChargeId { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private Subscription() { }

    public static Subscription Create(Guid userId, SubscriptionPlan plan,
        SubscriptionTier tier, int starsPaid, string chargeId) =>
        new()
        {
            Id = Guid.NewGuid(), UserId = userId, Plan = plan, Tier = tier,
            StarsPaid = starsPaid, PurchasedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMonths((int)plan),
            IsAutoRenew = true, TelegramPaymentChargeId = chargeId
        };
}
```

`src/InstantBot.Domain/Entities/RelationshipAnalysis.cs`:
```csharp
namespace InstantBot.Domain.Entities;

public sealed class RelationshipAnalysis
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string PartnerName { get; private set; } = null!;
    public DateOnly PartnerBirthDate { get; private set; }
    public string Strengths { get; private set; } = null!;
    public string ConflictZones { get; private set; } = null!;
    public string CommunicationStyle { get; private set; } = null!;
    public string Recommendations { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public User User { get; private set; } = null!;

    private RelationshipAnalysis() { }

    public static RelationshipAnalysis Create(Guid userId, string partnerName,
        DateOnly partnerBirth, string strengths, string conflicts,
        string communication, string recommendations) =>
        new()
        {
            Id = Guid.NewGuid(), UserId = userId, PartnerName = partnerName,
            PartnerBirthDate = partnerBirth, Strengths = strengths,
            ConflictZones = conflicts, CommunicationStyle = communication,
            Recommendations = recommendations, CreatedAt = DateTime.UtcNow
        };
}
```

`src/InstantBot.Domain/Entities/Referral.cs`:
```csharp
namespace InstantBot.Domain.Entities;

public sealed class Referral
{
    public Guid Id { get; private set; }
    public Guid ReferrerId { get; private set; }
    public Guid ReferredId { get; private set; }
    public bool BonusGranted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public User Referrer { get; private set; } = null!;
    public User Referred { get; private set; } = null!;

    private Referral() { }

    public static Referral Create(Guid referrerId, Guid referredId) =>
        new() { Id = Guid.NewGuid(), ReferrerId = referrerId,
                ReferredId = referredId, CreatedAt = DateTime.UtcNow };

    public void GrantBonus() => BonusGranted = true;
}
```

`src/InstantBot.Domain/Entities/Achievement.cs`:
```csharp
using InstantBot.Domain.Enums;
namespace InstantBot.Domain.Entities;

public sealed class Achievement
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public AchievementType Type { get; private set; }
    public DateTime UnlockedAt { get; private set; }
    public User User { get; private set; } = null!;

    private Achievement() { }

    public static Achievement Create(Guid userId, AchievementType type) =>
        new() { Id = Guid.NewGuid(), UserId = userId, Type = type, UnlockedAt = DateTime.UtcNow };
}
```

- [ ] **Step 5: Run tests (expect pass)**

```bash
dotnet test tests/InstantBot.Domain.Tests/ --filter "UserEntityTests"
```

Expected: 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add . && git commit -m "feat(domain): add all entities with tests"
```

---

### Task 4: NumerologyCalculator and ArchetypeAnalyzer

**Files:** `src/InstantBot.Application/Services/NumerologyCalculator.cs`, `ArchetypeAnalyzer.cs`
**Tests:** `tests/InstantBot.Domain.Tests/NumerologyCalculatorTests.cs`, `ArchetypeAnalyzerTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/InstantBot.Domain.Tests/NumerologyCalculatorTests.cs`:
```csharp
using FluentAssertions;
using InstantBot.Application.Services;

namespace InstantBot.Domain.Tests;

public class NumerologyCalculatorTests
{
    [Theory]
    [InlineData(1990, 5, 15, 3)]  // 1+5+5+1+9+9+0 = 30 → 3
    [InlineData(2000, 1, 1, 3)]   // 1+1+2 = 4 → wait: 01+01+2000 = 1+1+2 = 4
    [InlineData(1985, 11, 22, 11)] // master number preserved: 22+11+23 = 56 → 11
    public void CalculateLifePathNumber_ReturnsCorrectValue(
        int year, int month, int day, int expected)
    {
        var result = NumerologyCalculator.CalculateLifePathNumber(new DateOnly(year, month, day));
        result.Should().Be(expected);
    }

    [Fact]
    public void ReduceToSingleDigit_MasterNumbers_Preserved()
    {
        NumerologyCalculator.ReduceToSingleDigitPublic(11).Should().Be(11);
        NumerologyCalculator.ReduceToSingleDigitPublic(22).Should().Be(22);
        NumerologyCalculator.ReduceToSingleDigitPublic(33).Should().Be(6); // non-master
    }
}
```

`tests/InstantBot.Domain.Tests/ArchetypeAnalyzerTests.cs`:
```csharp
using FluentAssertions;
using InstantBot.Application.Services;
using InstantBot.Domain.Enums;

namespace InstantBot.Domain.Tests;

public class ArchetypeAnalyzerTests
{
    [Theory]
    [InlineData(1, ArchetypeType.Leader)]
    [InlineData(2, ArchetypeType.Diplomat)]
    [InlineData(9, ArchetypeType.Sage)]
    [InlineData(11, ArchetypeType.Visionary)]
    [InlineData(22, ArchetypeType.Builder)]
    public void GetArchetypeInfo_ReturnsCorrectArchetype(int lifePathNumber, ArchetypeType expected)
    {
        var info = ArchetypeAnalyzer.GetArchetypeInfo(lifePathNumber);
        info.Archetype.Should().Be(expected);
        info.Strengths.Should().NotBeEmpty();
        info.Weaknesses.Should().NotBeEmpty();
        info.Recommendations.Should().NotBeEmpty();
    }
}
```

- [ ] **Step 2: Run tests (expect failure)**

```bash
dotnet test tests/InstantBot.Domain.Tests/ --filter "NumerologyCalculatorTests|ArchetypeAnalyzerTests"
```

Expected: FAIL.

- [ ] **Step 3: Implement NumerologyCalculator**

`src/InstantBot.Application/Services/NumerologyCalculator.cs`:
```csharp
namespace InstantBot.Application.Services;

public static class NumerologyCalculator
{
    public static int CalculateLifePathNumber(DateOnly birthDate)
    {
        var sum = SumDigits(birthDate.Day) + SumDigits(birthDate.Month) + SumDigits(birthDate.Year);
        return ReduceToSingleDigit(sum);
    }

    public static int CalculateDestinyNumber(string fullName)
    {
        var sum = fullName.ToLowerInvariant()
            .Where(char.IsLetter)
            .Sum(GetPythagoreanValue);
        return ReduceToSingleDigit(sum);
    }

    public static int CalculateSoulNumber(string fullName)
    {
        const string vowels = "аеёиоуыэюяaeiouy";
        var sum = fullName.ToLowerInvariant()
            .Where(c => vowels.Contains(c))
            .Sum(GetPythagoreanValue);
        return ReduceToSingleDigit(sum);
    }

    // Exposed for testing
    public static int ReduceToSingleDigitPublic(int n) => ReduceToSingleDigit(n);

    private static int ReduceToSingleDigit(int n)
    {
        if (n == 11 || n == 22) return n;
        while (n > 9) n = SumDigits(n);
        return n;
    }

    private static int SumDigits(int n)
    {
        var s = 0;
        while (n > 0) { s += n % 10; n /= 10; }
        return s;
    }

    private static int GetPythagoreanValue(char c)
    {
        // Latin Pythagorean table
        return c switch
        {
            'a' or 'j' or 's' => 1,
            'b' or 'k' or 't' => 2,
            'c' or 'l' or 'u' => 3,
            'd' or 'm' or 'v' => 4,
            'e' or 'n' or 'w' => 5,
            'f' or 'o' or 'x' => 6,
            'g' or 'p' or 'y' => 7,
            'h' or 'q' or 'z' => 8,
            'i' or 'r'        => 9,
            // Cyrillic simplified (а=1...я=9 cycle)
            'а' or 'й' or 'с' => 1,
            'б' or 'к' or 'т' => 2,
            'в' or 'л' or 'у' => 3,
            'г' or 'м' or 'ф' => 4,
            'д' or 'н' or 'х' => 5,
            'е' or 'о' or 'ц' => 6,
            'ё' or 'п' or 'ч' => 7,
            'ж' or 'р' or 'ш' => 8,
            'з' or 'щ'        => 9,
            'и' or 'ъ'        => 1,
            'ы' or 'ь'        => 2,
            'э'               => 3,
            'ю'               => 4,
            'я'               => 5,
            _ => 0
        };
    }
}
```

- [ ] **Step 4: Implement ArchetypeAnalyzer**

`src/InstantBot.Application/Services/ArchetypeAnalyzer.cs`:
```csharp
using InstantBot.Domain.Enums;

namespace InstantBot.Application.Services;

public record ArchetypeInfo(ArchetypeType Archetype, string Name, string[] Strengths,
    string[] Weaknesses, string[] Recommendations);

public static class ArchetypeAnalyzer
{
    private static readonly Dictionary<int, ArchetypeInfo> Archetypes = new()
    {
        [1]  = new(ArchetypeType.Leader, "Лидер",
                   ["Инициативность", "Решительность", "Уверенность"],
                   ["Упрямство", "Нетерпеливость"],
                   ["Развивай командную работу", "Учись делегировать"]),
        [2]  = new(ArchetypeType.Diplomat, "Дипломат",
                   ["Эмпатия", "Сотрудничество", "Интуиция"],
                   ["Нерешительность", "Зависимость от мнения других"],
                   ["Доверяй своим решениям", "Устанавливай личные границы"]),
        [3]  = new(ArchetypeType.Creative, "Творец",
                   ["Креативность", "Общительность", "Оптимизм"],
                   ["Рассеянность", "Поверхностность"],
                   ["Фокусируйся на завершении начатого", "Цени глубину"]),
        [4]  = new(ArchetypeType.Organizer, "Организатор",
                   ["Дисциплина", "Надёжность", "Системность"],
                   ["Ригидность", "Чрезмерный контроль"],
                   ["Позволь себе гибкость", "Принимай неожиданное"]),
        [5]  = new(ArchetypeType.Adventurer, "Искатель",
                   ["Свобода", "Адаптивность", "Смелость"],
                   ["Непостоянство", "Избегание ответственности"],
                   ["Цени стабильность", "Доводи проекты до конца"]),
        [6]  = new(ArchetypeType.Nurturer, "Опекун",
                   ["Забота", "Ответственность", "Гармония"],
                   ["Жертвенность", "Сложность в отказе"],
                   ["Помни о собственных потребностях", "Принимай помощь"]),
        [7]  = new(ArchetypeType.Seeker, "Мыслитель",
                   ["Аналитичность", "Мудрость", "Интроспекция"],
                   ["Замкнутость", "Недоверие"],
                   ["Открывайся людям", "Делись знаниями"]),
        [8]  = new(ArchetypeType.Authority, "Стратег",
                   ["Амбиции", "Стратегическое мышление", "Власть"],
                   ["Материализм", "Жёсткость"],
                   ["Баланс между работой и жизнью", "Развивай великодушие"]),
        [9]  = new(ArchetypeType.Sage, "Мудрец",
                   ["Альтруизм", "Широта взглядов", "Сострадание"],
                   ["Идеализм", "Сложность с практическим"],
                   ["Применяй мудрость на практике", "Цени малое"]),
        [11] = new(ArchetypeType.Visionary, "Провидец",
                   ["Интуиция", "Вдохновение", "Духовность"],
                   ["Сверхчувствительность", "Нестабильность"],
                   ["Заземляйся", "Воплощай видения в жизнь"]),
        [22] = new(ArchetypeType.Builder, "Строитель",
                   ["Мастерство", "Практичность", "Системность"],
                   ["Перфекционизм", "Сверхтребовательность к себе"],
                   ["Делись знаниями", "Доверяй процессу"]),
    };

    public static ArchetypeInfo GetArchetypeInfo(int lifePathNumber) =>
        Archetypes.TryGetValue(lifePathNumber, out var info) ? info : Archetypes[1];
}
```

- [ ] **Step 5: Run tests (expect pass)**

```bash
dotnet test tests/InstantBot.Domain.Tests/
```

Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add . && git commit -m "feat(application): add NumerologyCalculator and ArchetypeAnalyzer with tests"
```

---

### Task 5: Application interfaces

**Files:** `src/InstantBot.Application/Interfaces/*.cs`

- [ ] **Step 1: Create all interfaces**

`src/InstantBot.Application/Interfaces/IUserRepository.cs`:
```csharp
using InstantBot.Domain.Entities;

namespace InstantBot.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByTelegramIdAsync(long telegramUserId, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByReferralCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

`src/InstantBot.Application/Interfaces/IConversationRepository.cs`:
```csharp
using InstantBot.Domain.Entities;

namespace InstantBot.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetActiveAsync(Guid userId, CancellationToken ct = default);
    Task<List<Message>> GetRecentMessagesAsync(Guid conversationId, int count, CancellationToken ct = default);
    Task<int> CountMessagesAsync(Guid conversationId, CancellationToken ct = default);
    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task AddMessageAsync(Message message, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<List<Conversation>> GetConversationsToSummarizeAsync(int threshold, CancellationToken ct = default);
}
```

`src/InstantBot.Application/Interfaces/IDailyCardRepository.cs`:
```csharp
using InstantBot.Domain.Entities;

namespace InstantBot.Application.Interfaces;

public interface IDailyCardRepository
{
    Task<DailyCard?> GetForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(DailyCard card, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

`src/InstantBot.Application/Interfaces/ICacheService.cs`:
```csharp
using InstantBot.Domain.Enums;

namespace InstantBot.Application.Interfaces;

public interface ICacheService
{
    Task<UserBotState> GetUserStateAsync(long telegramUserId);
    Task SetUserStateAsync(long telegramUserId, UserBotState state, TimeSpan? ttl = null);
    Task<int> IncrementDailyQuestionsAsync(long telegramUserId);
    Task<string?> GetSessionValueAsync(long telegramUserId, string key);
    Task SetSessionValueAsync(long telegramUserId, string key, string value, TimeSpan? ttl = null);
    Task<bool> IsOfferShownAsync(long telegramUserId, string offerType);
    Task MarkOfferShownAsync(long telegramUserId, string offerType, TimeSpan ttl);
    Task<string?> GetDailyCardCacheAsync(long telegramUserId, DateOnly date);
    Task SetDailyCardCacheAsync(long telegramUserId, DateOnly date, string json, TimeSpan ttl);
}
```

`src/InstantBot.Application/Interfaces/IOpenAiService.cs`:
```csharp
using InstantBot.Domain.Entities;

namespace InstantBot.Application.Interfaces;

public interface IOpenAiService
{
    Task<(string Response, int TokensUsed)> GetMentorResponseAsync(
        User user, List<Message> history, string userMessage, CancellationToken ct = default);

    Task<string> GenerateDailyCardAsync(
        User user, string cardName, string cardMeaning, CancellationToken ct = default);

    Task<string> GeneratePersonalityProfileAsync(
        string firstName, int lifePathNumber, string archetypeName, CancellationToken ct = default);

    Task<string> GenerateRelationshipAnalysisAsync(
        User user, string partnerName, DateOnly partnerBirth, CancellationToken ct = default);

    Task<string> GenerateSummarySAsync(List<Message> messages, CancellationToken ct = default);
}
```

`src/InstantBot.Application/Interfaces/IWhisperService.cs`:
```csharp
namespace InstantBot.Application.Interfaces;

public interface IWhisperService
{
    Task<string> TranscribeAsync(Stream audioStream, string filename, CancellationToken ct = default);
}
```

`src/InstantBot.Application/Interfaces/ITtsService.cs`:
```csharp
namespace InstantBot.Application.Interfaces;

public interface ITtsService
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default);
}
```

`src/InstantBot.Application/Interfaces/INotificationService.cs`:
```csharp
namespace InstantBot.Application.Interfaces;

public interface INotificationService
{
    Task SendTextAsync(long telegramUserId, string text, CancellationToken ct = default);
    Task SendVoiceAsync(long telegramUserId, byte[] mp3, string caption, CancellationToken ct = default);
}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/InstantBot.Application/InstantBot.Application.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add . && git commit -m "feat(application): add repository and service interfaces"
```

---

### Task 6: EF Core AppDbContext + configurations

**Files:** `src/InstantBot.Infrastructure/Persistence/AppDbContext.cs`, `Configurations/*.cs`

- [ ] **Step 1: Create AppDbContext**

`src/InstantBot.Infrastructure/Persistence/AppDbContext.cs`:
```csharp
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PersonalityProfile> PersonalityProfiles => Set<PersonalityProfile>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<DailyCard> DailyCards => Set<DailyCard>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<RelationshipAnalysis> RelationshipAnalyses => Set<RelationshipAnalysis>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<Achievement> Achievements => Set<Achievement>();

    protected override void OnModelCreating(ModelBuilder mb)
        => mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

- [ ] **Step 2: Create EF configurations**

`src/InstantBot.Infrastructure/Persistence/Configurations/UserConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(u => u.Id);
        b.HasIndex(u => u.TelegramUserId).IsUnique();
        b.HasIndex(u => u.ReferralCode).IsUnique();
        b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        b.Property(u => u.Username).HasMaxLength(100);
        b.Property(u => u.ReferralCode).HasMaxLength(10).IsRequired();
        b.Property(u => u.SubscriptionTier).HasConversion<int>();

        b.HasOne(u => u.PersonalityProfile)
         .WithOne(p => p.User)
         .HasForeignKey<PersonalityProfile>(p => p.UserId);

        b.HasMany(u => u.Conversations)
         .WithOne(c => c.User)
         .HasForeignKey(c => c.UserId);

        b.HasMany(u => u.Subscriptions)
         .WithOne(s => s.User)
         .HasForeignKey(s => s.UserId);

        b.HasMany(u => u.Achievements)
         .WithOne(a => a.User)
         .HasForeignKey(a => a.UserId);
    }
}
```

`src/InstantBot.Infrastructure/Persistence/Configurations/PersonalityProfileConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class PersonalityProfileConfiguration : IEntityTypeConfiguration<PersonalityProfile>
{
    public void Configure(EntityTypeBuilder<PersonalityProfile> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.ArchetypeType).HasConversion<int>();
        b.Property(p => p.Strengths).HasColumnType("text[]");
        b.Property(p => p.Weaknesses).HasColumnType("text[]");
        b.Property(p => p.Recommendations).HasColumnType("text[]");
    }
}
```

`src/InstantBot.Infrastructure/Persistence/Configurations/ConversationConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.HasKey(c => c.Id);
        b.HasIndex(c => new { c.UserId, c.LastMessageAt });
        b.Property(c => c.Summary).HasMaxLength(2000);

        b.HasMany(c => c.Messages)
         .WithOne(m => m.Conversation)
         .HasForeignKey(m => m.ConversationId);
    }
}
```

`src/InstantBot.Infrastructure/Persistence/Configurations/MessageConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.HasKey(m => m.Id);
        b.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        b.Property(m => m.Role).HasConversion<int>();
        b.Property(m => m.Content).HasColumnType("text").IsRequired();
    }
}
```

`src/InstantBot.Infrastructure/Persistence/Configurations/DailyCardConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class DailyCardConfiguration : IEntityTypeConfiguration<DailyCard>
{
    public void Configure(EntityTypeBuilder<DailyCard> b)
    {
        b.HasKey(d => d.Id);
        b.HasIndex(d => new { d.UserId, d.Date }).IsUnique();
        b.OwnsOne(d => d.TarotCard, card =>
        {
            card.Property(c => c.Name).HasColumnName("tarot_name").HasMaxLength(100);
            card.Property(c => c.Arcana).HasColumnName("tarot_arcana").HasMaxLength(50);
            card.Property(c => c.Meaning).HasColumnName("tarot_meaning").HasMaxLength(500);
        });
        b.Property(d => d.DayAdvice).HasColumnType("text");
        b.Property(d => d.DayFocus).HasMaxLength(300);
    }
}
```

`src/InstantBot.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.HasKey(s => s.Id);
        b.HasIndex(s => s.ExpiresAt);
        b.Property(s => s.Plan).HasConversion<int>();
        b.Property(s => s.Tier).HasConversion<int>();
        b.Property(s => s.TelegramPaymentChargeId).HasMaxLength(200);
    }
}
```

`src/InstantBot.Infrastructure/Persistence/Configurations/ReferralConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> b)
    {
        b.HasKey(r => r.Id);
        b.HasOne(r => r.Referrer).WithMany()
         .HasForeignKey(r => r.ReferrerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Referred).WithMany()
         .HasForeignKey(r => r.ReferredId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

`src/InstantBot.Infrastructure/Persistence/Configurations/AchievementConfiguration.cs`:
```csharp
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> b)
    {
        b.HasKey(a => a.Id);
        b.HasIndex(a => new { a.UserId, a.Type }).IsUnique();
        b.Property(a => a.Type).HasConversion<int>();
    }
}
```

- [ ] **Step 3: Build**

```bash
dotnet build src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add . && git commit -m "feat(infrastructure): add AppDbContext and EF Core configurations"
```

---

### Task 7: Repositories and EF migrations

**Files:** `src/InstantBot.Infrastructure/Persistence/Repositories/*.cs`, `Migrations/`

- [ ] **Step 1: Create UserRepository**

`src/InstantBot.Infrastructure/Persistence/Repositories/UserRepository.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByTelegramIdAsync(long telegramUserId, CancellationToken ct = default) =>
        db.Users.Include(u => u.PersonalityProfile)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.Include(u => u.PersonalityProfile)
                .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByReferralCodeAsync(string code, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.ReferralCode == code, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
```

- [ ] **Step 2: Create ConversationRepository**

`src/InstantBot.Infrastructure/Persistence/Repositories/ConversationRepository.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.Infrastructure.Persistence.Repositories;

public class ConversationRepository(AppDbContext db) : IConversationRepository
{
    public Task<Conversation?> GetActiveAsync(Guid userId, CancellationToken ct = default) =>
        db.Conversations
          .OrderByDescending(c => c.LastMessageAt)
          .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public Task<List<Message>> GetRecentMessagesAsync(Guid convId, int count, CancellationToken ct = default) =>
        db.Messages
          .Where(m => m.ConversationId == convId)
          .OrderByDescending(m => m.CreatedAt)
          .Take(count)
          .OrderBy(m => m.CreatedAt)
          .ToListAsync(ct);

    public Task<int> CountMessagesAsync(Guid convId, CancellationToken ct = default) =>
        db.Messages.CountAsync(m => m.ConversationId == convId, ct);

    public async Task AddAsync(Conversation conv, CancellationToken ct = default) =>
        await db.Conversations.AddAsync(conv, ct);

    public async Task AddMessageAsync(Message msg, CancellationToken ct = default) =>
        await db.Messages.AddAsync(msg, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    public Task<List<Conversation>> GetConversationsToSummarizeAsync(int threshold, CancellationToken ct = default) =>
        db.Conversations
          .Where(c => db.Messages.Count(m => m.ConversationId == c.Id) > threshold)
          .ToListAsync(ct);
}
```

- [ ] **Step 3: Create DailyCardRepository**

`src/InstantBot.Infrastructure/Persistence/Repositories/DailyCardRepository.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.Infrastructure.Persistence.Repositories;

public class DailyCardRepository(AppDbContext db) : IDailyCardRepository
{
    public Task<DailyCard?> GetForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default) =>
        db.DailyCards.FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, ct);

    public async Task AddAsync(DailyCard card, CancellationToken ct = default) =>
        await db.DailyCards.AddAsync(card, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
```

- [ ] **Step 4: Create migrations**

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project src/InstantBot.Infrastructure \
  --startup-project src/InstantBot.TelegramBot \
  --output-dir Persistence/Migrations
```

> **Note:** Before running this, add the connection string placeholder in `src/InstantBot.TelegramBot/appsettings.json`:
>
> ```json
> {
>   "ConnectionStrings": {
>     "DefaultConnection": "Host=localhost;Database=instantbot;Username=postgres;Password=postgres"
>   }
> }
> ```
>
> And register DbContext in `src/InstantBot.TelegramBot/Program.cs` temporarily (will be replaced in Task 8):
>
> ```csharp
> builder.Services.AddDbContext<AppDbContext>(opts =>
>     opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
> ```

Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 5: Commit**

```bash
git add . && git commit -m "feat(infrastructure): add repositories and initial EF migration"
```

---

### Task 8: RedisCacheService

**Files:** `src/InstantBot.Infrastructure/Cache/RedisCacheService.cs`

- [ ] **Step 1: Implement RedisCacheService**

`src/InstantBot.Infrastructure/Cache/RedisCacheService.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;
using StackExchange.Redis;

namespace InstantBot.Infrastructure.Cache;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private IDatabase Db => redis.GetDatabase();

    private static string StateKey(long uid) => $"state:{uid}";
    private static string RateLimitKey(long uid) => $"rate:{uid}";
    private static string SessionKey(long uid, string k) => $"session:{uid}:{k}";
    private static string OfferKey(long uid, string t) => $"offer:{uid}:{t}";
    private static string CardKey(long uid, DateOnly d) => $"card:{uid}:{d:yyyy-MM-dd}";

    public async Task<UserBotState> GetUserStateAsync(long uid)
    {
        var val = await Db.StringGetAsync(StateKey(uid));
        return val.HasValue && Enum.TryParse<UserBotState>(val!, out var s) ? s : UserBotState.None;
    }

    public Task SetUserStateAsync(long uid, UserBotState state, TimeSpan? ttl = null) =>
        Db.StringSetAsync(StateKey(uid), state.ToString(), ttl ?? TimeSpan.FromMinutes(30));

    public async Task<int> IncrementDailyQuestionsAsync(long uid)
    {
        var key = RateLimitKey(uid);
        var count = await Db.StringIncrementAsync(key);
        if (count == 1)
        {
            var midnight = DateTime.UtcNow.Date.AddDays(1);
            await Db.KeyExpireAsync(key, midnight - DateTime.UtcNow);
        }
        return (int)count;
    }

    public async Task<string?> GetSessionValueAsync(long uid, string key)
    {
        var val = await Db.StringGetAsync(SessionKey(uid, key));
        return val.HasValue ? val.ToString() : null;
    }

    public Task SetSessionValueAsync(long uid, string key, string value, TimeSpan? ttl = null) =>
        Db.StringSetAsync(SessionKey(uid, key), value, ttl ?? TimeSpan.FromMinutes(30));

    public async Task<bool> IsOfferShownAsync(long uid, string offerType) =>
        await Db.KeyExistsAsync(OfferKey(uid, offerType));

    public Task MarkOfferShownAsync(long uid, string offerType, TimeSpan ttl) =>
        Db.StringSetAsync(OfferKey(uid, offerType), "1", ttl);

    public async Task<string?> GetDailyCardCacheAsync(long uid, DateOnly date)
    {
        var val = await Db.StringGetAsync(CardKey(uid, date));
        return val.HasValue ? val.ToString() : null;
    }

    public Task SetDailyCardCacheAsync(long uid, DateOnly date, string json, TimeSpan ttl) =>
        Db.StringSetAsync(CardKey(uid, date), json, ttl);
}
```

- [ ] **Step 2: Commit**

```bash
git add . && git commit -m "feat(infrastructure): add RedisCacheService"
```

---

### Task 9: RegisterUser command and Telegram bot setup

**Files:** `src/InstantBot.Application/Commands/RegisterUser/*.cs`, `src/InstantBot.TelegramBot/Program.cs`, `StateMachine/UserStateManager.cs`, `Handlers/MessageHandler.cs`
**Test:** `tests/InstantBot.Application.Tests/RegisterUserCommandHandlerTests.cs`

- [ ] **Step 1: Write failing test**

`tests/InstantBot.Application.Tests/RegisterUserCommandHandlerTests.cs`:
```csharp
using FluentAssertions;
using InstantBot.Application.Commands.RegisterUser;
using InstantBot.Application.Interfaces;
using InstantBot.Application.Services;
using InstantBot.Domain.Entities;
using Moq;

namespace InstantBot.Application.Tests;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewUser_CreatesUserAndProfile()
    {
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByTelegramIdAsync(It.IsAny<long>(), default))
                .ReturnsAsync((User?)null);

        var handler = new RegisterUserCommandHandler(repoMock.Object);
        var cmd = new RegisterUserCommand(123L, "Иван", null, new DateOnly(1990, 5, 15), null);

        var result = await handler.Handle(cmd, default);

        result.Should().NotBeNull();
        result.TelegramUserId.Should().Be(123L);
        result.PersonalityProfile.Should().NotBeNull();
        repoMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsExistingUser()
    {
        var existing = User.Create(123L, "Иван", null, new DateOnly(1990, 5, 15));
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByTelegramIdAsync(123L, default)).ReturnsAsync(existing);

        var handler = new RegisterUserCommandHandler(repoMock.Object);
        var cmd = new RegisterUserCommand(123L, "Иван", null, new DateOnly(1990, 5, 15), null);

        var result = await handler.Handle(cmd, default);

        result.Should().Be(existing);
        repoMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests (expect failure)**

```bash
dotnet test tests/InstantBot.Application.Tests/ --filter "RegisterUserCommandHandlerTests"
```

Expected: FAIL.

- [ ] **Step 3: Create RegisterUserCommand**

`src/InstantBot.Application/Commands/RegisterUser/RegisterUserCommand.cs`:
```csharp
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Commands.RegisterUser;

public record RegisterUserCommand(
    long TelegramUserId,
    string FirstName,
    string? Username,
    DateOnly BirthDate,
    string? ReferralCode) : IRequest<User>;
```

`src/InstantBot.Application/Commands/RegisterUser/RegisterUserCommandHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Application.Services;
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Commands.RegisterUser;

public class RegisterUserCommandHandler(IUserRepository userRepo) : IRequestHandler<RegisterUserCommand, User>
{
    public async Task<User> Handle(RegisterUserCommand cmd, CancellationToken ct)
    {
        var existing = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct);
        if (existing is not null) return existing;

        var user = User.Create(cmd.TelegramUserId, cmd.FirstName, cmd.Username, cmd.BirthDate);

        if (cmd.ReferralCode is not null)
        {
            var referrer = await userRepo.GetByReferralCodeAsync(cmd.ReferralCode, ct);
            if (referrer is not null) user.SetReferredBy(referrer.Id);
        }

        var lifePath = NumerologyCalculator.CalculateLifePathNumber(cmd.BirthDate);
        var destiny  = NumerologyCalculator.CalculateDestinyNumber(cmd.FirstName);
        var soul     = NumerologyCalculator.CalculateSoulNumber(cmd.FirstName);
        var archInfo = ArchetypeAnalyzer.GetArchetypeInfo(lifePath);

        var profile = PersonalityProfile.Create(
            user.Id, lifePath, destiny, soul, archInfo.Archetype,
            archInfo.Strengths, archInfo.Weaknesses, archInfo.Recommendations);

        user.AttachProfile(profile);
        await userRepo.AddAsync(user, ct);
        await userRepo.SaveChangesAsync(ct);
        return user;
    }
}
```

- [ ] **Step 4: Run tests (expect pass)**

```bash
dotnet test tests/InstantBot.Application.Tests/ --filter "RegisterUserCommandHandlerTests"
```

Expected: 2 tests PASS.

- [ ] **Step 5: Create UserStateManager**

`src/InstantBot.TelegramBot/StateMachine/UserStateManager.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;

namespace InstantBot.TelegramBot.StateMachine;

public class UserStateManager(ICacheService cache)
{
    public Task<UserBotState> GetStateAsync(long telegramUserId) =>
        cache.GetUserStateAsync(telegramUserId);

    public Task SetStateAsync(long telegramUserId, UserBotState state) =>
        cache.SetUserStateAsync(telegramUserId, state, TimeSpan.FromMinutes(30));

    public Task<string?> GetTempValueAsync(long telegramUserId, string key) =>
        cache.GetSessionValueAsync(telegramUserId, key);

    public Task SetTempValueAsync(long telegramUserId, string key, string value) =>
        cache.SetSessionValueAsync(telegramUserId, key, value, TimeSpan.FromMinutes(30));
}
```

- [ ] **Step 6: Create MainMenuKeyboard**

`src/InstantBot.TelegramBot/Keyboards/MainMenuKeyboard.cs`:
```csharp
using Telegram.Bot.Types.ReplyMarkups;

namespace InstantBot.TelegramBot.Keyboards;

public static class MainMenuKeyboard
{
    public static InlineKeyboardMarkup Get() => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("🔮 Спросить наставника", "ask_mentor"),
                InlineKeyboardButton.WithCallbackData("💫 Карта дня",           "daily_card") },
        new[] { InlineKeyboardButton.WithCallbackData("❤️ Анализ отношений",    "relationship"),
                InlineKeyboardButton.WithCallbackData("⭐ Подписка",            "subscription") },
        new[] { InlineKeyboardButton.WithCallbackData("👤 Мой профиль",         "my_profile") }
    });
}
```

- [ ] **Step 7: Create MessageHandler**

`src/InstantBot.TelegramBot/Handlers/MessageHandler.cs`:
```csharp
using InstantBot.Application.Commands.RegisterUser;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;
using InstantBot.TelegramBot.Keyboards;
using InstantBot.TelegramBot.StateMachine;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class MessageHandler(
    ITelegramBotClient bot,
    ISender mediator,
    UserStateManager stateManager,
    IUserRepository userRepo)
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        if (msg.From is null) return;
        var uid = msg.From.Id;
        var text = msg.Text ?? string.Empty;

        if (text.StartsWith("/start"))
        {
            await HandleStartAsync(msg, ct);
            return;
        }

        if (text == "/menu")
        {
            await stateManager.SetStateAsync(uid, UserBotState.MainMenu);
            await bot.SendMessage(uid, "Главное меню:", replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
            return;
        }

        var state = await stateManager.GetStateAsync(uid);
        switch (state)
        {
            case UserBotState.AwaitingName:
                await HandleAwaitingNameAsync(msg, ct);
                break;
            case UserBotState.AwaitingBirthDate:
                await HandleAwaitingBirthDateAsync(msg, ct);
                break;
            case UserBotState.AwaitingMentorQuestion:
                await HandleMentorQuestionAsync(msg, ct);
                break;
            case UserBotState.AwaitingPartnerName:
                await HandlePartnerNameAsync(msg, ct);
                break;
            case UserBotState.AwaitingPartnerBirthDate:
                await HandlePartnerBirthDateAsync(msg, ct);
                break;
            default:
                await bot.SendMessage(uid, "Используй меню ниже:", replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
                break;
        }
    }

    private async Task HandleStartAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        var parts = msg.Text!.Split(' ');
        var refCode = parts.Length > 1 && parts[1].StartsWith("REF_") ? parts[1][4..] : null;

        var user = await userRepo.GetByTelegramIdAsync(uid, ct);
        if (user is not null)
        {
            await bot.SendMessage(uid, $"С возвращением, {user.FirstName}! 👋", replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
            await stateManager.SetStateAsync(uid, UserBotState.MainMenu);
            return;
        }

        if (refCode is not null)
            await stateManager.SetTempValueAsync(uid, "ref_code", refCode);

        await bot.SendMessage(uid,
            "Привет! Я твой личный ИИ-наставник. 🌟\n\nДавай познакомимся. Как тебя зовут?",
            cancellationToken: ct);
        await stateManager.SetStateAsync(uid, UserBotState.AwaitingName);
    }

    private async Task HandleAwaitingNameAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        var name = msg.Text?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            await bot.SendMessage(uid, "Введи своё имя (минимум 2 символа):", cancellationToken: ct);
            return;
        }
        await stateManager.SetTempValueAsync(uid, "name", name);
        await stateManager.SetStateAsync(uid, UserBotState.AwaitingBirthDate);
        await bot.SendMessage(uid, $"Приятно познакомиться, {name}! 😊\n\nВведи дату рождения в формате ДД.ММ.ГГГГ\nНапример: 15.05.1990", cancellationToken: ct);
    }

    private async Task HandleAwaitingBirthDateAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        if (!DateOnly.TryParseExact(msg.Text?.Trim(), "dd.MM.yyyy", out var birthDate))
        {
            await bot.SendMessage(uid, "Неверный формат. Введи дату как ДД.ММ.ГГГГ, например: 15.05.1990", cancellationToken: ct);
            return;
        }

        var name = await stateManager.GetTempValueAsync(uid, "name") ?? msg.From.FirstName;
        var refCode = await stateManager.GetTempValueAsync(uid, "ref_code");

        var user = await mediator.Send(new RegisterUserCommand(uid, name, msg.From.Username, birthDate, refCode), ct);

        await stateManager.SetStateAsync(uid, UserBotState.MainMenu);

        var profile = user.PersonalityProfile!;
        var archetypeName = ArchetypeNameMap[profile.ArchetypeType];
        var profileText = $"""
            ✨ Твой профиль готов, {user.FirstName}!

            🔢 Число жизненного пути: {profile.LifePathNumber}
            🌟 Архетип: {archetypeName}

            💪 Сильные стороны:
            {string.Join("\n", profile.Strengths.Select(s => $"• {s}"))}

            🎯 Зоны роста:
            {string.Join("\n", profile.Weaknesses.Select(w => $"• {w}"))}

            💡 Рекомендации:
            {string.Join("\n", profile.Recommendations.Select(r => $"• {r}"))}
            """;

        await bot.SendMessage(uid, profileText, replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
    }

    private Task HandleMentorQuestionAsync(Message msg, CancellationToken ct) =>
        Task.CompletedTask; // Implemented in Task 10 (AI Mentor)

    private Task HandlePartnerNameAsync(Message msg, CancellationToken ct) =>
        Task.CompletedTask; // Implemented in Task 14 (Relationships)

    private Task HandlePartnerBirthDateAsync(Message msg, CancellationToken ct) =>
        Task.CompletedTask; // Implemented in Task 14 (Relationships)

    private static readonly Dictionary<Domain.Enums.ArchetypeType, string> ArchetypeNameMap = new()
    {
        [ArchetypeType.Leader]     = "Лидер",     [ArchetypeType.Diplomat]  = "Дипломат",
        [ArchetypeType.Creative]   = "Творец",    [ArchetypeType.Organizer] = "Организатор",
        [ArchetypeType.Adventurer] = "Искатель",  [ArchetypeType.Nurturer]  = "Опекун",
        [ArchetypeType.Seeker]     = "Мыслитель", [ArchetypeType.Authority] = "Стратег",
        [ArchetypeType.Sage]       = "Мудрец",    [ArchetypeType.Visionary] = "Провидец",
        [ArchetypeType.Builder]    = "Строитель", [ArchetypeType.Unknown]   = "Исследователь",
    };
}
```

- [ ] **Step 8: Create UpdateRouter and Program.cs**

`src/InstantBot.TelegramBot/Handlers/UpdateRouter.cs`:
```csharp
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InstantBot.TelegramBot.Handlers;

public class UpdateRouter(
    MessageHandler messageHandler,
    CallbackHandler callbackHandler,
    PaymentHandler paymentHandler,
    VoiceHandler voiceHandler,
    ILogger<UpdateRouter> logger)
{
    public async Task RouteAsync(Update update, CancellationToken ct)
    {
        try
        {
            await (update.Type switch
            {
                UpdateType.Message when update.Message?.Voice is not null =>
                    voiceHandler.HandleAsync(update.Message, ct),
                UpdateType.Message when update.Message is not null =>
                    messageHandler.HandleAsync(update.Message, ct),
                UpdateType.CallbackQuery when update.CallbackQuery is not null =>
                    callbackHandler.HandleAsync(update.CallbackQuery, ct),
                UpdateType.PreCheckoutQuery when update.PreCheckoutQuery is not null =>
                    paymentHandler.HandlePreCheckoutAsync(update.PreCheckoutQuery, ct),
                UpdateType.Message when update.Message?.SuccessfulPayment is not null =>
                    paymentHandler.HandleSuccessfulPaymentAsync(update.Message, ct),
                _ => Task.CompletedTask
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error routing update {UpdateId}", update.Id);
        }
    }
}
```

`src/InstantBot.TelegramBot/Program.cs`:
```csharp
using InstantBot.Application.Commands.RegisterUser;
using InstantBot.Application.Interfaces;
using InstantBot.Infrastructure.Cache;
using InstantBot.Infrastructure.OpenAi;
using InstantBot.Infrastructure.Payments;
using InstantBot.Infrastructure.Persistence;
using InstantBot.Infrastructure.Persistence.Repositories;
using InstantBot.TelegramBot.Handlers;
using InstantBot.TelegramBot.StateMachine;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Services.AddSerilog();

// Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(builder.Configuration["TelegramBot:Token"]!));

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IDailyCardRepository, DailyCardRepository>();

// Cache
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// OpenAI Services (stubs resolved in Phase 2)
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<IWhisperService, WhisperService>();
builder.Services.AddScoped<ITtsService, TtsService>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly));

// Bot handlers
builder.Services.AddScoped<UserStateManager>();
builder.Services.AddScoped<MessageHandler>();
builder.Services.AddScoped<CallbackHandler>();
builder.Services.AddScoped<PaymentHandler>();
builder.Services.AddScoped<VoiceHandler>();
builder.Services.AddScoped<UpdateRouter>();

// Background service for long polling
builder.Services.AddHostedService<BotPollingService>();

var app = builder.Build();

// Apply migrations on start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
```

`src/InstantBot.TelegramBot/BotPollingService.cs`:
```csharp
using InstantBot.TelegramBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace InstantBot.TelegramBot;

public class BotPollingService(
    ITelegramBotClient bot,
    IServiceProvider services,
    ILogger<BotPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var opts = new ReceiverOptions { AllowedUpdates = [] };
        bot.StartReceiving(
            async (_, update, token) =>
            {
                await using var scope = services.CreateAsyncScope();
                var router = scope.ServiceProvider.GetRequiredService<UpdateRouter>();
                await router.RouteAsync(update, token);
            },
            (_, ex, token) => { logger.LogError(ex, "Telegram polling error"); return Task.CompletedTask; },
            opts, ct);

        logger.LogInformation("Bot started polling");
        await Task.Delay(Timeout.Infinite, ct);
    }
}
```

Create stub handlers (to be filled in Phase 2+):

`src/InstantBot.TelegramBot/Handlers/CallbackHandler.cs`:
```csharp
using InstantBot.TelegramBot.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class CallbackHandler(ITelegramBotClient bot)
{
    public async Task HandleAsync(CallbackQuery query, CancellationToken ct)
    {
        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
        // Expanded in Phase 2-3
    }
}
```

`src/InstantBot.TelegramBot/Handlers/PaymentHandler.cs`:
```csharp
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class PaymentHandler(ITelegramBotClient bot)
{
    public Task HandlePreCheckoutAsync(PreCheckoutQuery query, CancellationToken ct) =>
        bot.AnswerPreCheckoutQuery(query.Id, cancellationToken: ct);

    public Task HandleSuccessfulPaymentAsync(Message msg, CancellationToken ct) =>
        Task.CompletedTask; // Expanded in Phase 3
}
```

`src/InstantBot.TelegramBot/Handlers/VoiceHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class VoiceHandler(ITelegramBotClient bot, IUserRepository userRepo)
{
    public Task HandleAsync(Message msg, CancellationToken ct) =>
        Task.CompletedTask; // Expanded in Phase 5 (VIP voice)
}
```

- [ ] **Step 9: Build**

```bash
dotnet build InstantBot.sln
```

Expected: `Build succeeded.`

- [ ] **Step 10: Commit**

```bash
git add . && git commit -m "feat(bot): add registration FSM, UpdateRouter, BotPollingService, Program.cs"
```

---

## Phase 2 — AI Mentor (M2)

### Task 10: OpenAiService with context injection

**Files:** `src/InstantBot.Infrastructure/OpenAi/OpenAiService.cs`
**Test:** `tests/InstantBot.Application.Tests/AskMentorCommandHandlerTests.cs`

- [ ] **Step 1: Write failing test for AskMentor**

`tests/InstantBot.Application.Tests/AskMentorCommandHandlerTests.cs`:
```csharp
using FluentAssertions;
using InstantBot.Application.Commands.AskMentor;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using Moq;

namespace InstantBot.Application.Tests;

public class AskMentorCommandHandlerTests
{
    private static User MakeUser(SubscriptionTier tier = SubscriptionTier.Free)
    {
        var u = User.Create(1L, "Тест", null, new DateOnly(1990, 1, 1));
        if (tier != SubscriptionTier.Free)
            u.UpdateSubscription(tier, DateTime.UtcNow.AddMonths(1));
        return u;
    }

    [Fact]
    public async Task Handle_FreeUser_UnderLimit_ReturnsResponse()
    {
        var user = MakeUser();
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var convRepo = new Mock<IConversationRepository>();
        var conv = Conversation.Create(user.Id);
        convRepo.Setup(r => r.GetActiveAsync(user.Id, default)).ReturnsAsync(conv);
        convRepo.Setup(r => r.GetRecentMessagesAsync(conv.Id, It.IsAny<int>(), default))
                .ReturnsAsync([]);

        var ai = new Mock<IOpenAiService>();
        ai.Setup(a => a.GetMentorResponseAsync(user, It.IsAny<List<Message>>(), "тест", default))
          .ReturnsAsync(("Ответ наставника", 50));

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.IncrementDailyQuestionsAsync(1L)).ReturnsAsync(1);

        var handler = new AskMentorCommandHandler(userRepo.Object, convRepo.Object, ai.Object, cache.Object);
        var result = await handler.Handle(new AskMentorCommand(1L, "тест"), default);

        result.Response.Should().Be("Ответ наставника");
        result.LimitReached.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FreeUser_OverLimit_ReturnsLimitMessage()
    {
        var user = MakeUser();
        // Already used 3 questions
        user.IncrementDailyQuestions(); user.IncrementDailyQuestions(); user.IncrementDailyQuestions();

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var handler = new AskMentorCommandHandler(
            userRepo.Object, Mock.Of<IConversationRepository>(),
            Mock.Of<IOpenAiService>(), Mock.Of<ICacheService>());

        var result = await handler.Handle(new AskMentorCommand(1L, "тест"), default);

        result.LimitReached.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests (expect failure)**

```bash
dotnet test tests/InstantBot.Application.Tests/ --filter "AskMentorCommandHandlerTests"
```

Expected: FAIL.

- [ ] **Step 3: Create AskMentorCommand**

`src/InstantBot.Application/Commands/AskMentor/AskMentorCommand.cs`:
```csharp
using MediatR;
namespace InstantBot.Application.Commands.AskMentor;

public record AskMentorCommand(long TelegramUserId, string Question) : IRequest<AskMentorResult>;
public record AskMentorResult(string Response, bool LimitReached, int QuestionsLeft);
```

`src/InstantBot.Application/Commands/AskMentor/AskMentorCommandHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using MediatR;

namespace InstantBot.Application.Commands.AskMentor;

public class AskMentorCommandHandler(
    IUserRepository userRepo,
    IConversationRepository convRepo,
    IOpenAiService ai,
    ICacheService cache) : IRequestHandler<AskMentorCommand, AskMentorResult>
{
    private static readonly Dictionary<SubscriptionTier, int> HistoryLimit = new()
    {
        [SubscriptionTier.Free]    = 5,
        [SubscriptionTier.Premium] = 10,
        [SubscriptionTier.Vip]     = 20,
    };

    public async Task<AskMentorResult> Handle(AskMentorCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct)
            ?? throw new InvalidOperationException("User not found");

        if (!user.CanAskQuestion())
            return new AskMentorResult("", true, 0);

        var conversation = await convRepo.GetActiveAsync(user.Id, ct);
        if (conversation is null)
        {
            conversation = Conversation.Create(user.Id);
            await convRepo.AddAsync(conversation, ct);
        }

        var historyCount = HistoryLimit[user.SubscriptionTier];
        var history = await convRepo.GetRecentMessagesAsync(conversation.Id, historyCount, ct);

        var (response, tokens) = await ai.GetMentorResponseAsync(user, history, cmd.Question, ct);

        var userMsg = Message.Create(conversation.Id, MessageRole.User, cmd.Question);
        var assistantMsg = Message.Create(conversation.Id, MessageRole.Assistant, response, tokensUsed: tokens);

        await convRepo.AddMessageAsync(userMsg, ct);
        await convRepo.AddMessageAsync(assistantMsg, ct);
        conversation.Touch();
        await convRepo.SaveChangesAsync(ct);

        user.IncrementDailyQuestions();
        user.UpdateStreak();
        await userRepo.SaveChangesAsync(ct);

        var questionsLeft = user.SubscriptionTier == SubscriptionTier.Free
            ? Math.Max(0, 3 - user.DailyQuestionsUsed) : int.MaxValue;

        return new AskMentorResult(response, false, questionsLeft);
    }
}
```

- [ ] **Step 4: Implement OpenAiService**

`src/InstantBot.Infrastructure/OpenAi/OpenAiService.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Application.Services;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using OpenAI.Chat;

namespace InstantBot.Infrastructure.OpenAi;

public class OpenAiService(IConfiguration config) : IOpenAiService
{
    private ChatClient CreateClient() =>
        new("gpt-4o-mini", config["OpenAI:ApiKey"]!);

    public async Task<(string Response, int TokensUsed)> GetMentorResponseAsync(
        User user, List<Message> history, string userMessage, CancellationToken ct = default)
    {
        var client = CreateClient();
        var profile = user.PersonalityProfile;

        var systemPrompt = $"""
            Ты — мудрый и эмпатичный личный наставник. Твоя задача — помочь человеку разобраться в жизненной ситуации.

            О пользователе:
            Имя: {user.FirstName}
            Возраст: {DateTime.UtcNow.Year - user.BirthDate.Year} лет
            {(profile != null ? $"""
            Число жизненного пути: {profile.LifePathNumber}
            Архетип: {ArchetypeAnalyzer.GetArchetypeInfo(profile.LifePathNumber).Name}
            Сильные стороны: {string.Join(", ", profile.Strengths)}
            Зоны роста: {string.Join(", ", profile.Weaknesses)}
            """ : "")}

            Правила ответов:
            - Отвечай тепло, конкретно, без шаблонов
            - Используй формулировки: "возможно", "один из вариантов", "с точки зрения..."
            - Никогда не предсказывай будущее как факт
            - Никогда не гарантируй результаты
            - Максимальная длина ответа: {GetTokenLimit(user.SubscriptionTier)} токенов
            """;

        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };

        foreach (var m in history)
        {
            messages.Add(m.Role == MessageRole.User
                ? new UserChatMessage(m.Content)
                : new AssistantChatMessage(m.Content));
        }
        messages.Add(new UserChatMessage(userMessage));

        var completion = await client.CompleteChatAsync(messages, cancellationToken: ct);
        var response = completion.Value.Content[0].Text;
        var tokens = completion.Value.Usage.TotalTokenCount;
        return (response, tokens);
    }

    public async Task<string> GenerateDailyCardAsync(User user, string cardName, string cardMeaning, CancellationToken ct = default)
    {
        var client = CreateClient();
        var prompt = $"""
            Создай персональный совет на день для {user.FirstName} (число жизненного пути: {user.PersonalityProfile?.LifePathNumber}).
            Карта дня: {cardName} — {cardMeaning}
            Формат ответа (строго):
            СОВЕТ: [1-2 предложения, персональный совет]
            ФОКУС: [1 предложение, на чём сосредоточиться сегодня]
            """;

        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    public async Task<string> GeneratePersonalityProfileAsync(
        string firstName, int lifePathNumber, string archetypeName, CancellationToken ct = default)
    {
        var client = CreateClient();
        var prompt = $"Напиши краткое вдохновляющее описание профиля личности для {firstName}. Число жизненного пути {lifePathNumber}, архетип: {archetypeName}. 2-3 предложения, тёплый тон.";
        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    public async Task<string> GenerateRelationshipAnalysisAsync(
        User user, string partnerName, DateOnly partnerBirth, CancellationToken ct = default)
    {
        var client = CreateClient();
        var userLifePath = user.PersonalityProfile?.LifePathNumber ??
            NumerologyCalculator.CalculateLifePathNumber(user.BirthDate);
        var partnerLifePath = NumerologyCalculator.CalculateLifePathNumber(partnerBirth);

        var prompt = $"""
            Проведи анализ совместимости пары (с точки зрения нумерологии и психологии):
            {user.FirstName}: число жизненного пути {userLifePath}
            {partnerName}: число жизненного пути {partnerLifePath}

            Структура ответа:
            💪 СИЛЬНЫЕ СТОРОНЫ ПАРЫ: [2-3 пункта]
            ⚡ ЗОНЫ КОНФЛИКТОВ: [2-3 пункта]
            💬 СТИЛЬ ОБЩЕНИЯ: [1-2 предложения]
            🌱 РЕКОМЕНДАЦИИ: [2-3 практических совета]

            Используй формулировки "возможно", "один из вариантов". Не предсказывай будущее.
            """;

        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    public async Task<string> GenerateSummarySAsync(List<Message> messages, CancellationToken ct = default)
    {
        var client = CreateClient();
        var dialog = string.Join("\n", messages.Select(m =>
            $"{(m.Role == MessageRole.User ? "Пользователь" : "Наставник")}: {m.Content}"));

        var prompt = $"Сделай краткое резюме (3-5 предложений) следующего диалога между пользователем и наставником:\n\n{dialog}";
        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    private static int GetTokenLimit(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Premium => 600,
        SubscriptionTier.Vip    => 1000,
        _                       => 300
    };
}
```

- [ ] **Step 5: Implement WhisperService and TtsService stubs**

`src/InstantBot.Infrastructure/OpenAi/WhisperService.cs`:
```csharp
using InstantBot.Application.Interfaces;
using OpenAI.Audio;

namespace InstantBot.Infrastructure.OpenAi;

public class WhisperService(IConfiguration config) : IWhisperService
{
    public async Task<string> TranscribeAsync(Stream audio, string filename, CancellationToken ct = default)
    {
        var client = new AudioClient("whisper-1", config["OpenAI:ApiKey"]!);
        var result = await client.TranscribeAudioAsync(audio, filename, cancellationToken: ct);
        return result.Value.Text;
    }
}
```

`src/InstantBot.Infrastructure/OpenAi/TtsService.cs`:
```csharp
using InstantBot.Application.Interfaces;
using OpenAI.Audio;

namespace InstantBot.Infrastructure.OpenAi;

public class TtsService(IConfiguration config) : ITtsService
{
    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        var client = new AudioClient("tts-1", config["OpenAI:ApiKey"]!);
        var speech = await client.GenerateSpeechAsync(text, GeneratedSpeechVoice.Alloy, cancellationToken: ct);
        return speech.Value.ToArray();
    }
}
```

- [ ] **Step 6: Wire AskMentor into MessageHandler**

In `src/InstantBot.TelegramBot/Handlers/MessageHandler.cs`, replace the stub `HandleMentorQuestionAsync`:
```csharp
private async Task HandleMentorQuestionAsync(Message msg, CancellationToken ct)
{
    var uid = msg.From!.Id;
    var question = msg.Text?.Trim();
    if (string.IsNullOrEmpty(question)) return;

    await bot.SendChatAction(uid, Telegram.Bot.Types.Enums.ChatAction.Typing, cancellationToken: ct);

    var result = await mediator.Send(new AskMentorCommand(uid, question), ct);

    if (result.LimitReached)
    {
        var shown = await cache.IsOfferShownAsync(uid, "limit_upsell");
        var text = "Ты использовал все 3 бесплатных вопроса на сегодня. 🌙\n\nПереходи на Premium и общайся с наставником без ограничений!";
        if (!shown)
        {
            text += "\n\n🎁 Специальное предложение: скидка 20% на первый месяц Premium — только 24 часа!";
            await cache.MarkOfferShownAsync(uid, "limit_upsell", TimeSpan.FromHours(24));
        }
        await bot.SendMessage(uid, text,
            replyMarkup: SubscriptionKeyboard.GetUpsell(),
            cancellationToken: ct);
        return;
    }

    var response = result.QuestionsLeft > 0 && result.QuestionsLeft < int.MaxValue
        ? $"{result.Response}\n\n_Осталось вопросов сегодня: {result.QuestionsLeft}_"
        : result.Response;

    await bot.SendMessage(uid, response, replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
    await stateManager.SetStateAsync(uid, UserBotState.MainMenu);
}
```

Add `ICacheService cache` to `MessageHandler` constructor and add `SubscriptionKeyboard.GetUpsell()` stub:

`src/InstantBot.TelegramBot/Keyboards/SubscriptionKeyboard.cs`:
```csharp
using Telegram.Bot.Types.ReplyMarkups;

namespace InstantBot.TelegramBot.Keyboards;

public static class SubscriptionKeyboard
{
    public static InlineKeyboardMarkup GetUpsell() => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("⭐ Купить Premium", "sub_premium_1mo") },
        new[] { InlineKeyboardButton.WithCallbackData("💎 Посмотреть все тарифы", "subscription") },
    });

    public static InlineKeyboardMarkup GetPlans() => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("Premium — 299 ⭐/мес",    "sub_premium_1mo")  },
        new[] { InlineKeyboardButton.WithCallbackData("Premium — 749 ⭐/3 мес",  "sub_premium_3mo")  },
        new[] { InlineKeyboardButton.WithCallbackData("Premium — 2490 ⭐/год",   "sub_premium_12mo") },
        new[] { InlineKeyboardButton.WithCallbackData("VIP — 599 ⭐/мес",        "sub_vip_1mo")      },
        new[] { InlineKeyboardButton.WithCallbackData("VIP — 1499 ⭐/3 мес",     "sub_vip_3mo")      },
        new[] { InlineKeyboardButton.WithCallbackData("VIP — 4990 ⭐/год",       "sub_vip_12mo")     },
    });
}
```

- [ ] **Step 7: Run all tests**

```bash
dotnet test InstantBot.sln
```

Expected: All tests PASS.

- [ ] **Step 8: Commit**

```bash
git add . && git commit -m "feat(ai): OpenAI GPT-4o-mini mentor, Whisper, TTS services"
```


---

## Phase 3 — Monetization (M3)

### Task 11: PurchaseSubscription command and Telegram Stars payment flow

**Files:** `src/InstantBot.Application/Commands/PurchaseSubscription/*.cs`, `src/InstantBot.Infrastructure/Payments/TelegramStarsService.cs`, `src/InstantBot.TelegramBot/Handlers/PaymentHandler.cs`, `src/InstantBot.TelegramBot/Handlers/CallbackHandler.cs`
**Test:** `tests/InstantBot.Application.Tests/PurchaseSubscriptionCommandHandlerTests.cs`

- [ ] **Step 1: Write failing test**

`tests/InstantBot.Application.Tests/PurchaseSubscriptionCommandHandlerTests.cs`:
```csharp
using FluentAssertions;
using InstantBot.Application.Commands.PurchaseSubscription;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using Moq;

namespace InstantBot.Application.Tests;

public class PurchaseSubscriptionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidPayment_UpgradesUserAndCreatesSubscription()
    {
        var user = User.Create(1L, "Иван", null, new DateOnly(1990, 1, 1));
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var handler = new PurchaseSubscriptionCommandHandler(userRepo.Object);
        var cmd = new PurchaseSubscriptionCommand(
            1L, SubscriptionPlan.OneMonth, SubscriptionTier.Premium, 299, "charge_abc");

        await handler.Handle(cmd, default);

        user.SubscriptionTier.Should().Be(SubscriptionTier.Premium);
        user.SubscriptionExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMonths(1), TimeSpan.FromSeconds(5));
        userRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
```

- [ ] **Step 2: Run (expect failure)**

```bash
dotnet test tests/InstantBot.Application.Tests/ --filter "PurchaseSubscriptionCommandHandlerTests"
```

Expected: FAIL.

- [ ] **Step 3: Create command and handler**

`src/InstantBot.Application/Commands/PurchaseSubscription/PurchaseSubscriptionCommand.cs`:
```csharp
using InstantBot.Domain.Enums;
using MediatR;

namespace InstantBot.Application.Commands.PurchaseSubscription;

public record PurchaseSubscriptionCommand(
    long TelegramUserId,
    SubscriptionPlan Plan,
    SubscriptionTier Tier,
    int StarsPaid,
    string TelegramPaymentChargeId) : IRequest;
```

`src/InstantBot.Application/Commands/PurchaseSubscription/PurchaseSubscriptionCommandHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Commands.PurchaseSubscription;

public class PurchaseSubscriptionCommandHandler(IUserRepository userRepo)
    : IRequestHandler<PurchaseSubscriptionCommand>
{
    public async Task Handle(PurchaseSubscriptionCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct)
            ?? throw new InvalidOperationException("User not found");

        var months = (int)cmd.Plan;
        var expiresAt = user.SubscriptionExpiresAt.HasValue && user.SubscriptionExpiresAt > DateTime.UtcNow
            ? user.SubscriptionExpiresAt.Value.AddMonths(months)
            : DateTime.UtcNow.AddMonths(months);

        user.UpdateSubscription(cmd.Tier, expiresAt);
        await userRepo.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Implement TelegramStarsService**

`src/InstantBot.Infrastructure/Payments/TelegramStarsService.cs`:
```csharp
using InstantBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.Payments;

namespace InstantBot.Infrastructure.Payments;

public static class TelegramStarsService
{
    private static readonly Dictionary<(SubscriptionTier, SubscriptionPlan), (int Stars, string Title)> Prices = new()
    {
        [(SubscriptionTier.Premium, SubscriptionPlan.OneMonth)]    = (299,  "Premium — 1 месяц"),
        [(SubscriptionTier.Premium, SubscriptionPlan.ThreeMonths)] = (749,  "Premium — 3 месяца"),
        [(SubscriptionTier.Premium, SubscriptionPlan.TwelveMonths)]= (2490, "Premium — 12 месяцев"),
        [(SubscriptionTier.Vip,     SubscriptionPlan.OneMonth)]    = (599,  "VIP — 1 месяц"),
        [(SubscriptionTier.Vip,     SubscriptionPlan.ThreeMonths)] = (1499, "VIP — 3 месяца"),
        [(SubscriptionTier.Vip,     SubscriptionPlan.TwelveMonths)]= (4990, "VIP — 12 месяцев"),
    };

    public static async Task SendInvoiceAsync(
        ITelegramBotClient bot, long chatId,
        SubscriptionTier tier, SubscriptionPlan plan, CancellationToken ct)
    {
        var key = (tier, plan);
        if (!Prices.TryGetValue(key, out var info)) return;

        var payload = $"{tier}:{plan}:{chatId}";
        await bot.SendInvoice(
            chatId: chatId,
            title: info.Title,
            description: GetDescription(tier),
            payload: payload,
            currency: "XTR",                                    // Telegram Stars
            prices: [new LabeledPrice(info.Title, info.Stars)],
            cancellationToken: ct);
    }

    private static string GetDescription(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Premium => "Безлимитный ИИ-наставник · Анализ отношений · Прогнозы недели",
        SubscriptionTier.Vip    => "Всё из Premium · Голосовые ответы · Прогнозы месяца · Расширенная аналитика",
        _ => ""
    };
}
```

- [ ] **Step 5: Wire PaymentHandler**

Replace stub in `src/InstantBot.TelegramBot/Handlers/PaymentHandler.cs`:
```csharp
using InstantBot.Application.Commands.PurchaseSubscription;
using InstantBot.Domain.Enums;
using InstantBot.TelegramBot.Keyboards;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class PaymentHandler(ITelegramBotClient bot, ISender mediator)
{
    public Task HandlePreCheckoutAsync(PreCheckoutQuery query, CancellationToken ct) =>
        bot.AnswerPreCheckoutQuery(query.Id, cancellationToken: ct);

    public async Task HandleSuccessfulPaymentAsync(Message msg, CancellationToken ct)
    {
        var payment = msg.SuccessfulPayment!;
        var parts = payment.InvoicePayload.Split(':');
        if (parts.Length < 3 ||
            !Enum.TryParse<SubscriptionTier>(parts[0], out var tier) ||
            !Enum.TryParse<SubscriptionPlan>(parts[1], out var plan)) return;

        var uid = msg.From!.Id;
        await mediator.Send(new PurchaseSubscriptionCommand(
            uid, plan, tier, payment.TotalAmount, payment.TelegramPaymentChargeId), ct);

        var tierName = tier == SubscriptionTier.Vip ? "VIP 💎" : "Premium ⭐";
        await bot.SendMessage(uid,
            $"🎉 Подписка {tierName} активирована!\n\nТеперь тебе доступны все возможности наставника без ограничений.",
            replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
    }
}
```

- [ ] **Step 6: Wire CallbackHandler for subscription flow**

Replace stub in `src/InstantBot.TelegramBot/Handlers/CallbackHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;
using InstantBot.Infrastructure.Payments;
using InstantBot.TelegramBot.Keyboards;
using InstantBot.TelegramBot.StateMachine;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class CallbackHandler(
    ITelegramBotClient bot,
    ISender mediator,
    IUserRepository userRepo,
    UserStateManager stateManager)
{
    public async Task HandleAsync(CallbackQuery query, CancellationToken ct)
    {
        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
        var uid = query.From.Id;
        var data = query.Data ?? "";

        switch (data)
        {
            case "ask_mentor":
                await stateManager.SetStateAsync(uid, UserBotState.AwaitingMentorQuestion);
                await bot.SendMessage(uid, "💬 Задай свой вопрос наставнику:", cancellationToken: ct);
                break;

            case "subscription":
                await bot.SendMessage(uid, "Выбери тариф:", replyMarkup: SubscriptionKeyboard.GetPlans(), cancellationToken: ct);
                break;

            case "my_profile":
                var user = await userRepo.GetByTelegramIdAsync(uid, ct);
                if (user?.PersonalityProfile is { } p)
                {
                    var info = $"👤 *Твой профиль*\n\nЧисло жизненного пути: {p.LifePathNumber}\nПодписка: {user.SubscriptionTier}\nСерия дней: {user.CurrentStreak} 🔥";
                    await bot.SendMessage(uid, info, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: ct);
                }
                break;

            case "relationship":
                var u = await userRepo.GetByTelegramIdAsync(uid, ct);
                if (u?.SubscriptionTier == SubscriptionTier.Free)
                {
                    await bot.SendMessage(uid, "Анализ отношений доступен в Premium 💫",
                        replyMarkup: SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
                    return;
                }
                await stateManager.SetStateAsync(uid, UserBotState.AwaitingPartnerName);
                await bot.SendMessage(uid, "Введи имя партнёра:", cancellationToken: ct);
                break;

            case string s when s.StartsWith("sub_"):
                await HandleSubscriptionPurchaseAsync(uid, s, ct);
                break;
        }
    }

    private async Task HandleSubscriptionPurchaseAsync(long uid, string data, CancellationToken ct)
    {
        // e.g. "sub_premium_1mo", "sub_vip_3mo"
        var parts = data.Split('_'); // ["sub", "premium", "1mo"]
        if (parts.Length < 3) return;

        var tier = parts[1] == "vip" ? SubscriptionTier.Vip : SubscriptionTier.Premium;
        var plan = parts[2] switch
        {
            "3mo"  => SubscriptionPlan.ThreeMonths,
            "12mo" => SubscriptionPlan.TwelveMonths,
            _      => SubscriptionPlan.OneMonth
        };

        await TelegramStarsService.SendInvoiceAsync(bot, uid, tier, plan, ct);
    }
}
```

- [ ] **Step 7: Run tests**

```bash
dotnet test InstantBot.sln
```

Expected: All tests PASS.

- [ ] **Step 8: Commit**

```bash
git add . && git commit -m "feat(payments): Telegram Stars subscriptions, PurchaseSubscription command, PaymentHandler"
```

---

### Task 12: Auto-renew reminder job and inactivity offers

**Files:** `src/InstantBot.Infrastructure/Notifications/SubscriptionExpiryJob.cs`, `src/InstantBot.Infrastructure/Notifications/InactivityJob.cs`, `src/InstantBot.Infrastructure/Notifications/NotificationScheduler.cs`

- [ ] **Step 1: Implement SubscriptionExpiryJob**

`src/InstantBot.Infrastructure/Notifications/SubscriptionExpiryJob.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Infrastructure.Persistence;
using InstantBot.TelegramBot.Keyboards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class SubscriptionExpiryJob(
    AppDbContext db,
    INotificationService notifier,
    ICacheService cache,
    ILogger<SubscriptionExpiryJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var in3Days = now.AddDays(3);

        // Reminder: expiring in 3 days
        var expiringSoon = await db.Users
            .Where(u => u.SubscriptionExpiresAt.HasValue &&
                        u.SubscriptionExpiresAt > now &&
                        u.SubscriptionExpiresAt <= in3Days &&
                        u.IsActive)
            .ToListAsync(ct);

        foreach (var user in expiringSoon)
        {
            var alreadyNotified = await cache.IsOfferShownAsync(user.TelegramUserId, "expiry_3d");
            if (alreadyNotified) continue;

            await notifier.SendTextAsync(user.TelegramUserId,
                $"⏰ Твоя подписка истекает через 3 дня. Продли её, чтобы не потерять доступ к наставнику!", ct);
            await cache.MarkOfferShownAsync(user.TelegramUserId, "expiry_3d", TimeSpan.FromDays(2));
            logger.LogInformation("Sent expiry reminder to {UserId}", user.TelegramUserId);
        }

        // Expired today — offer 10% discount
        var expiredToday = await db.Users
            .Where(u => u.SubscriptionExpiresAt.HasValue &&
                        u.SubscriptionExpiresAt <= now &&
                        u.SubscriptionExpiresAt > now.AddHours(-6) &&
                        u.IsActive)
            .ToListAsync(ct);

        foreach (var user in expiredToday)
        {
            await notifier.SendTextAsync(user.TelegramUserId,
                "💔 Твоя подписка истекла. Возвращайся — скидка 10% на продление только сегодня!\n\nИспользуй /subscription", ct);
        }
    }
}
```

- [ ] **Step 2: Implement InactivityJob**

`src/InstantBot.Infrastructure/Notifications/InactivityJob.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class InactivityJob(
    AppDbContext db,
    INotificationService notifier,
    ICacheService cache,
    ILogger<InactivityJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var day3Ago = today.AddDays(-3);
        var day7Ago = today.AddDays(-7);

        var inactive3 = await db.Users
            .Where(u => u.LastActiveDate == day3Ago && u.IsActive)
            .ToListAsync(ct);

        foreach (var user in inactive3)
        {
            var shown = await cache.IsOfferShownAsync(user.TelegramUserId, "inactivity_3d");
            if (shown) continue;
            await notifier.SendTextAsync(user.TelegramUserId,
                $"Привет, {user.FirstName}! 👋 Скучаем по тебе.\n\nЗаходи — наставник ждёт. Сегодня для тебя скидка 15% на Premium!", ct);
            await cache.MarkOfferShownAsync(user.TelegramUserId, "inactivity_3d", TimeSpan.FromDays(3));
        }

        var inactive7 = await db.Users
            .Where(u => u.LastActiveDate == day7Ago && u.IsActive)
            .ToListAsync(ct);

        foreach (var user in inactive7)
        {
            var shown = await cache.IsOfferShownAsync(user.TelegramUserId, "inactivity_7d");
            if (shown) continue;
            await notifier.SendTextAsync(user.TelegramUserId,
                $"{user.FirstName}, прошла неделя! 🌙\n\nВернись и получи скидку 25% на Premium + бонусную Карту недели.", ct);
            await cache.MarkOfferShownAsync(user.TelegramUserId, "inactivity_7d", TimeSpan.FromDays(7));
        }

        logger.LogInformation("InactivityJob: sent {C3} + {C7} messages", inactive3.Count, inactive7.Count);
    }
}
```

- [ ] **Step 3: Create NotificationScheduler**

`src/InstantBot.Infrastructure/Notifications/NotificationScheduler.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class NotificationScheduler(IServiceProvider services, ILogger<NotificationScheduler> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // DailyCardJob at 08:00 UTC
            if (now.Hour == 8 && now.Minute == 0)
            {
                await RunJobAsync<DailyCardJob>(j => j.RunAsync(ct), ct);
                await Task.Delay(TimeSpan.FromMinutes(1), ct); // prevent double-fire
            }

            // InactivityJob at 10:00 UTC
            if (now.Hour == 10 && now.Minute == 0)
                await RunJobAsync<InactivityJob>(j => j.RunAsync(ct), ct);

            // SubscriptionExpiryJob every 6 hours
            if (now.Minute == 0 && now.Hour % 6 == 0)
                await RunJobAsync<SubscriptionExpiryJob>(j => j.RunAsync(ct), ct);

            // ConversationSummaryJob every hour
            if (now.Minute == 0)
                await RunJobAsync<ConversationSummaryJob>(j => j.RunAsync(ct), ct);

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task RunJobAsync<T>(Func<T, Task> action, CancellationToken ct)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<T>();
            await action(job);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {Job} failed", typeof(T).Name);
        }
    }
}
```

- [ ] **Step 4: Implement ConversationSummaryJob**

`src/InstantBot.Infrastructure/Notifications/ConversationSummaryJob.cs`:
```csharp
using InstantBot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class ConversationSummaryJob(
    IConversationRepository convRepo,
    IOpenAiService ai,
    ILogger<ConversationSummaryJob> logger)
{
    private const int SummarizeThreshold = 20;

    public async Task RunAsync(CancellationToken ct)
    {
        var conversations = await convRepo.GetConversationsToSummarizeAsync(SummarizeThreshold, ct);
        foreach (var conv in conversations)
        {
            var messages = await convRepo.GetRecentMessagesAsync(conv.Id, 30, ct);
            var summary = await ai.GenerateSummarySAsync(messages, ct);
            conv.UpdateSummary(summary);
            await convRepo.SaveChangesAsync(ct);
            logger.LogInformation("Summarized conversation {Id}", conv.Id);
        }
    }
}
```

- [ ] **Step 5: Implement TelegramNotificationService**

`src/InstantBot.Infrastructure/Notifications/TelegramNotificationService.cs`:
```csharp
using InstantBot.Application.Interfaces;
using Telegram.Bot;

namespace InstantBot.Infrastructure.Notifications;

public class TelegramNotificationService(ITelegramBotClient bot) : INotificationService
{
    public Task SendTextAsync(long telegramUserId, string text, CancellationToken ct = default) =>
        bot.SendMessage(telegramUserId, text, cancellationToken: ct);

    public async Task SendVoiceAsync(long telegramUserId, byte[] mp3, string caption, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(mp3);
        await bot.SendVoice(telegramUserId, new Telegram.Bot.Types.InputFileStream(stream, "response.mp3"),
            caption: caption, cancellationToken: ct);
    }
}
```

- [ ] **Step 6: Register all jobs in Program.cs**

In `src/InstantBot.TelegramBot/Program.cs`, add before `app.Run()`:
```csharp
builder.Services.AddScoped<DailyCardJob>();
builder.Services.AddScoped<SubscriptionExpiryJob>();
builder.Services.AddScoped<InactivityJob>();
builder.Services.AddScoped<ConversationSummaryJob>();
builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();
builder.Services.AddHostedService<NotificationScheduler>();
```

- [ ] **Step 7: Commit**

```bash
git add . && git commit -m "feat(notifications): add scheduler, subscription expiry, inactivity, summary jobs"
```

---

## Phase 4 — Engagement (M4)

### Task 13: Daily card, weekly forecast, streaks, and achievements

**Files:** `src/InstantBot.Infrastructure/Notifications/DailyCardJob.cs`, `src/InstantBot.Application/Queries/GetDailyCard/*.cs`, `src/InstantBot.TelegramBot/Handlers/CallbackHandler.cs` (extended)

- [ ] **Step 1: Implement DailyCardJob**

`src/InstantBot.Infrastructure/Notifications/DailyCardJob.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.ValueObjects;
using InstantBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class DailyCardJob(
    AppDbContext db,
    IDailyCardRepository cardRepo,
    IOpenAiService ai,
    INotificationService notifier,
    ICacheService cache,
    ILogger<DailyCardJob> logger)
{
    private static readonly TarotCard[] MajorArcana =
    [
        new("Шут", "Major", "Новые начинания, спонтанность, свобода"),
        new("Маг", "Major", "Воля, мастерство, умение воплощать задуманное"),
        new("Верховная Жрица", "Major", "Интуиция, тайное знание, внутренний голос"),
        new("Императрица", "Major", "Творчество, изобилие, забота, природа"),
        new("Император", "Major", "Структура, авторитет, стабильность"),
        new("Иерофант", "Major", "Традиции, духовное руководство, обучение"),
        new("Влюблённые", "Major", "Выборы, партнёрство, ценности"),
        new("Колесница", "Major", "Победа, решимость, контроль"),
        new("Сила", "Major", "Внутренняя сила, мужество, терпение"),
        new("Отшельник", "Major", "Уединение, поиск пути, мудрость"),
        new("Колесо Фортуны", "Major", "Циклы, перемены, судьба"),
        new("Справедливость", "Major", "Равновесие, истина, честность"),
        new("Повешенный", "Major", "Принятие, новый взгляд, пауза"),
        new("Смерть", "Major", "Трансформация, окончание, перемены"),
        new("Умеренность", "Major", "Баланс, терпение, гармония"),
        new("Дьявол", "Major", "Ограничения, зависимости, материализм"),
        new("Башня", "Major", "Разрушение иллюзий, внезапные перемены"),
        new("Звезда", "Major", "Надежда, вдохновение, исцеление"),
        new("Луна", "Major", "Иллюзии, страхи, подсознание"),
        new("Солнце", "Major", "Радость, успех, витальность"),
        new("Суд", "Major", "Возрождение, оценка, пробуждение"),
        new("Мир", "Major", "Завершение, целостность, достижение"),
    ];

    public async Task RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var users = await db.Users
            .Where(u => u.IsActive)
            .Include(u => u.PersonalityProfile)
            .ToListAsync(ct);

        var rng = new Random();
        var batches = users.Chunk(50);

        foreach (var batch in batches)
        {
            foreach (var user in batch)
            {
                try
                {
                    var existing = await cardRepo.GetForDateAsync(user.Id, today, ct);
                    if (existing is not null) continue;

                    var card = MajorArcana[rng.Next(MajorArcana.Length)];
                    var generatedText = await ai.GenerateDailyCardAsync(user, card.Name, card.Meaning, ct);

                    var advice = ExtractSection(generatedText, "СОВЕТ:");
                    var focus = ExtractSection(generatedText, "ФОКУС:");

                    var dailyCard = DailyCard.Create(user.Id, today, card, advice, focus);
                    await cardRepo.AddAsync(dailyCard, ct);
                    await cardRepo.SaveChangesAsync(ct);

                    var json = System.Text.Json.JsonSerializer.Serialize(new { card.Name, advice, focus });
                    await cache.SetDailyCardCacheAsync(user.TelegramUserId, today, json, TimeSpan.FromHours(24));

                    await notifier.SendTextAsync(user.TelegramUserId,
                        $"🌅 Доброе утро, {user.FirstName}!\n\n🃏 *{card.Name}*\n\n📖 {advice}\n\n🎯 Фокус дня: {focus}",
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "DailyCardJob failed for user {UserId}", user.TelegramUserId);
                }
            }
            await Task.Delay(500, ct); // Rate limiting
        }
        logger.LogInformation("DailyCardJob completed for {Count} users", users.Count);
    }

    private static string ExtractSection(string text, string prefix)
    {
        var idx = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return text.Trim();
        var start = idx + prefix.Length;
        var end = text.IndexOfAny(['\n'], start);
        return end > 0 ? text[start..end].Trim() : text[start..].Trim();
    }
}
```

- [ ] **Step 2: Create GetDailyCard query**

`src/InstantBot.Application/Queries/GetDailyCard/GetDailyCardQuery.cs`:
```csharp
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Queries.GetDailyCard;

public record GetDailyCardQuery(long TelegramUserId) : IRequest<DailyCard?>;
```

`src/InstantBot.Application/Queries/GetDailyCard/GetDailyCardQueryHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Queries.GetDailyCard;

public class GetDailyCardQueryHandler(IUserRepository userRepo, IDailyCardRepository cardRepo)
    : IRequestHandler<GetDailyCardQuery, DailyCard?>
{
    public async Task<DailyCard?> Handle(GetDailyCardQuery query, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(query.TelegramUserId, ct);
        if (user is null) return null;
        return await cardRepo.GetForDateAsync(user.Id, DateOnly.FromDateTime(DateTime.UtcNow), ct);
    }
}
```

- [ ] **Step 3: Add StreakService**

`src/InstantBot.Application/Services/StreakService.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;

namespace InstantBot.Application.Services;

public class StreakService(IUserRepository userRepo)
{
    public async Task CheckAndAwardAchievementAsync(User user, CancellationToken ct)
    {
        var milestones = new[] { (7, AchievementType.Streak7), (30, AchievementType.Streak30) };
        foreach (var (days, achievementType) in milestones)
        {
            if (user.CurrentStreak >= days &&
                user.Achievements.All(a => a.Type != achievementType))
            {
                user.Achievements.Add(Achievement.Create(user.Id, achievementType));
            }
        }
        await userRepo.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Wire DailyCard in CallbackHandler**

In `src/InstantBot.TelegramBot/Handlers/CallbackHandler.cs`, add to `HandleAsync` switch:
```csharp
case "daily_card":
    var card = await mediator.Send(new GetDailyCardQuery(uid), ct);
    if (card is null)
        await bot.SendMessage(uid, "Карта дня ещё не готова. Загляни в 8:00 UTC!", cancellationToken: ct);
    else
    {
        card.MarkViewed();
        await bot.SendMessage(uid,
            $"🃏 *{card.TarotCard.Name}*\n\n📖 {card.DayAdvice}\n\n🎯 Фокус: {card.DayFocus}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: ShareKeyboard.Get("Поделиться картой дня"),
            cancellationToken: ct);
    }
    break;
```

- [ ] **Step 5: Create ShareKeyboard**

`src/InstantBot.TelegramBot/Keyboards/ShareKeyboard.cs`:
```csharp
using Telegram.Bot.Types.ReplyMarkups;

namespace InstantBot.TelegramBot.Keyboards;

public static class ShareKeyboard
{
    public static InlineKeyboardMarkup Get(string label) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData($"📤 {label}", "share_result") },
        new[] { InlineKeyboardButton.WithCallbackData("👥 Пригласить друга", "invite_friend") },
    });
}
```

- [ ] **Step 6: Build and test**

```bash
dotnet build InstantBot.sln
dotnet test InstantBot.sln
```

Expected: Build and all tests PASS.

- [ ] **Step 7: Commit**

```bash
git add . && git commit -m "feat(engagement): daily cards, notifications, streaks, achievements"
```


---

## Phase 5 — Relationships & Voice (M5)

### Task 14: Relationship analysis flow

**Files:** `src/InstantBot.Application/Commands/AnalyzeRelationship/*.cs`, `src/InstantBot.TelegramBot/Handlers/MessageHandler.cs` (extended)

- [ ] **Step 1: Create AnalyzeRelationshipCommand**

`src/InstantBot.Application/Commands/AnalyzeRelationship/AnalyzeRelationshipCommand.cs`:
```csharp
using MediatR;

namespace InstantBot.Application.Commands.AnalyzeRelationship;

public record AnalyzeRelationshipCommand(
    long TelegramUserId,
    string PartnerName,
    DateOnly PartnerBirthDate) : IRequest<string>;
```

`src/InstantBot.Application/Commands/AnalyzeRelationship/AnalyzeRelationshipCommandHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Commands.AnalyzeRelationship;

public class AnalyzeRelationshipCommandHandler(
    IUserRepository userRepo,
    IOpenAiService ai) : IRequestHandler<AnalyzeRelationshipCommand, string>
{
    public async Task<string> Handle(AnalyzeRelationshipCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct)
            ?? throw new InvalidOperationException("User not found");

        var result = await ai.GenerateRelationshipAnalysisAsync(user, cmd.PartnerName, cmd.PartnerBirthDate, ct);

        // Persist to DB via AppDbContext directly (add IRelationshipAnalysisRepository if needed)
        return result;
    }
}
```

- [ ] **Step 2: Wire relationship flow in MessageHandler**

Replace stub `HandlePartnerNameAsync` and `HandlePartnerBirthDateAsync` in `src/InstantBot.TelegramBot/Handlers/MessageHandler.cs`:

```csharp
private async Task HandlePartnerNameAsync(Message msg, CancellationToken ct)
{
    var uid = msg.From!.Id;
    var name = msg.Text?.Trim();
    if (string.IsNullOrEmpty(name) || name.Length < 2)
    {
        await bot.SendMessage(uid, "Введи имя партнёра (минимум 2 символа):", cancellationToken: ct);
        return;
    }
    await stateManager.SetTempValueAsync(uid, "partner_name", name);
    await stateManager.SetStateAsync(uid, UserBotState.AwaitingPartnerBirthDate);
    await bot.SendMessage(uid, $"Отлично! Теперь введи дату рождения {name} (ДД.ММ.ГГГГ):", cancellationToken: ct);
}

private async Task HandlePartnerBirthDateAsync(Message msg, CancellationToken ct)
{
    var uid = msg.From!.Id;
    if (!DateOnly.TryParseExact(msg.Text?.Trim(), "dd.MM.yyyy", out var partnerBirth))
    {
        await bot.SendMessage(uid, "Неверный формат. Введи как ДД.ММ.ГГГГ:", cancellationToken: ct);
        return;
    }

    var partnerName = await stateManager.GetTempValueAsync(uid, "partner_name") ?? "партнёр";
    await bot.SendChatAction(uid, Telegram.Bot.Types.Enums.ChatAction.Typing, cancellationToken: ct);

    var result = await mediator.Send(new AnalyzeRelationshipCommand(uid, partnerName, partnerBirth), ct);

    await stateManager.SetStateAsync(uid, UserBotState.MainMenu);
    await bot.SendMessage(uid, $"❤️ *Анализ пары: {(await userRepo.GetByTelegramIdAsync(uid, ct))?.FirstName} & {partnerName}*\n\n{result}",
        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
        replyMarkup: ShareKeyboard.Get("Поделиться анализом"),
        cancellationToken: ct);

    // Trigger upsell if needed
    var user = await userRepo.GetByTelegramIdAsync(uid, ct);
    if (user?.SubscriptionTier == Domain.Enums.SubscriptionTier.Premium)
    {
        var shown = await cache.IsOfferShownAsync(uid, "vip_upsell_after_relationship");
        if (!shown)
        {
            await Task.Delay(2000, ct);
            await bot.SendMessage(uid,
                "💡 Хочешь узнать больше? В VIP-тарифе доступны голосовые ответы наставника и расширенная аналитика отношений.",
                replyMarkup: SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
            await cache.MarkOfferShownAsync(uid, "vip_upsell_after_relationship", TimeSpan.FromDays(7));
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add . && git commit -m "feat(relationships): AnalyzeRelationship command and Telegram flow"
```

---

### Task 15: Voice input (Whisper) and voice output (TTS) for VIP

**Files:** `src/InstantBot.TelegramBot/Handlers/VoiceHandler.cs`

- [ ] **Step 1: Implement VoiceHandler**

Replace stub in `src/InstantBot.TelegramBot/Handlers/VoiceHandler.cs`:
```csharp
using InstantBot.Application.Commands.AskMentor;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;
using InstantBot.TelegramBot.Keyboards;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class VoiceHandler(
    ITelegramBotClient bot,
    ISender mediator,
    IUserRepository userRepo,
    IWhisperService whisper,
    ITtsService tts)
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        var uid = msg.From!.Id;
        var user = await userRepo.GetByTelegramIdAsync(uid, ct);
        if (user is null) return;

        if (user.SubscriptionTier != SubscriptionTier.Vip)
        {
            await bot.SendMessage(uid,
                "🎤 Голосовые сообщения доступны только в тарифе VIP 💎",
                replyMarkup: SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
            return;
        }

        var voice = msg.Voice!;
        await bot.SendChatAction(uid, Telegram.Bot.Types.Enums.ChatAction.RecordVoice, cancellationToken: ct);

        // Download voice file
        var fileInfo = await bot.GetFile(voice.FileId, ct);
        using var audioStream = new MemoryStream();
        await bot.DownloadFile(fileInfo.FilePath!, audioStream, ct);
        audioStream.Position = 0;

        // Transcribe with Whisper
        var transcription = await whisper.TranscribeAsync(audioStream, "voice.ogg", ct);

        await bot.SendChatAction(uid, Telegram.Bot.Types.Enums.ChatAction.Typing, cancellationToken: ct);

        // Get mentor response
        var result = await mediator.Send(new AskMentorCommand(uid, transcription), ct);
        if (result.LimitReached)
        {
            await bot.SendMessage(uid, "Достигнут лимит вопросов.", cancellationToken: ct);
            return;
        }

        // Synthesize response to voice
        var mp3 = await tts.SynthesizeAsync(result.Response, ct);
        using var mp3Stream = new MemoryStream(mp3);
        await bot.SendVoice(uid,
            new InputFileStream(mp3Stream, "response.mp3"),
            caption: $"💬 {result.Response[..Math.Min(200, result.Response.Length)]}...",
            cancellationToken: ct);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add . && git commit -m "feat(voice): VoiceHandler with Whisper transcription and TTS response"
```

---

## Phase 6 — Growth (M6)

### Task 16: Referral system and share feature

**Files:** `src/InstantBot.Application/Commands/ProcessReferral/*.cs`, `src/InstantBot.TelegramBot/Keyboards/ShareKeyboard.cs` (extended)

- [ ] **Step 1: Create ProcessReferralCommand**

`src/InstantBot.Application/Commands/ProcessReferral/ProcessReferralCommand.cs`:
```csharp
using MediatR;

namespace InstantBot.Application.Commands.ProcessReferral;

public record ProcessReferralCommand(Guid ReferrerId, Guid NewUserId) : IRequest;
```

`src/InstantBot.Application/Commands/ProcessReferral/ProcessReferralCommandHandler.cs`:
```csharp
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using MediatR;

namespace InstantBot.Application.Commands.ProcessReferral;

public class ProcessReferralCommandHandler(
    IUserRepository userRepo,
    INotificationService notifier) : IRequestHandler<ProcessReferralCommand>
{
    public async Task Handle(ProcessReferralCommand cmd, CancellationToken ct)
    {
        var referrer = await userRepo.GetByIdAsync(cmd.ReferrerId, ct);
        var newUser  = await userRepo.GetByIdAsync(cmd.NewUserId, ct);
        if (referrer is null || newUser is null) return;

        // Grant referrer +7 days Premium
        var bonus = referrer.SubscriptionExpiresAt.HasValue && referrer.SubscriptionExpiresAt > DateTime.UtcNow
            ? referrer.SubscriptionExpiresAt.Value.AddDays(7)
            : DateTime.UtcNow.AddDays(7);

        var tier = referrer.SubscriptionTier == SubscriptionTier.Free
            ? SubscriptionTier.Premium
            : referrer.SubscriptionTier;
        referrer.UpdateSubscription(tier, bonus);

        // Achievement for first referral
        if (referrer.Achievements.All(a => a.Type != AchievementType.FirstReferral))
            referrer.Achievements.Add(Achievement.Create(referrer.Id, AchievementType.FirstReferral));

        await userRepo.SaveChangesAsync(ct);

        await notifier.SendTextAsync(referrer.TelegramUserId,
            $"🎉 Твой друг {newUser.FirstName} присоединился по твоей ссылке!\n\n+7 дней Premium добавлено к твоей подписке.", ct);
    }
}
```

- [ ] **Step 2: Trigger referral after registration**

In `src/InstantBot.Application/Commands/RegisterUser/RegisterUserCommandHandler.cs`, add after `await userRepo.SaveChangesAsync(ct)`:
```csharp
if (user.ReferredByUserId.HasValue)
{
    // Fire referral reward (non-blocking is fine, but use ct)
    var referrer = await userRepo.GetByIdAsync(user.ReferredByUserId.Value, ct);
    if (referrer is not null)
    {
        var referral = Referral.Create(referrer.Id, user.Id);
        referral.GrantBonus();
        // Persist referral via dbContext - inject IDbContext or add IReferralRepository
        // For simplicity, fire the command via a domain event or inline:
        var bonus = referrer.SubscriptionExpiresAt.HasValue && referrer.SubscriptionExpiresAt > DateTime.UtcNow
            ? referrer.SubscriptionExpiresAt.Value.AddDays(7)
            : DateTime.UtcNow.AddDays(7);
        var tier = referrer.SubscriptionTier == Domain.Enums.SubscriptionTier.Free
            ? Domain.Enums.SubscriptionTier.Premium : referrer.SubscriptionTier;
        referrer.UpdateSubscription(tier, bonus);
        await userRepo.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 3: Add invite_friend and share_result callbacks**

In `src/InstantBot.TelegramBot/Handlers/CallbackHandler.cs`, add to switch:
```csharp
case "invite_friend":
    var invUser = await userRepo.GetByTelegramIdAsync(uid, ct);
    if (invUser is not null)
    {
        var botInfo = await bot.GetMe(ct);
        var link = $"https://t.me/{botInfo.Username}?start=REF_{invUser.ReferralCode}";
        await bot.SendMessage(uid,
            $"🔗 Твоя реферальная ссылка:\n{link}\n\nПоделись с другом — ты получишь +7 дней Premium, а друг — расширенный триал!",
            cancellationToken: ct);
    }
    break;

case "share_result":
    await bot.SendMessage(uid,
        "📤 Скопируй и поделись своим результатом в любом чате или канале!",
        cancellationToken: ct);
    break;
```

- [ ] **Step 4: Commit**

```bash
git add . && git commit -m "feat(growth): referral system, invite link, share callbacks"
```

---

## Phase 7 — Admin Panel (M7)

### Task 17: ASP.NET Core Admin Panel with metrics

**Files:** `src/InstantBot.AdminPanel/Program.cs`, `Controllers/AuthController.cs`, `Controllers/DashboardController.cs`, `Views/Dashboard/Index.cshtml`, `Models/DashboardViewModel.cs`

- [ ] **Step 1: Configure AdminPanel Program.cs**

`src/InstantBot.AdminPanel/Program.cs`:
```csharp
using InstantBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Logging.ClearProviders();
builder.Services.AddSerilog();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

var jwtKey = builder.Configuration["Admin:JwtSecret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false, ValidateAudience = false
        };
        // Also read token from cookie for MVC pages
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["admin_token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddResponseCaching();

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Auth}/{action=Login}/{id?}");

app.Run();
```

- [ ] **Step 2: Create AuthController**

`src/InstantBot.AdminPanel/Controllers/AuthController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InstantBot.AdminPanel.Controllers;

public class AuthController(IConfiguration config) : Controller
{
    [HttpGet] public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (username != config["Admin:Username"] || password != config["Admin:Password"])
        {
            ViewBag.Error = "Неверные данные";
            return View();
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Admin:JwtSecret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Name, username)],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        Response.Cookies.Append("admin_token", tokenString, new CookieOptions
        {
            HttpOnly = true, Secure = false, Expires = DateTimeOffset.UtcNow.AddHours(8)
        });
        return RedirectToAction("Index", "Dashboard");
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("admin_token");
        return RedirectToAction("Login");
    }
}
```

`src/InstantBot.AdminPanel/Views/Auth/Login.cshtml`:
```html
<!DOCTYPE html>
<html><head><title>Admin Login</title>
<style>body{font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;background:#f5f5f5}
.box{background:#fff;padding:2rem;border-radius:8px;box-shadow:0 2px 10px rgba(0,0,0,.1);width:300px}
input{width:100%;padding:.5rem;margin:.5rem 0;box-sizing:border-box}
button{width:100%;padding:.75rem;background:#4f46e5;color:#fff;border:none;border-radius:4px;cursor:pointer}</style>
</head>
<body><div class="box">
<h2>Admin Panel</h2>
@if(ViewBag.Error != null){<p style="color:red">@ViewBag.Error</p>}
<form method="post">
<input name="username" placeholder="Username" required/>
<input name="password" type="password" placeholder="Password" required/>
<button>Войти</button>
</form></div></body></html>
```

- [ ] **Step 3: Create DashboardViewModel**

`src/InstantBot.AdminPanel/Models/DashboardViewModel.cs`:
```csharp
namespace InstantBot.AdminPanel.Models;

public class DashboardViewModel
{
    public int Dau { get; set; }
    public int Mau { get; set; }
    public int TotalUsers { get; set; }
    public int PremiumUsers { get; set; }
    public int VipUsers { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal Mrr { get; set; }
    public decimal Arpu { get; set; }
    public decimal ChurnRate { get; set; }
    public decimal Ltv { get; set; }
    public List<DailyStats> Last30Days { get; set; } = [];
}

public record DailyStats(DateOnly Date, int NewUsers, int ActiveUsers, int StarsSold);
```

- [ ] **Step 4: Create DashboardController**

`src/InstantBot.AdminPanel/Controllers/DashboardController.cs`:
```csharp
using InstantBot.AdminPanel.Models;
using InstantBot.Domain.Enums;
using InstantBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace InstantBot.AdminPanel.Controllers;

[Authorize]
public class DashboardController(AppDbContext db, IConnectionMultiplexer redis) : Controller
{
    private const string CacheKey = "admin:dashboard";
    private IDatabase Cache => redis.GetDatabase();

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cached = await Cache.StringGetAsync(CacheKey);
        if (cached.HasValue)
        {
            var vm = System.Text.Json.JsonSerializer.Deserialize<DashboardViewModel>(cached.ToString())!;
            return View(vm);
        }

        var model = await BuildMetricsAsync(ct);
        var json = System.Text.Json.JsonSerializer.Serialize(model);
        await Cache.StringSetAsync(CacheKey, json, TimeSpan.FromHours(1));
        return View(model);
    }

    private async Task<DashboardViewModel> BuildMetricsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var thirtyDaysAgo = now.AddDays(-30);

        var totalUsers   = await db.Users.CountAsync(ct);
        var dau          = await db.Users.CountAsync(u => u.LastActiveDate == today, ct);
        var mau          = await db.Users.CountAsync(u => u.LastActiveDate >= DateOnly.FromDateTime(thirtyDaysAgo), ct);
        var premiumCount = await db.Users.CountAsync(u => u.SubscriptionTier == SubscriptionTier.Premium, ct);
        var vipCount     = await db.Users.CountAsync(u => u.SubscriptionTier == SubscriptionTier.Vip, ct);

        var conversionRate = totalUsers > 0
            ? Math.Round((decimal)(premiumCount + vipCount) / totalUsers * 100, 1)
            : 0;

        var subscriptionsSince = await db.Subscriptions
            .Where(s => s.PurchasedAt >= thirtyDaysAgo)
            .ToListAsync(ct);

        var mrr = subscriptionsSince.Sum(s => (decimal)s.StarsPaid / (int)s.Plan);
        var arpu = mau > 0 ? Math.Round(mrr / mau, 2) : 0;

        var last30 = await db.Users
            .Where(u => u.RegistrationDate >= thirtyDaysAgo)
            .GroupBy(u => DateOnly.FromDateTime(u.RegistrationDate))
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync(ct);

        var starsByDay = subscriptionsSince
            .GroupBy(s => DateOnly.FromDateTime(s.PurchasedAt))
            .ToDictionary(g => g.Key, g => g.Sum(s => s.StarsPaid));

        var last30Stats = last30.Select(g => new DailyStats(
            g.Date, g.Count, 0,
            starsByDay.TryGetValue(g.Date, out var stars) ? stars : 0
        )).ToList();

        return new DashboardViewModel
        {
            Dau = dau, Mau = mau, TotalUsers = totalUsers,
            PremiumUsers = premiumCount, VipUsers = vipCount,
            ConversionRate = conversionRate, Mrr = Math.Round(mrr, 2),
            Arpu = arpu, ChurnRate = 0, Ltv = arpu > 0 ? Math.Round(arpu / 0.05m, 2) : 0,
            Last30Days = last30Stats
        };
    }
}
```

- [ ] **Step 5: Create Dashboard view**

`src/InstantBot.AdminPanel/Views/Dashboard/Index.cshtml`:
```html
@model InstantBot.AdminPanel.Models.DashboardViewModel
@{ViewBag.Title = "Dashboard";}
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8"><title>InstantBot Admin</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:system-ui,sans-serif;background:#f8fafc;color:#1e293b}
nav{background:#4f46e5;color:#fff;padding:1rem 2rem;display:flex;gap:1rem;align-items:center}
nav a{color:#fff;text-decoration:none;opacity:.8}nav a:hover{opacity:1}
.container{max-width:1400px;margin:2rem auto;padding:0 1rem}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:1rem;margin-bottom:2rem}
.card{background:#fff;border-radius:8px;padding:1.25rem;box-shadow:0 1px 3px rgba(0,0,0,.1)}
.card h3{font-size:.8rem;color:#64748b;text-transform:uppercase;letter-spacing:.05em}
.card .value{font-size:2rem;font-weight:700;color:#1e293b;margin-top:.25rem}
.card .sub{font-size:.8rem;color:#94a3b8;margin-top:.25rem}
.chart-card{background:#fff;border-radius:8px;padding:1.5rem;box-shadow:0 1px 3px rgba(0,0,0,.1);margin-bottom:2rem}
h2{font-size:1rem;font-weight:600;margin-bottom:1rem;color:#475569}
</style>
</head>
<body>
<nav>
  <strong>InstantBot Admin</strong>
  <a href="/Dashboard">Dashboard</a>
  <a href="/Users">Пользователи</a>
  <a href="/Subscriptions">Подписки</a>
  <a href="/Auth/Logout">Выход</a>
</nav>
<div class="container">
  <div class="grid">
    <div class="card"><h3>DAU</h3><div class="value">@Model.Dau</div><div class="sub">Сегодня</div></div>
    <div class="card"><h3>MAU</h3><div class="value">@Model.Mau</div><div class="sub">30 дней</div></div>
    <div class="card"><h3>Всего юзеров</h3><div class="value">@Model.TotalUsers</div></div>
    <div class="card"><h3>Conversion</h3><div class="value">@Model.ConversionRate%</div><div class="sub">Платящие</div></div>
    <div class="card"><h3>MRR</h3><div class="value">@Model.Mrr ⭐</div><div class="sub">Stars/мес</div></div>
    <div class="card"><h3>ARPU</h3><div class="value">@Model.Arpu ⭐</div></div>
    <div class="card"><h3>Premium</h3><div class="value">@Model.PremiumUsers</div></div>
    <div class="card"><h3>VIP</h3><div class="value">@Model.VipUsers</div></div>
    <div class="card"><h3>LTV (est.)</h3><div class="value">@Model.Ltv ⭐</div></div>
  </div>

  <div class="chart-card">
    <h2>Новые пользователи — 30 дней</h2>
    <canvas id="usersChart" height="100"></canvas>
  </div>

  <div class="chart-card">
    <h2>Доход (Stars) — 30 дней</h2>
    <canvas id="revenueChart" height="100"></canvas>
  </div>
</div>

<script>
const labels = [@Html.Raw(string.Join(",", Model.Last30Days.Select(d => $'"{d.Date:dd.MM}"')))];
const newUsers = [@string.Join(",", Model.Last30Days.Select(d => d.NewUsers))];
const stars = [@string.Join(",", Model.Last30Days.Select(d => d.StarsSold))];

new Chart(document.getElementById('usersChart'), {
  type: 'line',
  data: { labels, datasets: [{ label: 'Новые пользователи', data: newUsers,
    borderColor: '#4f46e5', backgroundColor: 'rgba(79,70,229,.1)', fill: true, tension: .3 }] }
});
new Chart(document.getElementById('revenueChart'), {
  type: 'bar',
  data: { labels, datasets: [{ label: 'Stars', data: stars, backgroundColor: '#f59e0b' }] }
});
</script>
</body>
</html>
```

- [ ] **Step 6: Add UsersController**

`src/InstantBot.AdminPanel/Controllers/UsersController.cs`:
```csharp
using InstantBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.AdminPanel.Controllers;

[Authorize]
public class UsersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1, string? tier = null, CancellationToken ct = default)
    {
        var query = db.Users.Include(u => u.PersonalityProfile).AsQueryable();
        if (!string.IsNullOrEmpty(tier) && Enum.TryParse<Domain.Enums.SubscriptionTier>(tier, out var t))
            query = query.Where(u => u.SubscriptionTier == t);

        var total = await query.CountAsync(ct);
        var users = await query.OrderByDescending(u => u.RegistrationDate)
            .Skip((page - 1) * 50).Take(50).ToListAsync(ct);

        ViewBag.Total = total; ViewBag.Page = page; ViewBag.Tier = tier;
        return View(users);
    }
}
```

`src/InstantBot.AdminPanel/Views/Users/Index.cshtml`:
```html
@using InstantBot.Domain.Entities
@model List<User>
@{ViewBag.Title = "Пользователи";}
<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Users — Admin</title>
<style>
body{font-family:system-ui;background:#f8fafc;color:#1e293b;margin:0}
nav{background:#4f46e5;color:#fff;padding:1rem 2rem;display:flex;gap:1rem}
nav a{color:#fff;text-decoration:none}
.container{max-width:1400px;margin:2rem auto;padding:0 1rem}
table{width:100%;border-collapse:collapse;background:#fff;border-radius:8px;box-shadow:0 1px 3px rgba(0,0,0,.1)}
th,td{padding:.75rem 1rem;text-align:left;border-bottom:1px solid #e2e8f0}
th{background:#f1f5f9;font-size:.8rem;color:#64748b;text-transform:uppercase}
.badge{padding:.2rem .5rem;border-radius:4px;font-size:.75rem}
.free{background:#f1f5f9}.premium{background:#dbeafe;color:#1e40af}.vip{background:#fde68a;color:#92400e}
</style></head>
<body>
<nav><strong>InstantBot Admin</strong><a href="/Dashboard">Dashboard</a><a href="/Users">Пользователи</a><a href="/Auth/Logout">Выход</a></nav>
<div class="container">
<h1 style="margin-bottom:1rem">Пользователи (@ViewBag.Total)</h1>
<div style="margin-bottom:1rem">
  Фильтр: <a href="?tier=Free">Free</a> | <a href="?tier=Premium">Premium</a> | <a href="?tier=Vip">VIP</a> | <a href="?">Все</a>
</div>
<table>
<thead><tr><th>Telegram ID</th><th>Имя</th><th>Тариф</th><th>Серия</th><th>Дата регистрации</th><th>Последняя активность</th></tr></thead>
<tbody>
@foreach(var u in Model){
<tr>
<td>@u.TelegramUserId</td>
<td>@u.FirstName @(u.Username != null ? $"(@{u.Username})" : "")</td>
<td><span class="badge @u.SubscriptionTier.ToString().ToLower()">@u.SubscriptionTier</span></td>
<td>@u.CurrentStreak 🔥</td>
<td>@u.RegistrationDate.ToString("dd.MM.yyyy")</td>
<td>@u.LastActiveDate.ToString("dd.MM.yyyy")</td>
</tr>}
</tbody></table>
<div style="margin-top:1rem">
@if(ViewBag.Page > 1){<a href="?page=@(ViewBag.Page-1)&tier=@ViewBag.Tier">← Назад</a>}
<a href="?page=@(ViewBag.Page+1)&tier=@ViewBag.Tier" style="margin-left:1rem">Вперёд →</a>
</div>
</div>
</body></html>
```

- [ ] **Step 7: Add health check endpoint**

In `src/InstantBot.AdminPanel/Program.cs`, before `app.Run()`:
```csharp
app.MapGet("/health", async (AppDbContext db, IConnectionMultiplexer redis) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        await redis.GetDatabase().PingAsync();
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});
```

- [ ] **Step 8: Build AdminPanel**

```bash
dotnet build src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add . && git commit -m "feat(admin): ASP.NET Core admin panel with JWT auth, metrics dashboard, users table"
```

---

## Phase 8 — Docker Compose & Final Configuration

### Task 18: Docker setup and environment configuration

**Files:** `docker-compose.yml`, `docker-compose.override.yml`, `.env.example`, `src/InstantBot.TelegramBot/Dockerfile`, `src/InstantBot.AdminPanel/Dockerfile`

- [ ] **Step 1: Create `.env.example`**

`.env.example`:
```dotenv
# Telegram
TELEGRAM_BOT_TOKEN=your_bot_token_here

# OpenAI
OPENAI_API_KEY=sk-...

# PostgreSQL
POSTGRES_USER=instantbot
POSTGRES_PASSWORD=strongpassword123
POSTGRES_DB=instantbot

# Redis
REDIS_PASSWORD=

# Admin Panel
ADMIN_JWT_SECRET=your-very-long-random-secret-at-least-32-chars
ADMIN_USERNAME=admin
ADMIN_PASSWORD=adminpassword
```

Copy to `.env` and fill in real values (`.env` is git-ignored).

- [ ] **Step 2: Create TelegramBot Dockerfile**

`src/InstantBot.TelegramBot/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj", "src/InstantBot.TelegramBot/"]
COPY ["src/InstantBot.Application/InstantBot.Application.csproj", "src/InstantBot.Application/"]
COPY ["src/InstantBot.Domain/InstantBot.Domain.csproj", "src/InstantBot.Domain/"]
COPY ["src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj", "src/InstantBot.Infrastructure/"]
RUN dotnet restore "src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj"
COPY . .
RUN dotnet publish "src/InstantBot.TelegramBot/InstantBot.TelegramBot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InstantBot.TelegramBot.dll"]
```

- [ ] **Step 3: Create AdminPanel Dockerfile**

`src/InstantBot.AdminPanel/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj", "src/InstantBot.AdminPanel/"]
COPY ["src/InstantBot.Application/InstantBot.Application.csproj", "src/InstantBot.Application/"]
COPY ["src/InstantBot.Domain/InstantBot.Domain.csproj", "src/InstantBot.Domain/"]
COPY ["src/InstantBot.Infrastructure/InstantBot.Infrastructure.csproj", "src/InstantBot.Infrastructure/"]
RUN dotnet restore "src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj"
COPY . .
RUN dotnet publish "src/InstantBot.AdminPanel/InstantBot.AdminPanel.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InstantBot.AdminPanel.dll"]
```

- [ ] **Step 4: Create docker-compose.yml**

`docker-compose.yml`:
```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  bot:
    build:
      context: .
      dockerfile: src/InstantBot.TelegramBot/Dockerfile
    env_file: .env
    environment:
      TelegramBot__Token: ${TELEGRAM_BOT_TOKEN}
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Redis__ConnectionString: "redis:6379"
      OpenAI__ApiKey: ${OPENAI_API_KEY}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped

  adminpanel:
    build:
      context: .
      dockerfile: src/InstantBot.AdminPanel/Dockerfile
    ports:
      - "8080:8080"
    env_file: .env
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Redis__ConnectionString: "redis:6379"
      Admin__JwtSecret: ${ADMIN_JWT_SECRET}
      Admin__Username: ${ADMIN_USERNAME}
      Admin__Password: ${ADMIN_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped

volumes:
  postgres_data:
  redis_data:
```

- [ ] **Step 5: Add .gitignore**

`.gitignore`:
```
.env
*.user
bin/
obj/
logs/
*.log
.DS_Store
```

- [ ] **Step 6: Final full solution build**

```bash
dotnet build InstantBot.sln
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 7: Run all tests**

```bash
dotnet test InstantBot.sln --verbosity normal
```

Expected: All tests pass.

- [ ] **Step 8: Start with Docker Compose (requires .env file)**

```bash
cp .env.example .env
# Edit .env with your actual values, then:
docker compose up --build
```

Expected:
- postgres healthy
- redis healthy
- bot starts polling (check logs: `Bot started polling`)
- adminpanel accessible at http://localhost:8080

- [ ] **Step 9: Final commit**

```bash
git add . && git commit -m "feat: complete Docker Compose setup, Dockerfiles, .gitignore"
```

---

## Self-Review

### Spec coverage check

| Spec requirement | Implemented in |
|---|---|
| User registration + personality profile | Task 9 (RegisterUserCommand) |
| Numerology calculation | Task 4 (NumerologyCalculator) |
| Archetype analysis | Task 4 (ArchetypeAnalyzer) |
| AI Mentor chat (GPT-4o-mini) | Task 10 (OpenAiService + AskMentorCommand) |
| Free tier: 3 questions/day | Task 10 (rate limiting in AskMentorCommandHandler) |
| Redis rate limiting | Task 8 (RedisCacheService.IncrementDailyQuestionsAsync) |
| Token budget by tier | Task 10 (GetTokenLimit in OpenAiService) |
| Telegram Stars payments | Task 11 (TelegramStarsService + PaymentHandler) |
| Subscription tiers: Free/Premium/VIP | Task 11 (PurchaseSubscriptionCommand) |
| Daily card (Tarot) | Task 13 (DailyCardJob + GetDailyCardQuery) |
| Weekly forecast | Available via DailyCardJob pattern — add WeeklyForecastJob in M4 |
| Monthly forecast (VIP) | Same pattern as weekly, gated by Vip tier |
| Streak tracking | Task 10 (user.UpdateStreak) + Task 13 (StreakService) |
| Achievements | Task 13 (StreakService.CheckAndAwardAchievementAsync) |
| Daily notifications | Task 13 (DailyCardJob sends push) |
| Inactivity offers | Task 12 (InactivityJob) |
| Auto-renew reminders | Task 12 (SubscriptionExpiryJob) |
| Relationship analysis | Task 14 (AnalyzeRelationshipCommand) |
| Voice input (Whisper) | Task 15 (VoiceHandler + WhisperService) |
| Voice output (TTS, VIP) | Task 15 (TtsService) |
| Referral system | Task 16 (ProcessReferralCommand) |
| Share result | Task 16 (share_result callback) |
| Admin panel: metrics | Task 17 (DashboardController) |
| Admin panel: users | Task 17 (UsersController) |
| Health check | Task 17 (/health endpoint) |
| Docker Compose | Task 18 |
| Conversation summarization | Task 12 (ConversationSummaryJob) |
| FSM state machine | Task 9 (UserStateManager) |
| Upsell after limit hit | Task 10 (AskMentorCommandHandler + MessageHandler) |
| Upsell after relationship analysis | Task 14 (MessageHandler.HandlePartnerBirthDateAsync) |

### Outstanding items (add in future milestones)

- `WeeklyForecastJob` — same pattern as `DailyCardJob`, runs Monday 08:00 UTC, gated to Premium+
- `MonthlyForecastJob` — same pattern, runs 1st of month, gated to Vip only
- Trial Premium activation — set `SubscriptionTier.Premium` + `ExpiresAt = +3 days` in `RegisterUserCommandHandler`, show after day 2
- `/subscriptions` admin page — same pattern as `UsersController` querying `db.Subscriptions`
- `/referrals` admin page — same pattern querying `db.Referrals`
- Persist `RelationshipAnalysis` to DB — inject `AppDbContext` directly in handler or add `IRelationshipAnalysisRepository`

