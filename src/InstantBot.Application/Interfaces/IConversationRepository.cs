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
