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
