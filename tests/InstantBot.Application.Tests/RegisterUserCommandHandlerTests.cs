using FluentAssertions;
using InstantBot.Application.Commands.RegisterUser;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using Moq;

namespace InstantBot.Application.Tests;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewUser_CreatesUserWithProfile()
    {
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByTelegramIdAsync(It.IsAny<long>(), default))
                .ReturnsAsync((User?)null);
        repoMock.Setup(r => r.GetByReferralCodeAsync(It.IsAny<string>(), default))
                .ReturnsAsync((User?)null);

        var handler = new RegisterUserCommandHandler(repoMock.Object);
        var cmd = new RegisterUserCommand(123L, "Иван", null, new DateOnly(1990, 5, 15), null);

        var result = await handler.Handle(cmd, default);

        result.TelegramUserId.Should().Be(123L);
        result.PersonalityProfile.Should().NotBeNull();
        result.PersonalityProfile!.LifePathNumber.Should().BeGreaterThan(0);
        repoMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsExistingWithoutCreating()
    {
        var existing = User.Create(123L, "Иван", null, new DateOnly(1990, 5, 15));
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByTelegramIdAsync(123L, default)).ReturnsAsync(existing);

        var handler = new RegisterUserCommandHandler(repoMock.Object);
        var result = await handler.Handle(new RegisterUserCommand(123L, "Иван", null, new DateOnly(1990, 5, 15), null), default);

        result.Should().Be(existing);
        repoMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Never);
    }
}
