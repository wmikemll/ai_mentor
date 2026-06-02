using Telegram.Bot.Types.ReplyMarkups;

namespace InstantBot.TelegramBot.Keyboards;

public static class SubscriptionKeyboard
{
    public static InlineKeyboardMarkup GetUpsell() => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("⭐ Купить Premium",            "sub_premium_1mo") },
        new[] { InlineKeyboardButton.WithCallbackData("💎 Посмотреть все тарифы",     "subscription") },
    });

    public static InlineKeyboardMarkup GetPlans() => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("Premium — 299 ⭐/мес",    "sub_premium_1mo")  },
        new[] { InlineKeyboardButton.WithCallbackData("Premium — 749 ⭐/3 мес",  "sub_premium_3mo")  },
        new[] { InlineKeyboardButton.WithCallbackData("Premium — 2490 ⭐/год",   "sub_premium_12mo") },
        new[] { InlineKeyboardButton.WithCallbackData("VIP — 599 ⭐/мес",        "sub_vip_1mo")      },
        new[] { InlineKeyboardButton.WithCallbackData("VIP — 1499 ⭐/3 мес",     "sub_vip_3mo")      },
        new[] { InlineKeyboardButton.WithCallbackData("VIP — 4990 ⭐/год",       "sub_vip_12mo")     },
    });
}
