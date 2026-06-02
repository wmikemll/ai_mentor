using InstantBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstantBot.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PersonalityProfile> PersonalityProfiles => Set<PersonalityProfile>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<DailyCard> DailyCards => Set<DailyCard>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<RelationshipAnalysis> RelationshipAnalyses => Set<RelationshipAnalysis>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<Achievement> Achievements => Set<Achievement>();

    protected override void OnModelCreating(ModelBuilder mb)
        => mb.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
