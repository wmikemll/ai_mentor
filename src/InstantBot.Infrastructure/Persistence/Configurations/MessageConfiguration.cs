using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.HasKey(m => m.Id);
        b.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        b.Property(m => m.Role).HasConversion<int>();
        b.Property(m => m.Content).HasColumnType("text").IsRequired();
    }
}
