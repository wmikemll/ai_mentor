using InstantBot.Application.Interfaces;
using InstantBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class SubscriptionExpiryJob(
    AppDbContext db,
    INotificationService notifier,
    ICacheService cache,
    ILogger<SubscriptionExpiryJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now    = DateTime.UtcNow;
        var in3Days = now.AddDays(3);

        var expiringSoon = await db.Users
            .Where(u => u.SubscriptionExpiresAt.HasValue
                     && u.SubscriptionExpiresAt > now
                     && u.SubscriptionExpiresAt <= in3Days
                     && u.IsActive)
            .ToListAsync(ct);

        foreach (var user in expiringSoon)
        {
            var shown = await cache.IsOfferShownAsync(user.TelegramUserId, "expiry_3d");
            if (shown) continue;
            await notifier.SendTextAsync(user.TelegramUserId,
                "⏰ Твоя подписка истекает через 3 дня. Продли, чтобы не потерять доступ к наставнику! /subscription", ct);
            await cache.MarkOfferShownAsync(user.TelegramUserId, "expiry_3d", TimeSpan.FromDays(2));
            logger.LogInformation("Sent expiry reminder to {UserId}", user.TelegramUserId);
        }

        var expiredToday = await db.Users
            .Where(u => u.SubscriptionExpiresAt.HasValue
                     && u.SubscriptionExpiresAt <= now
                     && u.SubscriptionExpiresAt > now.AddHours(-6)
                     && u.IsActive)
            .ToListAsync(ct);

        foreach (var user in expiredToday)
        {
            await notifier.SendTextAsync(user.TelegramUserId,
                "💔 Твоя подписка истекла. Возвращайся — скидка 10% на продление только сегодня! /subscription", ct);
        }
    }
}
