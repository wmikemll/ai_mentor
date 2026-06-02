using InstantBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.Payments;

namespace InstantBot.Infrastructure.Payments;

public static class TelegramStarsService
{
    private static readonly Dictionary<(SubscriptionTier, SubscriptionPlan), (int Stars, string Title)> Prices = new()
    {
        [(SubscriptionTier.Premium, SubscriptionPlan.OneMonth)]     = (299,  "Premium — 1 месяц"),
        [(SubscriptionTier.Premium, SubscriptionPlan.ThreeMonths)]  = (749,  "Premium — 3 месяца"),
        [(SubscriptionTier.Premium, SubscriptionPlan.TwelveMonths)] = (2490, "Premium — 12 месяцев"),
        [(SubscriptionTier.Vip,     SubscriptionPlan.OneMonth)]     = (599,  "VIP — 1 месяц"),
        [(SubscriptionTier.Vip,     SubscriptionPlan.ThreeMonths)]  = (1499, "VIP — 3 месяца"),
        [(SubscriptionTier.Vip,     SubscriptionPlan.TwelveMonths)] = (4990, "VIP — 12 месяцев"),
    };

    public static async Task SendInvoiceAsync(
        ITelegramBotClient bot, long chatId,
        SubscriptionTier tier, SubscriptionPlan plan, CancellationToken ct)
    {
        if (!Prices.TryGetValue((tier, plan), out var info)) return;

        var payload = $"{tier}:{plan}:{chatId}";
        await bot.SendInvoice(
            chatId: chatId,
            title: info.Title,
            description: GetDescription(tier),
            payload: payload,
            currency: "XTR",
            prices: [new LabeledPrice(info.Title, info.Stars)],
            cancellationToken: ct);
    }

    private static string GetDescription(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Premium => "Безлимитный ИИ-наставник · Анализ отношений · Прогнозы недели",
        SubscriptionTier.Vip    => "Всё из Premium + Голосовые ответы · Прогнозы месяца · Расширенная аналитика",
        _ => ""
    };
}
