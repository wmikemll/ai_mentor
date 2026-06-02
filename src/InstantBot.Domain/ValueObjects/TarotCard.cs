namespace InstantBot.Domain.ValueObjects;
public sealed record TarotCard(string Name, string Arcana, string Meaning)
{
    public static TarotCard Empty => new("Неизвестно", "Major", "");
}
