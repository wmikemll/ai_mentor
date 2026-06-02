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
