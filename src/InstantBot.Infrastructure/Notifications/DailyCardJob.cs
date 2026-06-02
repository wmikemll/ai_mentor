using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.ValueObjects;
using InstantBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InstantBot.Infrastructure.Notifications;

public class DailyCardJob(
    AppDbContext db,
    IDailyCardRepository cardRepo,
    IOpenAiService ai,
    INotificationService notifier,
    ICacheService cache,
    ILogger<DailyCardJob> logger)
{
    private static readonly TarotCard[] MajorArcana =
    [
        new("Шут",              "Major", "Новые начинания, спонтанность, свобода"),
        new("Маг",              "Major", "Воля, мастерство, умение воплощать задуманное"),
        new("Верховная Жрица",  "Major", "Интуиция, тайное знание, внутренний голос"),
        new("Императрица",      "Major", "Творчество, изобилие, забота"),
        new("Император",        "Major", "Структура, авторитет, стабильность"),
        new("Иерофант",         "Major", "Традиции, духовное руководство, обучение"),
        new("Влюблённые",       "Major", "Выборы, партнёрство, ценности"),
        new("Колесница",        "Major", "Победа, решимость, контроль"),
        new("Сила",             "Major", "Внутренняя сила, мужество, терпение"),
        new("Отшельник",        "Major", "Уединение, поиск пути, мудрость"),
        new("Колесо Фортуны",   "Major", "Циклы, перемены, судьба"),
        new("Справедливость",   "Major", "Равновесие, истина, честность"),
        new("Повешенный",       "Major", "Принятие, новый взгляд, пауза"),
        new("Смерть",           "Major", "Трансформация, окончание, перемены"),
        new("Умеренность",      "Major", "Баланс, терпение, гармония"),
        new("Дьявол",           "Major", "Ограничения, зависимости, материализм"),
        new("Башня",            "Major", "Разрушение иллюзий, внезапные перемены"),
        new("Звезда",           "Major", "Надежда, вдохновение, исцеление"),
        new("Луна",             "Major", "Иллюзии, страхи, подсознание"),
        new("Солнце",           "Major", "Радость, успех, витальность"),
        new("Суд",              "Major", "Возрождение, оценка, пробуждение"),
        new("Мир",              "Major", "Завершение, целостность, достижение"),
    ];

    public async Task RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var users = await db.Users
            .Where(u => u.IsActive)
            .Include(u => u.PersonalityProfile)
            .ToListAsync(ct);

        var rng = new Random();
        foreach (var batch in users.Chunk(50))
        {
            foreach (var user in batch)
            {
                try
                {
                    var existing = await cardRepo.GetForDateAsync(user.Id, today, ct);
                    if (existing is not null) continue;

                    var card   = MajorArcana[rng.Next(MajorArcana.Length)];
                    var text   = await ai.GenerateDailyCardAsync(user, card.Name, card.Meaning, ct);
                    var advice = ExtractSection(text, "СОВЕТ:");
                    var focus  = ExtractSection(text, "ФОКУС:");

                    var dailyCard = DailyCard.Create(user.Id, today, card, advice, focus);
                    await cardRepo.AddAsync(dailyCard, ct);
                    await cardRepo.SaveChangesAsync(ct);

                    var json = System.Text.Json.JsonSerializer.Serialize(new { card.Name, advice, focus });
                    await cache.SetDailyCardCacheAsync(user.TelegramUserId, today, json, TimeSpan.FromHours(24));

                    await notifier.SendTextAsync(user.TelegramUserId,
                        $"🌅 Доброе утро, {user.FirstName}!\n\n🃏 *{card.Name}*\n\n📖 {advice}\n\n🎯 Фокус дня: {focus}", ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "DailyCardJob failed for user {UserId}", user.TelegramUserId);
                }
            }
            await Task.Delay(500, ct);
        }
        logger.LogInformation("DailyCardJob completed for {Count} users", users.Count);
    }

    private static string ExtractSection(string text, string prefix)
    {
        var idx = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return text.Trim();
        var start = idx + prefix.Length;
        var end   = text.IndexOf('\n', start);
        return (end > 0 ? text[start..end] : text[start..]).Trim();
    }
}
