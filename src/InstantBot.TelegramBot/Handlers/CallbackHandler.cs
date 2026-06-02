using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;
using InstantBot.TelegramBot.Keyboards;
using InstantBot.TelegramBot.StateMachine;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.TelegramBot.Handlers;

public class CallbackHandler(
    ITelegramBotClient bot,
    ISender mediator,
    IUserRepository userRepo,
    UserStateManager stateManager)
{
    public async Task HandleAsync(CallbackQuery query, CancellationToken ct)
    {
        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
        var uid  = query.From.Id;
        var data = query.Data ?? "";

        switch (data)
        {
            case "ask_mentor":
                await stateManager.SetStateAsync(uid, UserBotState.AwaitingMentorQuestion);
                await bot.SendMessage(uid, "💬 Задай свой вопрос наставнику:", cancellationToken: ct);
                break;

            case "subscription":
                await bot.SendMessage(uid, "Выбери тариф:", replyMarkup: SubscriptionKeyboard.GetPlans(), cancellationToken: ct);
                break;

            case "my_profile":
                var user = await userRepo.GetByTelegramIdAsync(uid, ct);
                if (user?.PersonalityProfile is { } p)
                {
                    var info = $"👤 *Твой профиль*\n\n" +
                               $"Число жизненного пути: *{p.LifePathNumber}*\n" +
                               $"Подписка: *{user.SubscriptionTier}*\n" +
                               $"Серия дней: *{user.CurrentStreak}* 🔥\n" +
                               $"Реферальный код: `{user.ReferralCode}`";
                    await bot.SendMessage(uid, info,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: ct);
                }
                break;

            case "daily_card":
                var card = await mediator.Send(new Application.Queries.GetDailyCard.GetDailyCardQuery(uid), ct);
                if (card is null)
                    await bot.SendMessage(uid,
                        "🌅 Карта дня появляется в 8:00 UTC. Загляни позже!", cancellationToken: ct);
                else
                    await bot.SendMessage(uid,
                        $"🃏 *{card.TarotCard.Name}*\n\n📖 {card.DayAdvice}\n\n🎯 Фокус: {card.DayFocus}",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        replyMarkup: InstantBot.TelegramBot.Keyboards.ShareKeyboard.Get("Поделиться картой дня"),
                        cancellationToken: ct);
                break;

            case "relationship":
                var u = await userRepo.GetByTelegramIdAsync(uid, ct);
                if (u?.SubscriptionTier == SubscriptionTier.Free)
                {
                    await bot.SendMessage(uid, "Анализ отношений доступен в Premium 💫",
                        replyMarkup: SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
                    return;
                }
                await stateManager.SetStateAsync(uid, UserBotState.AwaitingPartnerName);
                await bot.SendMessage(uid, "Введи имя партнёра:", cancellationToken: ct);
                break;

            case string s when s.StartsWith("sub_"):
                await HandleSubscriptionCallbackAsync(uid, s, ct);
                break;

            case "invite_friend":
                var invUser = await userRepo.GetByTelegramIdAsync(uid, ct);
                if (invUser is not null)
                {
                    var botInfo = await bot.GetMe(ct);
                    var link = $"https://t.me/{botInfo.Username}?start=REF_{invUser.ReferralCode}";
                    await bot.SendMessage(uid,
                        $"🔗 Твоя реферальная ссылка:\n{link}\n\n" +
                        $"Поделись с другом — ты получишь +7 дней Premium, а друг — расширенный триал!",
                        cancellationToken: ct);
                }
                break;

            case "share_result":
                await bot.SendMessage(uid,
                    "📤 Скопируй и поделись своим результатом в любом чате!", cancellationToken: ct);
                break;
        }
    }

    private async Task HandleSubscriptionCallbackAsync(long uid, string data, CancellationToken ct)
    {
        // data = "sub_premium_1mo", "sub_vip_3mo", etc.
        var parts = data.Split('_'); // ["sub", "premium", "1mo"]
        if (parts.Length < 3) return;

        var tier = parts[1].Equals("vip", StringComparison.OrdinalIgnoreCase)
            ? SubscriptionTier.Vip : SubscriptionTier.Premium;
        var plan = parts[2] switch
        {
            "3mo"  => SubscriptionPlan.ThreeMonths,
            "12mo" => SubscriptionPlan.TwelveMonths,
            _      => SubscriptionPlan.OneMonth
        };

        await Infrastructure.Payments.TelegramStarsService.SendInvoiceAsync(bot, uid, tier, plan, ct);
    }
}
