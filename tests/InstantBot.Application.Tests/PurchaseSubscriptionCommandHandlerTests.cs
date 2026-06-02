using FluentAssertions;
using InstantBot.Application.Commands.PurchaseSubscription;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using Moq;

namespace InstantBot.Application.Tests;

public class PurchaseSubscriptionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidPayment_UpgradesUserSubscription()
    {
        var user = User.Create(1L, "Иван", null, new DateOnly(1990, 1, 1));
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var handler = new PurchaseSubscriptionCommandHandler(userRepo.Object);
        await handler.Handle(
            new PurchaseSubscriptionCommand(1L, SubscriptionPlan.OneMonth, SubscriptionTier.Premium, 299, "charge_abc"),
            default);

        user.SubscriptionTier.Should().Be(SubscriptionTier.Premium);
        user.SubscriptionExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMonths(1), TimeSpan.FromSeconds(5));
        userRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExtendExistingSubscription_AddsMonths()
    {
        var user = User.Create(1L, "Иван", null, new DateOnly(1990, 1, 1));
        var futureExpiry = DateTime.UtcNow.AddMonths(2);
        user.UpdateSubscription(SubscriptionTier.Premium, futureExpiry);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var handler = new PurchaseSubscriptionCommandHandler(userRepo.Object);
        await handler.Handle(
            new PurchaseSubscriptionCommand(1L, SubscriptionPlan.OneMonth, SubscriptionTier.Premium, 299, "charge_xyz"),
            default);

        user.SubscriptionExpiresAt.Should().BeCloseTo(futureExpiry.AddMonths(1), TimeSpan.FromSeconds(5));
    }
}
