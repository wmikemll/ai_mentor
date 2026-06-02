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
