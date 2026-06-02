using FluentAssertions;
using InstantBot.Application.Commands.AskMentor;
using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using Moq;

namespace InstantBot.Application.Tests;

public class AskMentorCommandHandlerTests
{
    private static User MakeUser(SubscriptionTier tier = SubscriptionTier.Free)
    {
        var u = User.Create(1L, "Тест", null, new DateOnly(1990, 1, 1));
        if (tier != SubscriptionTier.Free)
            u.UpdateSubscription(tier, DateTime.UtcNow.AddMonths(1));
        return u;
    }

    [Fact]
    public async Task Handle_FreeUser_UnderLimit_ReturnsResponse()
    {
        var user = MakeUser();
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var conv = Conversation.Create(user.Id);
        var convRepo = new Mock<IConversationRepository>();
        convRepo.Setup(r => r.GetActiveAsync(user.Id, default)).ReturnsAsync(conv);
        convRepo.Setup(r => r.GetRecentMessagesAsync(conv.Id, It.IsAny<int>(), default))
                .ReturnsAsync(new List<Message>());

        var ai = new Mock<IOpenAiService>();
        ai.Setup(a => a.GetMentorResponseAsync(user, It.IsAny<List<Message>>(), "тест", default))
          .ReturnsAsync(("Ответ наставника", 50));

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.IncrementDailyQuestionsAsync(1L)).ReturnsAsync(1);

        var handler = new AskMentorCommandHandler(userRepo.Object, convRepo.Object, ai.Object);
        var result  = await handler.Handle(new AskMentorCommand(1L, "тест"), default);

        result.Response.Should().Be("Ответ наставника");
        result.LimitReached.Should().BeFalse();
        result.QuestionsLeft.Should().Be(2);
    }

    [Fact]
    public async Task Handle_FreeUser_OverLimit_ReturnsLimitReached()
    {
        var user = MakeUser();
        user.IncrementDailyQuestions();
        user.IncrementDailyQuestions();
        user.IncrementDailyQuestions();

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var handler = new AskMentorCommandHandler(
            userRepo.Object, Mock.Of<IConversationRepository>(),
            Mock.Of<IOpenAiService>());

        var result = await handler.Handle(new AskMentorCommand(1L, "тест"), default);

        result.LimitReached.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PremiumUser_AlwaysAnswers()
    {
        var user = MakeUser(SubscriptionTier.Premium);
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByTelegramIdAsync(1L, default)).ReturnsAsync(user);

        var conv = Conversation.Create(user.Id);
        var convRepo = new Mock<IConversationRepository>();
        convRepo.Setup(r => r.GetActiveAsync(user.Id, default)).ReturnsAsync(conv);
        convRepo.Setup(r => r.GetRecentMessagesAsync(conv.Id, It.IsAny<int>(), default))
                .ReturnsAsync(new List<Message>());

        var ai = new Mock<IOpenAiService>();
        ai.Setup(a => a.GetMentorResponseAsync(user, It.IsAny<List<Message>>(), It.IsAny<string>(), default))
          .ReturnsAsync(("Premium ответ", 100));

        var handler = new AskMentorCommandHandler(userRepo.Object, convRepo.Object, ai.Object);
        var result  = await handler.Handle(new AskMentorCommand(1L, "вопрос"), default);

        result.LimitReached.Should().BeFalse();
        result.QuestionsLeft.Should().Be(int.MaxValue);
    }
}
