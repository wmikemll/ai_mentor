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
