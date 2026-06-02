using InstantBot.Domain.Entities;

namespace InstantBot.Application.Interfaces;

public interface IDailyCardRepository
{
    Task<DailyCard?> GetForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(DailyCard card, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
