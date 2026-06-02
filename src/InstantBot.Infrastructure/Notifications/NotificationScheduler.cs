using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class NotificationScheduler(IServiceProvider services, ILogger<NotificationScheduler> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now.Minute == 0)
                {
                    if (now.Hour == 8)  await RunJobAsync<DailyCardJob>(j => j.RunAsync(ct), ct);
                    if (now.Hour == 10) await RunJobAsync<InactivityJob>(j => j.RunAsync(ct), ct);
                    if (now.Hour % 6 == 0) await RunJobAsync<SubscriptionExpiryJob>(j => j.RunAsync(ct), ct);
                    await RunJobAsync<ConversationSummaryJob>(j => j.RunAsync(ct), ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "NotificationScheduler error");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task RunJobAsync<T>(Func<T, Task> action, CancellationToken ct) where T : class
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<T>();
            await action(job);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {Job} failed", typeof(T).Name);
        }
    }
}
