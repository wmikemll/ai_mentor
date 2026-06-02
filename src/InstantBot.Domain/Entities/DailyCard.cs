using InstantBot.Domain.ValueObjects;
namespace InstantBot.Domain.Entities;

public sealed class DailyCard
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public TarotCard TarotCard { get; private set; } = TarotCard.Empty;
    public string DayAdvice { get; private set; } = null!;
    public string DayFocus { get; private set; } = null!;
    public bool IsViewed { get; private set; }
    public User User { get; private set; } = null!;

    private DailyCard() { }

    public static DailyCard Create(Guid userId, DateOnly date, TarotCard card, string advice, string focus) =>
        new() { Id = Guid.NewGuid(), UserId = userId, Date = date,
                TarotCard = card, DayAdvice = advice, DayFocus = focus };

    public void MarkViewed() => IsViewed = true;
}
