using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Queries.GetDailyCard;

public record GetDailyCardQuery(long TelegramUserId) : IRequest<DailyCard?>;
