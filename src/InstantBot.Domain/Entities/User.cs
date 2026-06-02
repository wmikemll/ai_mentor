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
