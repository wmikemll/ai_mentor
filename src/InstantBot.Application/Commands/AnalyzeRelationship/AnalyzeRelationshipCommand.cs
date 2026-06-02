using MediatR;
namespace InstantBot.Application.Commands.AnalyzeRelationship;

public record AnalyzeRelationshipCommand(
    long TelegramUserId,
    string PartnerName,
    DateOnly PartnerBirthDate) : IRequest<string>;
