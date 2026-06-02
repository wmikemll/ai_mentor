using InstantBot.Application.Commands.PurchaseSubscription;
using InstantBot.Domain.Enums;
using InstantBot.TelegramBot.Keyboards;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;

namespace InstantBot.TelegramBot.Handlers;

public class PaymentHandler(ITelegramBotClient bot, ISender mediator)
{
    public Task HandlePreCheckoutAsync(PreCheckoutQuery query, CancellationToken ct) =>
        bot.AnswerPreCheckoutQuery(query.Id, cancellationToken: ct);

    public async Task HandleSuccessfulPaymentAsync(Message msg, CancellationToken ct)
    {
        var payment = msg.SuccessfulPayment!;
        var parts   = payment.InvoicePayload.Split(':');
        if (parts.Length < 3 ||
            !Enum.TryParse<SubscriptionTier>(parts[0], out var tier) ||
            !Enum.TryParse<SubscriptionPlan>(parts[1], out var plan)) return;

        var uid = msg.From!.Id;
        await mediator.Send(new PurchaseSubscriptionCommand(
            uid, plan, tier, (int)payment.TotalAmount, payment.TelegramPaymentChargeId), ct);

        var tierName = tier == SubscriptionTier.Vip ? "VIP 💎" : "Premium ⭐";
        await bot.SendMessage(uid,
            $"🎉 Подписка {tierName} активирована!\n\nТеперь тебе доступны все возможности наставника.",
            replyMarkup: MainMenuKeyboard.Get(), cancellationToken: ct);
    }
}
