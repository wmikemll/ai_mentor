using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> b)
    {
        b.HasKey(a => a.Id);
        b.HasIndex(a => new { a.UserId, a.Type }).IsUnique();
        b.Property(a => a.Type).HasConversion<int>();
    }
}
