using FluentAssertions;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;

namespace InstantBot.Domain.Tests;

public class UserEntityTests
{
    [Fact]
    public void Create_InitializesWithFreeSubscriptionAndStreak1()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.SubscriptionTier.Should().Be(SubscriptionTier.Free);
        user.DailyQuestionsUsed.Should().Be(0);
        user.CurrentStreak.Should().Be(1);
        user.ReferralCode.Should().HaveLength(6);
    }

    [Fact]
    public void CanAskQuestion_FreeUser_FalseAfterThreeQuestions()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.IncrementDailyQuestions(); user.IncrementDailyQuestions(); user.IncrementDailyQuestions();
        user.CanAskQuestion().Should().BeFalse();
    }

    [Fact]
    public void CanAskQuestion_PremiumUser_AlwaysTrue()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.UpdateSubscription(SubscriptionTier.Premium, DateTime.UtcNow.AddMonths(1));
        for (var i = 0; i < 10; i++) user.IncrementDailyQuestions();
        user.CanAskQuestion().Should().BeTrue();
    }

    [Fact]
    public void UpdateStreak_ConsecutiveDay_Increments()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.SetLastActiveDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        user.UpdateStreak();
        user.CurrentStreak.Should().Be(2);
    }

    [Fact]
    public void UpdateStreak_GapMoreThanOneDay_ResetsToOne()
    {
        var user = User.Create(123456789L, "Иван", null, new DateOnly(1990, 5, 15));
        user.SetLastActiveDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3));
        user.UpdateStreak();
        user.CurrentStreak.Should().Be(1);
    }
}
