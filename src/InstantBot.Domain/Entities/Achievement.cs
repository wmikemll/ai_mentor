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
