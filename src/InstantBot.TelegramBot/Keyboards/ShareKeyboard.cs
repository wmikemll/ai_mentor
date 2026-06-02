using Telegram.Bot.Types.ReplyMarkups;

namespace InstantBot.TelegramBot.Keyboards;

public static class ShareKeyboard
{
    public static InlineKeyboardMarkup Get(string label) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData($"📤 {label}", "share_result") },
        new[] { InlineKeyboardButton.WithCallbackData("👥 Пригласить друга", "invite_friend") },
    });
}
