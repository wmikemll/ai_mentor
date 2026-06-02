using InstantBot.Domain.Enums;

namespace InstantBot.Application.Interfaces;

public interface ICacheService
{
    Task<UserBotState> GetUserStateAsync(long telegramUserId);
    Task SetUserStateAsync(long telegramUserId, UserBotState state, TimeSpan? ttl = null);
    Task<int> IncrementDailyQuestionsAsync(long telegramUserId);
    Task<string?> GetSessionValueAsync(long telegramUserId, string key);
    Task SetSessionValueAsync(long telegramUserId, string key, string value, TimeSpan? ttl = null);
    Task<bool> IsOfferShownAsync(long telegramUserId, string offerType);
    Task MarkOfferShownAsync(long telegramUserId, string offerType, TimeSpan ttl);
    Task<string?> GetDailyCardCacheAsync(long telegramUserId, DateOnly date);
    Task SetDailyCardCacheAsync(long telegramUserId, DateOnly date, string json, TimeSpan ttl);
}
