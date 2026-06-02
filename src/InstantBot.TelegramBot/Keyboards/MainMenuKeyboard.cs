using Telegram.Bot.Types.ReplyMarkups;

namespace InstantBot.TelegramBot.Keyboards;

public static class MainMenuKeyboard
{
    public static InlineKeyboardMarkup Get() => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("🔮 Спросить наставника", "ask_mentor"),
                InlineKeyboardButton.WithCallbackData("💫 Карта дня",           "daily_card") },
        new[] { InlineKeyboardButton.WithCallbackData("❤️ Анализ отношений",    "relationship"),
                InlineKeyboardButton.WithCallbackData("⭐ Подписка",            "subscription") },
        new[] { InlineKeyboardButton.WithCallbackData("👤 Мой профиль",         "my_profile") }
    });
}
