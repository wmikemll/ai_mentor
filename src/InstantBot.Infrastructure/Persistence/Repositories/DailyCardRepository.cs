using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.Infrastructure.Persistence.Repositories;

public class DailyCardRepository(AppDbContext db) : IDailyCardRepository
{
    public Task<DailyCard?> GetForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default) =>
        db.DailyCards.FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, ct);

    public async Task AddAsync(DailyCard card, CancellationToken ct = default) =>
        await db.DailyCards.AddAsync(card, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
