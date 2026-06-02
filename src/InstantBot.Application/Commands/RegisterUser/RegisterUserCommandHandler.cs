using InstantBot.Application.Interfaces;
using InstantBot.Application.Services;
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Commands.RegisterUser;

public class RegisterUserCommandHandler(IUserRepository userRepo)
    : IRequestHandler<RegisterUserCommand, User>
{
    public async Task<User> Handle(RegisterUserCommand cmd, CancellationToken ct)
    {
        var existing = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct);
        if (existing is not null) return existing;

        var user = User.Create(cmd.TelegramUserId, cmd.FirstName, cmd.Username, cmd.BirthDate);

        if (cmd.ReferralCode is not null)
        {
            var referrer = await userRepo.GetByReferralCodeAsync(cmd.ReferralCode, ct);
            if (referrer is not null)
            {
                user.SetReferredBy(referrer.Id);
                // Grant referrer +7 days Premium bonus
                var bonusExpiry = referrer.SubscriptionExpiresAt.HasValue && referrer.SubscriptionExpiresAt > DateTime.UtcNow
                    ? referrer.SubscriptionExpiresAt.Value.AddDays(7)
                    : DateTime.UtcNow.AddDays(7);
                var bonusTier = referrer.SubscriptionTier == Domain.Enums.SubscriptionTier.Free
                    ? Domain.Enums.SubscriptionTier.Premium
                    : referrer.SubscriptionTier;
                referrer.UpdateSubscription(bonusTier, bonusExpiry);
            }
        }

        var profile = PersonalityProfileBuilder.Build(user.Id, cmd.FirstName, cmd.BirthDate);
        user.AttachProfile(profile);

        await userRepo.AddAsync(user, ct);
        await userRepo.SaveChangesAsync(ct);
        return user;
    }
}
