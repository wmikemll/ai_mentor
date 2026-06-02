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
