using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByTelegramIdAsync(long telegramUserId, CancellationToken ct = default) =>
        db.Users.Include(u => u.PersonalityProfile)
                .Include(u => u.Achievements)
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.Include(u => u.PersonalityProfile)
                .Include(u => u.Achievements)
                .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByReferralCodeAsync(string code, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.ReferralCode == code, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
