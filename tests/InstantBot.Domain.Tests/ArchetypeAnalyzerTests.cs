using FluentAssertions;
using InstantBot.Application.Services;
using InstantBot.Domain.Enums;

namespace InstantBot.Domain.Tests;

public class ArchetypeAnalyzerTests
{
    [Theory]
    [InlineData(1,  ArchetypeType.Leader)]
    [InlineData(2,  ArchetypeType.Diplomat)]
    [InlineData(9,  ArchetypeType.Sage)]
    [InlineData(11, ArchetypeType.Visionary)]
    [InlineData(22, ArchetypeType.Builder)]
    public void GetArchetypeInfo_ReturnsCorrectArchetype(int lifePathNumber, ArchetypeType expected)
    {
        var info = ArchetypeAnalyzer.GetArchetypeInfo(lifePathNumber);
        info.Archetype.Should().Be(expected);
        info.Strengths.Should().NotBeEmpty();
        info.Weaknesses.Should().NotBeEmpty();
        info.Recommendations.Should().NotBeEmpty();
    }
}
