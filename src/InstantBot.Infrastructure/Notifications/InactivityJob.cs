using InstantBot.Application.Interfaces;
using InstantBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class InactivityJob(
    AppDbContext db,
    INotificationService notifier,
    ICacheService cache,
    ILogger<InactivityJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var today   = DateOnly.FromDateTime(DateTime.UtcNow);
        var day3Ago = today.AddDays(-3);
        var day7Ago = today.AddDays(-7);

        var inactive3 = await db.Users
            .Where(u => u.LastActiveDate == day3Ago && u.IsActive)
            .ToListAsync(ct);

        foreach (var user in inactive3)
        {
            var shown = await cache.IsOfferShownAsync(user.TelegramUserId, "inactivity_3d");
            if (shown) continue;
            await notifier.SendTextAsync(user.TelegramUserId,
                $"Привет, {user.FirstName}! 👋 Скучаем по тебе.\n\nСегодня скидка 15% на Premium!", ct);
            await cache.MarkOfferShownAsync(user.TelegramUserId, "inactivity_3d", TimeSpan.FromDays(3));
        }

        var inactive7 = await db.Users
            .Where(u => u.LastActiveDate == day7Ago && u.IsActive)
            .ToListAsync(ct);

        foreach (var user in inactive7)
        {
            var shown = await cache.IsOfferShownAsync(user.TelegramUserId, "inactivity_7d");
            if (shown) continue;
            await notifier.SendTextAsync(user.TelegramUserId,
                $"{user.FirstName}, прошла неделя! 🌙\n\nВернись — скидка 25% на Premium + бонус.", ct);
            await cache.MarkOfferShownAsync(user.TelegramUserId, "inactivity_7d", TimeSpan.FromDays(7));
        }

        logger.LogInformation("InactivityJob: {C3} + {C7} messages sent", inactive3.Count, inactive7.Count);
    }
}
