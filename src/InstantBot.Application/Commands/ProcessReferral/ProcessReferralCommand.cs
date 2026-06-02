using MediatR;
namespace InstantBot.Application.Commands.ProcessReferral;

public record ProcessReferralCommand(Guid ReferrerId, Guid NewUserId) : IRequest;
