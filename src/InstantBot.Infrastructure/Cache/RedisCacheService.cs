using InstantBot.Application.Interfaces;
using InstantBot.Domain.Enums;
using StackExchange.Redis;

namespace InstantBot.Infrastructure.Cache;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private IDatabase Db => redis.GetDatabase();

    private static string StateKey(long uid)           => $"state:{uid}";
    private static string RateLimitKey(long uid)       => $"rate:{uid}";
    private static string SessionKey(long uid, string k) => $"session:{uid}:{k}";
    private static string OfferKey(long uid, string t)   => $"offer:{uid}:{t}";
    private static string CardKey(long uid, DateOnly d)  => $"card:{uid}:{d:yyyy-MM-dd}";

    public async Task<UserBotState> GetUserStateAsync(long uid)
    {
        var val = await Db.StringGetAsync(StateKey(uid));
        return val.HasValue && Enum.TryParse<UserBotState>(val!, out var s) ? s : UserBotState.None;
    }

    public Task SetUserStateAsync(long uid, UserBotState state, TimeSpan? ttl = null) =>
        Db.StringSetAsync(StateKey(uid), state.ToString(), ttl ?? TimeSpan.FromMinutes(30));

    public async Task<int> IncrementDailyQuestionsAsync(long uid)
    {
        var key = RateLimitKey(uid);
        var count = await Db.StringIncrementAsync(key);
        if (count == 1)
        {
            var midnight = DateTime.UtcNow.Date.AddDays(1);
            await Db.KeyExpireAsync(key, midnight - DateTime.UtcNow);
        }
        return (int)count;
    }

    public async Task<string?> GetSessionValueAsync(long uid, string key)
    {
        var val = await Db.StringGetAsync(SessionKey(uid, key));
        return val.HasValue ? val.ToString() : null;
    }

    public Task SetSessionValueAsync(long uid, string key, string value, TimeSpan? ttl = null) =>
        Db.StringSetAsync(SessionKey(uid, key), value, ttl ?? TimeSpan.FromMinutes(30));

    public async Task<bool> IsOfferShownAsync(long uid, string offerType) =>
        await Db.KeyExistsAsync(OfferKey(uid, offerType));

    public Task MarkOfferShownAsync(long uid, string offerType, TimeSpan ttl) =>
        Db.StringSetAsync(OfferKey(uid, offerType), "1", ttl);

    public async Task<string?> GetDailyCardCacheAsync(long uid, DateOnly date)
    {
        var val = await Db.StringGetAsync(CardKey(uid, date));
        return val.HasValue ? val.ToString() : null;
    }

    public Task SetDailyCardCacheAsync(long uid, DateOnly date, string json, TimeSpan ttl) =>
        Db.StringSetAsync(CardKey(uid, date), json, ttl);
}
