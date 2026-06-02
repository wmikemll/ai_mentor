using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;

namespace InstantBot.Application.Services;

public static class PersonalityProfileBuilder
{
    public static PersonalityProfile Build(Guid userId, string firstName, DateOnly birthDate)
    {
        var lifePath = NumerologyCalculator.CalculateLifePathNumber(birthDate);
        var destiny  = NumerologyCalculator.CalculateDestinyNumber(firstName);
        var soul     = NumerologyCalculator.CalculateSoulNumber(firstName);
        var archInfo = ArchetypeAnalyzer.GetArchetypeInfo(lifePath);

        return PersonalityProfile.Create(userId, lifePath, destiny, soul,
            archInfo.Archetype, archInfo.Strengths, archInfo.Weaknesses, archInfo.Recommendations);
    }
}
