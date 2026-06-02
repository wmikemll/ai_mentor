using InstantBot.AdminPanel.Models;
using InstantBot.Domain.Enums;
using InstantBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace InstantBot.AdminPanel.Controllers;

[Authorize]
public class DashboardController(AppDbContext db, IConnectionMultiplexer redis) : Controller
{
    private const string CacheKey = "admin:dashboard";

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cached = await redis.GetDatabase().StringGetAsync(CacheKey);
        if (cached.HasValue)
        {
            var vm = JsonSerializer.Deserialize<DashboardViewModel>(cached.ToString())!;
            return View(vm);
        }

        var model = await BuildMetricsAsync(ct);
        await redis.GetDatabase().StringSetAsync(CacheKey,
            JsonSerializer.Serialize(model), TimeSpan.FromHours(1));
        return View(model);
    }

    private async Task<DashboardViewModel> BuildMetricsAsync(CancellationToken ct)
    {
        var now           = DateTime.UtcNow;
        var today         = DateOnly.FromDateTime(now);
        var thirtyDaysAgo = now.AddDays(-30);

        var totalUsers   = await db.Users.CountAsync(ct);
        var dau          = await db.Users.CountAsync(u => u.LastActiveDate == today, ct);
        var mau          = await db.Users.CountAsync(u => u.LastActiveDate >= DateOnly.FromDateTime(thirtyDaysAgo), ct);
        var premiumCount = await db.Users.CountAsync(u => u.SubscriptionTier == SubscriptionTier.Premium, ct);
        var vipCount     = await db.Users.CountAsync(u => u.SubscriptionTier == SubscriptionTier.Vip, ct);

        var conversionRate = totalUsers > 0
            ? Math.Round((decimal)(premiumCount + vipCount) / totalUsers * 100, 1) : 0;

        var subs = await db.Subscriptions.Where(s => s.PurchasedAt >= thirtyDaysAgo).ToListAsync(ct);
        var mrr  = subs.Sum(s => (decimal)s.StarsPaid / (int)s.Plan);
        var arpu = mau > 0 ? Math.Round(mrr / mau, 2) : 0;

        var newByDay = await db.Users
            .Where(u => u.RegistrationDate >= thirtyDaysAgo)
            .GroupBy(u => DateOnly.FromDateTime(u.RegistrationDate))
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync(ct);

        var starsByDay = subs.GroupBy(s => DateOnly.FromDateTime(s.PurchasedAt))
            .ToDictionary(g => g.Key, g => g.Sum(s => s.StarsPaid));

        return new DashboardViewModel
        {
            Dau = dau, Mau = mau, TotalUsers = totalUsers,
            PremiumUsers = premiumCount, VipUsers = vipCount,
            ConversionRate = conversionRate, Mrr = Math.Round(mrr, 2),
            Arpu = arpu, Ltv = arpu > 0 ? Math.Round(arpu / 0.05m, 2) : 0,
            Last30Days = newByDay.Select(g => new DailyStats(
                g.Date, g.Count,
                starsByDay.TryGetValue(g.Date, out var s) ? s : 0)).ToList()
        };
    }
}
