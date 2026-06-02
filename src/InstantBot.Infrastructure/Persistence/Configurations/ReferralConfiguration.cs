using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> b)
    {
        b.HasKey(r => r.Id);
        b.HasOne(r => r.Referrer).WithMany()
         .HasForeignKey(r => r.ReferrerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Referred).WithMany()
         .HasForeignKey(r => r.ReferredId).OnDelete(DeleteBehavior.Restrict);
    }
}
