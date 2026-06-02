namespace InstantBot.Application.Interfaces;

public interface INotificationService
{
    Task SendTextAsync(long telegramUserId, string text, CancellationToken ct = default);
    Task SendVoiceAsync(long telegramUserId, byte[] mp3, string caption, CancellationToken ct = default);
}
