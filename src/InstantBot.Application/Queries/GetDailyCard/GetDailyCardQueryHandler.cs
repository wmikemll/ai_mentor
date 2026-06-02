using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Queries.GetDailyCard;

public class GetDailyCardQueryHandler(IUserRepository userRepo, IDailyCardRepository cardRepo)
    : IRequestHandler<GetDailyCardQuery, DailyCard?>
{
    public async Task<DailyCard?> Handle(GetDailyCardQuery query, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(query.TelegramUserId, ct);
        if (user is null) return null;
        return await cardRepo.GetForDateAsync(user.Id, DateOnly.FromDateTime(DateTime.UtcNow), ct);
    }
}
