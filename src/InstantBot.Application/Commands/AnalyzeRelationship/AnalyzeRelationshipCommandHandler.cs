using InstantBot.Application.Interfaces;
using MediatR;

namespace InstantBot.Application.Commands.AnalyzeRelationship;

public class AnalyzeRelationshipCommandHandler(
    IUserRepository userRepo,
    IOpenAiService ai) : IRequestHandler<AnalyzeRelationshipCommand, string>
{
    public async Task<string> Handle(AnalyzeRelationshipCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct)
            ?? throw new InvalidOperationException("User not found");
        return await ai.GenerateRelationshipAnalysisAsync(user, cmd.PartnerName, cmd.PartnerBirthDate, ct);
    }
}
