using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class RelationshipAnalysisConfiguration : IEntityTypeConfiguration<RelationshipAnalysis>
{
    public void Configure(EntityTypeBuilder<RelationshipAnalysis> b)
    {
        b.HasKey(r => r.Id);
        b.HasIndex(r => r.UserId);
        b.Property(r => r.PartnerName).HasMaxLength(100);
        b.Property(r => r.Strengths).HasColumnType("text");
        b.Property(r => r.ConflictZones).HasColumnType("text");
        b.Property(r => r.CommunicationStyle).HasColumnType("text");
        b.Property(r => r.Recommendations).HasColumnType("text");
    }
}
