using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using MediatR;

namespace InstantBot.Application.Commands.ProcessReferral;

public class ProcessReferralCommandHandler(
    IUserRepository userRepo,
    INotificationService notifier) : IRequestHandler<ProcessReferralCommand>
{
    public async Task Handle(ProcessReferralCommand cmd, CancellationToken ct)
    {
        var referrer = await userRepo.GetByIdAsync(cmd.ReferrerId, ct);
        var newUser  = await userRepo.GetByIdAsync(cmd.NewUserId, ct);
        if (referrer is null || newUser is null) return;

        var bonusExpiry = referrer.SubscriptionExpiresAt.HasValue && referrer.SubscriptionExpiresAt > DateTime.UtcNow
            ? referrer.SubscriptionExpiresAt.Value.AddDays(7)
            : DateTime.UtcNow.AddDays(7);

        var tier = referrer.SubscriptionTier == SubscriptionTier.Free
            ? SubscriptionTier.Premium
            : referrer.SubscriptionTier;
        referrer.UpdateSubscription(tier, bonusExpiry);

        if (referrer.Achievements.All(a => a.Type != AchievementType.FirstReferral))
            referrer.Achievements.Add(Achievement.Create(referrer.Id, AchievementType.FirstReferral));

        await userRepo.SaveChangesAsync(ct);

        await notifier.SendTextAsync(referrer.TelegramUserId,
            $"🎉 Твой друг {newUser.FirstName} присоединился по твоей ссылке!\n+7 дней Premium добавлено к твоей подписке.", ct);
    }
}
