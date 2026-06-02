using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(u => u.Id);
        b.HasIndex(u => u.TelegramUserId).IsUnique();
        b.HasIndex(u => u.ReferralCode).IsUnique();
        b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        b.Property(u => u.Username).HasMaxLength(100);
        b.Property(u => u.ReferralCode).HasMaxLength(10).IsRequired();
        b.Property(u => u.SubscriptionTier).HasConversion<int>();

        b.HasOne(u => u.PersonalityProfile)
         .WithOne(p => p.User)
         .HasForeignKey<PersonalityProfile>(p => p.UserId);

        b.HasMany(u => u.Conversations)
         .WithOne(c => c.User)
         .HasForeignKey(c => c.UserId);

        b.HasMany(u => u.Subscriptions)
         .WithOne(s => s.User)
         .HasForeignKey(s => s.UserId);

        b.HasMany(u => u.Achievements)
         .WithOne(a => a.User)
         .HasForeignKey(a => a.UserId);
    }
}
