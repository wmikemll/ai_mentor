using InstantBot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class ConversationSummaryJob(
    IConversationRepository convRepo,
    IOpenAiService ai,
    ILogger<ConversationSummaryJob> logger)
{
    private const int SummarizeThreshold = 20;

    public async Task RunAsync(CancellationToken ct)
    {
        var conversations = await convRepo.GetConversationsToSummarizeAsync(SummarizeThreshold, ct);
        foreach (var conv in conversations)
        {
            try
            {
                var messages = await convRepo.GetRecentMessagesAsync(conv.Id, 30, ct);
                var summary  = await ai.GenerateSummarySAsync(messages, ct);
                conv.UpdateSummary(summary);
                await convRepo.SaveChangesAsync(ct);
                logger.LogInformation("Summarized conversation {Id}", conv.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to summarize conversation {Id}", conv.Id);
            }
        }
    }
}
