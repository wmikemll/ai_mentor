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
