using InstantBot.Application.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstantBot.Infrastructure.Notifications;

public class TelegramNotificationService(ITelegramBotClient bot) : INotificationService
{
    public Task SendTextAsync(long telegramUserId, string text, CancellationToken ct = default) =>
        bot.SendMessage(telegramUserId, text, cancellationToken: ct);

    public async Task SendVoiceAsync(long telegramUserId, byte[] mp3, string caption, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(mp3);
        await bot.SendVoice(telegramUserId,
            new InputFileStream(stream, "response.mp3"),
            caption: caption, cancellationToken: ct);
    }
}
