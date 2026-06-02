using FluentAssertions;
using InstantBot.Application.Services;

namespace InstantBot.Domain.Tests;

public class NumerologyCalculatorTests
{
    // Verified math:
    // 1990/5/15: day=1+5=6, month=5, year=1+9+9+0=19, sum=30 → 3+0=3
    // 2000/1/1:  day=1,     month=1, year=2+0+0+0=2,  sum=4  → 4
    // 1985/3/2:  day=2,     month=3, year=1+9+8+5=23→5, sum=10 → 1
    [Theory]
    [InlineData(1990, 5, 15, 3)]
    [InlineData(2000, 1, 1,  4)]
    [InlineData(1985, 3, 2,  1)]
    public void CalculateLifePathNumber_ReturnsExpected(int year, int month, int day, int expected)
    {
        NumerologyCalculator.CalculateLifePathNumber(new DateOnly(year, month, day))
            .Should().Be(expected);
    }

    [Fact]
    public void MasterNumbers_NotReduced()
    {
        NumerologyCalculator.ReduceToSingleDigitPublic(11).Should().Be(11);
        NumerologyCalculator.ReduceToSingleDigitPublic(22).Should().Be(22);
    }

    [Fact]
    public void NonMasterNumber_IsReduced()
    {
        NumerologyCalculator.ReduceToSingleDigitPublic(33).Should().Be(6);
        NumerologyCalculator.ReduceToSingleDigitPublic(29).Should().Be(2);
        NumerologyCalculator.ReduceToSingleDigitPublic(30).Should().Be(3);
    }
}
