using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class PersonalityProfileConfiguration : IEntityTypeConfiguration<PersonalityProfile>
{
    public void Configure(EntityTypeBuilder<PersonalityProfile> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.ArchetypeType).HasConversion<int>();
        b.Property(p => p.Strengths).HasColumnType("text[]");
        b.Property(p => p.Weaknesses).HasColumnType("text[]");
        b.Property(p => p.Recommendations).HasColumnType("text[]");
    }
}
