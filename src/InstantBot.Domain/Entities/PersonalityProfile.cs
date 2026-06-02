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
