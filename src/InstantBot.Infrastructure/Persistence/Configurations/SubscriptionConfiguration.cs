using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.HasKey(s => s.Id);
        b.HasIndex(s => s.ExpiresAt);
        b.Property(s => s.Plan).HasConversion<int>();
        b.Property(s => s.Tier).HasConversion<int>();
        b.Property(s => s.TelegramPaymentChargeId).HasMaxLength(200);
    }
}
