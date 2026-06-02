using InstantBot.TelegramBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace InstantBot.TelegramBot;

public class BotPollingService(
    ITelegramBotClient bot,
    IServiceProvider services,
    ILogger<BotPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var opts = new ReceiverOptions { AllowedUpdates = [] };
        bot.StartReceiving(
            async (_, update, token) =>
            {
                await using var scope = services.CreateAsyncScope();
                var router = scope.ServiceProvider.GetRequiredService<UpdateRouter>();
                await router.RouteAsync(update, token);
            },
            (_, ex, _) =>
            {
                logger.LogError(ex, "Telegram polling error");
                return Task.CompletedTask;
            },
            opts, ct);

        logger.LogInformation("Bot started polling");
        await Task.Delay(Timeout.Infinite, ct);
    }
}
