using InstantBot.Application.Interfaces;
using MediatR;

namespace InstantBot.Application.Commands.PurchaseSubscription;

public class PurchaseSubscriptionCommandHandler(IUserRepository userRepo)
    : IRequestHandler<PurchaseSubscriptionCommand>
{
    public async Task Handle(PurchaseSubscriptionCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct)
            ?? throw new InvalidOperationException("User not found");

        var months    = (int)cmd.Plan;
        var expiresAt = user.SubscriptionExpiresAt.HasValue && user.SubscriptionExpiresAt > DateTime.UtcNow
            ? user.SubscriptionExpiresAt.Value.AddMonths(months)
            : DateTime.UtcNow.AddMonths(months);

        user.UpdateSubscription(cmd.Tier, expiresAt);
        await userRepo.SaveChangesAsync(ct);
    }
}
