using InstantBot.Application.Interfaces;
using InstantBot.Domain.Entities;
using InstantBot.Domain.Enums;

namespace InstantBot.Application.Services;

public class StreakService(IUserRepository userRepo)
{
    public async Task CheckAndAwardAchievementsAsync(User user, CancellationToken ct)
    {
        var milestones = new (int Days, AchievementType Type)[]
        {
            (7,  AchievementType.Streak7),
            (30, AchievementType.Streak30),
        };

        foreach (var (days, achievementType) in milestones)
        {
            if (user.CurrentStreak >= days &&
                user.Achievements.All(a => a.Type != achievementType))
            {
                user.Achievements.Add(Achievement.Create(user.Id, achievementType));
            }
        }
        await userRepo.SaveChangesAsync(ct);
    }
}
