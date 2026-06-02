using InstantBot.Domain.Entities;
using MediatR;

namespace InstantBot.Application.Commands.RegisterUser;

public record RegisterUserCommand(
    long TelegramUserId,
    string FirstName,
    string? Username,
    DateOnly BirthDate,
    string? ReferralCode) : IRequest<User>;
