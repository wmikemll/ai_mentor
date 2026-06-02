using InstantBot.Application.Commands.AskMentor;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;
using InstantBot.TelegramBot.Keyboards;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InstantBot.TelegramBot.Handlers;

public class VoiceHandler(
    ITelegramBotClient bot,
    ISender mediator,
    IUserRepository userRepo,
    IWhisperService whisper,
    ITtsService tts)
{
    public async Task HandleAsync(Message msg, CancellationToken ct)
    {
        var uid  = msg.From!.Id;
        var user = await userRepo.GetByTelegramIdAsync(uid, ct);

        if (user?.SubscriptionTier != SubscriptionTier.Vip)
        {
            await bot.SendMessage(uid,
                "🎤 Голосовые сообщения доступны только в тарифе VIP 💎",
                replyMarkup: SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
            return;
        }

        var voice = msg.Voice!;
        await bot.SendChatAction(uid, ChatAction.RecordVoice, cancellationToken: ct);

        // Download voice file
        var fileInfo = await bot.GetFile(voice.FileId, ct);
        using var audioStream = new MemoryStream();
        await bot.DownloadFile(fileInfo.FilePath!, audioStream, ct);
        audioStream.Position = 0;

        // Transcribe with Whisper
        var transcription = await whisper.TranscribeAsync(audioStream, "voice.ogg", ct);

        await bot.SendChatAction(uid, ChatAction.Typing, cancellationToken: ct);

        // Get mentor response
        var result = await mediator.Send(new AskMentorCommand(uid, transcription), ct);
        if (result.LimitReached)
        {
            await bot.SendMessage(uid, "Достигнут дневной лимит вопросов. 🌙",
                replyMarkup: SubscriptionKeyboard.GetUpsell(), cancellationToken: ct);
            return;
        }

        // Synthesize TTS response
        var mp3 = await tts.SynthesizeAsync(result.Response, ct);
        if (mp3.Length > 0)
        {
            using var mp3Stream = new MemoryStream(mp3);
            var caption = result.Response.Length > 200
                ? result.Response[..200] + "..."
                : result.Response;
            await bot.SendVoice(uid,
                new Telegram.Bot.Types.InputFileStream(mp3Stream, "response.mp3"),
                caption: caption, cancellationToken: ct);
        }
        else
        {
            // TTS unavailable fallback (e.g. no API key)
            await bot.SendMessage(uid, result.Response, cancellationToken: ct);
        }
    }
}
