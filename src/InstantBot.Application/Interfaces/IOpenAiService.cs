using InstantBot.Domain.Entities;

namespace InstantBot.Application.Interfaces;

public interface IOpenAiService
{
    Task<(string Response, int TokensUsed)> GetMentorResponseAsync(
        User user, List<Message> history, string userMessage, CancellationToken ct = default);
    Task<string> GenerateDailyCardAsync(
        User user, string cardName, string cardMeaning, CancellationToken ct = default);
    Task<string> GeneratePersonalityProfileAsync(
        string firstName, int lifePathNumber, string archetypeName, CancellationToken ct = default);
    Task<string> GenerateRelationshipAnalysisAsync(
        User user, string partnerName, DateOnly partnerBirth, CancellationToken ct = default);
    Task<string> GenerateSummarySAsync(List<Message> messages, CancellationToken ct = default);
}
