using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;

namespace InstantBot.TelegramBot.StateMachine;

public class UserStateManager(ICacheService cache)
{
    public Task<UserBotState> GetStateAsync(long telegramUserId) =>
        cache.GetUserStateAsync(telegramUserId);

    public Task SetStateAsync(long telegramUserId, UserBotState state) =>
        cache.SetUserStateAsync(telegramUserId, state, TimeSpan.FromMinutes(30));

    public Task<string?> GetTempValueAsync(long telegramUserId, string key) =>
        cache.GetSessionValueAsync(telegramUserId, key);

    public Task SetTempValueAsync(long telegramUserId, string key, string value) =>
        cache.SetSessionValueAsync(telegramUserId, key, value, TimeSpan.FromMinutes(30));
}
