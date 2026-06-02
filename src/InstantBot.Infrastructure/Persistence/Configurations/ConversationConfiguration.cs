using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstantBot.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.HasKey(c => c.Id);
        b.HasIndex(c => new { c.UserId, c.LastMessageAt });
        b.Property(c => c.Summary).HasMaxLength(2000);

        b.HasMany(c => c.Messages)
         .WithOne(m => m.Conversation)
         .HasForeignKey(m => m.ConversationId);
    }
}
