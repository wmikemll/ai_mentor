namespace InstantBot.Domain.Enums;
public enum UserBotState
{
    None = 0, AwaitingName = 1, AwaitingBirthDate = 2, MainMenu = 3,
    AwaitingMentorQuestion = 4, AwaitingPartnerName = 5,
    AwaitingPartnerBirthDate = 6
}
