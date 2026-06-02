using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InstantBot.TelegramBot.Handlers;

public class UpdateRouter(
    MessageHandler messageHandler,
    CallbackHandler callbackHandler,
    PaymentHandler paymentHandler,
    VoiceHandler voiceHandler,
    ILogger<UpdateRouter> logger)
{
    public async Task RouteAsync(Update update, CancellationToken ct)
    {
        try
        {
            await (update.Type switch
            {
                UpdateType.Message when update.Message?.Voice is not null =>
                    voiceHandler.HandleAsync(update.Message, ct),
                UpdateType.Message when update.Message?.SuccessfulPayment is not null =>
                    paymentHandler.HandleSuccessfulPaymentAsync(update.Message, ct),
                UpdateType.Message when update.Message is not null =>
                    messageHandler.HandleAsync(update.Message, ct),
                UpdateType.CallbackQuery when update.CallbackQuery is not null =>
                    callbackHandler.HandleAsync(update.CallbackQuery, ct),
                UpdateType.PreCheckoutQuery when update.PreCheckoutQuery is not null =>
                    paymentHandler.HandlePreCheckoutAsync(update.PreCheckoutQuery, ct),
                _ => Task.CompletedTask
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error routing update {UpdateId}", update.Id);
        }
    }
}
