using InstantBot.Domain.Enums;
using MediatR;

namespace InstantBot.Application.Commands.PurchaseSubscription;

public record PurchaseSubscriptionCommand(
    long TelegramUserId,
    SubscriptionPlan Plan,
    SubscriptionTier Tier,
    int StarsPaid,
    string TelegramPaymentChargeId) : IRequest;
