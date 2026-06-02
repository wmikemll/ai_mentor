using InstantBot.Application.Interfaces;
using InstantBot.Application.Services;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace InstantBot.Infrastructure.OpenAi;

public class OpenAiService(IConfiguration config) : IOpenAiService
{
    private ChatClient CreateClient() =>
        new("gpt-4o-mini", config["OpenAI:ApiKey"]!);

    public async Task<(string Response, int TokensUsed)> GetMentorResponseAsync(
        User user, List<Message> history, string userMessage, CancellationToken ct = default)
    {
        var client  = CreateClient();
        var profile = user.PersonalityProfile;

        var systemPrompt = $"""
            Ты — мудрый и эмпатичный личный наставник. Твоя задача — помочь человеку разобраться в жизненной ситуации.

            О пользователе:
            Имя: {user.FirstName}
            Возраст: {DateTime.UtcNow.Year - user.BirthDate.Year} лет
            {(profile != null ? $"Число жизненного пути: {profile.LifePathNumber}\nАрхетип: {ArchetypeAnalyzer.GetArchetypeInfo(profile.LifePathNumber).Name}\nСильные стороны: {string.Join(", ", profile.Strengths)}" : "")}

            Правила:
            - Отвечай тепло, конкретно, без шаблонов
            - Используй: "возможно", "один из вариантов", "с точки зрения..."
            - Никогда не предсказывай будущее как факт и не гарантируй результаты
            """;

        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
        foreach (var m in history)
            messages.Add(m.Role == MessageRole.User
                ? new UserChatMessage(m.Content)
                : (ChatMessage)new AssistantChatMessage(m.Content));
        messages.Add(new UserChatMessage(userMessage));

        var opts = new ChatCompletionOptions
        {
            MaxOutputTokenCount = GetTokenLimit(user.SubscriptionTier)
        };

        var completion = await client.CompleteChatAsync(messages, opts, ct);
        return (completion.Value.Content[0].Text, completion.Value.Usage.TotalTokenCount);
    }

    public async Task<string> GenerateDailyCardAsync(
        User user, string cardName, string cardMeaning, CancellationToken ct = default)
    {
        var client = CreateClient();
        var prompt = $"""
            Создай персональный совет на день для {user.FirstName} (число жизненного пути: {user.PersonalityProfile?.LifePathNumber}).
            Карта дня: {cardName} — {cardMeaning}
            Формат (строго):
            СОВЕТ: [1-2 предложения, персональный совет]
            ФОКУС: [1 предложение, на чём сосредоточиться сегодня]
            """;
        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    public async Task<string> GeneratePersonalityProfileAsync(
        string firstName, int lifePathNumber, string archetypeName, CancellationToken ct = default)
    {
        var client = CreateClient();
        var prompt = $"Напиши краткое вдохновляющее описание профиля личности для {firstName}. Число жизненного пути {lifePathNumber}, архетип: {archetypeName}. 2-3 предложения, тёплый тон.";
        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    public async Task<string> GenerateRelationshipAnalysisAsync(
        User user, string partnerName, DateOnly partnerBirth, CancellationToken ct = default)
    {
        var client        = CreateClient();
        var userLifePath  = user.PersonalityProfile?.LifePathNumber
                            ?? NumerologyCalculator.CalculateLifePathNumber(user.BirthDate);
        var partnerPath   = NumerologyCalculator.CalculateLifePathNumber(partnerBirth);

        var prompt = $"""
            Проведи анализ совместимости пары:
            {user.FirstName}: число жизненного пути {userLifePath}
            {partnerName}: число жизненного пути {partnerPath}

            Структура:
            💪 СИЛЬНЫЕ СТОРОНЫ ПАРЫ: [2-3 пункта]
            ⚡ ЗОНЫ КОНФЛИКТОВ: [2-3 пункта]
            💬 СТИЛЬ ОБЩЕНИЯ: [1-2 предложения]
            🌱 РЕКОМЕНДАЦИИ: [2-3 практических совета]

            Используй "возможно", "один из вариантов". Не предсказывай будущее.
            """;
        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    public async Task<string> GenerateSummarySAsync(List<Message> messages, CancellationToken ct = default)
    {
        var client = CreateClient();
        var dialog = string.Join("\n", messages.Select(m =>
            $"{(m.Role == MessageRole.User ? "Пользователь" : "Наставник")}: {m.Content}"));
        var prompt = $"Сделай краткое резюме (3-5 предложений) диалога:\n\n{dialog}";
        var completion = await client.CompleteChatAsync([new UserChatMessage(prompt)], cancellationToken: ct);
        return completion.Value.Content[0].Text;
    }

    private static int GetTokenLimit(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Premium => 600,
        SubscriptionTier.Vip    => 1000,
        _                       => 300
    };
}
