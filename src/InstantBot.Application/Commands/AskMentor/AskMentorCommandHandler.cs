using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;
using MediatR;

namespace InstantBot.Application.Commands.AskMentor;

public class AskMentorCommandHandler(
    IUserRepository userRepo,
    IConversationRepository convRepo,
    IOpenAiService ai) : IRequestHandler<AskMentorCommand, AskMentorResult>
{
    private static readonly Dictionary<SubscriptionTier, int> HistoryLimit = new()
    {
        [SubscriptionTier.Free]    = 5,
        [SubscriptionTier.Premium] = 10,
        [SubscriptionTier.Vip]     = 20,
    };

    public async Task<AskMentorResult> Handle(AskMentorCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByTelegramIdAsync(cmd.TelegramUserId, ct)
            ?? throw new InvalidOperationException("User not found");

        if (!user.CanAskQuestion())
            return new AskMentorResult("", true, 0);

        var conversation = await convRepo.GetActiveAsync(user.Id, ct);
        if (conversation is null)
        {
            conversation = Conversation.Create(user.Id);
            await convRepo.AddAsync(conversation, ct);
        }

        var historyCount = HistoryLimit[user.SubscriptionTier];
        var history = await convRepo.GetRecentMessagesAsync(conversation.Id, historyCount, ct);

        var (response, tokens) = await ai.GetMentorResponseAsync(user, history, cmd.Question, ct);

        await convRepo.AddMessageAsync(Message.Create(conversation.Id, MessageRole.User, cmd.Question), ct);
        await convRepo.AddMessageAsync(Message.Create(conversation.Id, MessageRole.Assistant, response, tokensUsed: tokens), ct);
        conversation.Touch();
        await convRepo.SaveChangesAsync(ct);

        user.IncrementDailyQuestions();
        user.UpdateStreak();
        CheckStreakAchievements(user);
        await userRepo.SaveChangesAsync(ct);

        var questionsLeft = user.SubscriptionTier == SubscriptionTier.Free
            ? Math.Max(0, 3 - user.DailyQuestionsUsed)
            : int.MaxValue;

        return new AskMentorResult(response, false, questionsLeft);
    }

    private static void CheckStreakAchievements(Domain.Entities.User user)
    {
        var milestones = new (int Days, Domain.Enums.AchievementType Type)[]
        {
            (7,  Domain.Enums.AchievementType.Streak7),
            (30, Domain.Enums.AchievementType.Streak30),
        };
        foreach (var (days, type) in milestones)
        {
            if (user.CurrentStreak >= days && user.Achievements.All(a => a.Type != type))
                user.Achievements.Add(Domain.Entities.Achievement.Create(user.Id, type));
        }
    }
}
