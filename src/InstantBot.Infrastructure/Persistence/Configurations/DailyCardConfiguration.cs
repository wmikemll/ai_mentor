using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class DailyCardConfiguration : IEntityTypeConfiguration<DailyCard>
{
    public void Configure(EntityTypeBuilder<DailyCard> b)
    {
        b.HasKey(d => d.Id);
        b.HasIndex(d => new { d.UserId, d.Date }).IsUnique();
        b.OwnsOne(d => d.TarotCard, card =>
        {
            card.Property(c => c.Name).HasColumnName("tarot_name").HasMaxLength(100);
            card.Property(c => c.Arcana).HasColumnName("tarot_arcana").HasMaxLength(50);
            card.Property(c => c.Meaning).HasColumnName("tarot_meaning").HasMaxLength(500);
        });
        b.Property(d => d.DayAdvice).HasColumnType("text");
        b.Property(d => d.DayFocus).HasMaxLength(300);
    }
}
