using InstantBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.AdminPanel.Controllers;

[Authorize]
public class UsersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1, string? tier = null, CancellationToken ct = default)
    {
        var query = db.Users.Include(u => u.PersonalityProfile).AsQueryable();
        if (!string.IsNullOrEmpty(tier) &&
            Enum.TryParse<Domain.Enums.SubscriptionTier>(tier, out var t))
            query = query.Where(u => u.SubscriptionTier == t);

        var total = await query.CountAsync(ct);
        var users = await query.OrderByDescending(u => u.RegistrationDate)
            .Skip((page - 1) * 50).Take(50).ToListAsync(ct);

        ViewBag.Total = total;
        ViewBag.Page  = page;
        ViewBag.Tier  = tier;
        return View(users);
    }
}
