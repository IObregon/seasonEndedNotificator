using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Catalog;
using SeasonEnded.Api.SeasonTracking;
using SeasonEnded.Api.Jobs;
using SeasonEnded.Api.Notifications;

namespace SeasonEnded.Api.Identity;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<RoleChangeAudit> RoleChangeAudits => Set<RoleChangeAudit>();
    public DbSet<Show> Shows => Set<Show>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<ShowFollow> ShowFollows => Set<ShowFollow>();
    public DbSet<SeasonCompletionEvent> SeasonCompletionEvents => Set<SeasonCompletionEvent>();
    public DbSet<JobLease> JobLeases => Set<JobLease>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<DigestDelivery> DigestDeliveries => Set<DigestDelivery>();
    public DbSet<DigestItem> DigestItems => Set<DigestItem>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAuditsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureAuditsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>();
            entity.Property(u => u.Status).IsRequired();
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Email).IsRequired();
            entity.Property(i => i.TokenHash).IsRequired();
            entity.Property(i => i.Role).HasConversion<string>();
            entity.Property(i => i.Status).IsRequired();
        });

        modelBuilder.Entity<MagicLinkToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TokenHash).IsRequired();
            entity.Property(t => t.Status).IsRequired();
        });

        modelBuilder.Entity<RoleChangeAudit>(entity =>
        {
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.PreviousRole).HasConversion<string>();
            entity.Property(audit => audit.NewRole).HasConversion<string>();
        });

        modelBuilder.Entity<Show>(entity =>
        {
            entity.HasKey(show => show.Id);
            entity.HasIndex(show => show.ProviderId).IsUnique();
            entity.Property(show => show.Title).IsRequired();
            entity.Property(show => show.Status).IsRequired();
            entity.HasMany(show => show.Seasons)
                .WithOne(season => season.Show)
                .HasForeignKey(season => season.ShowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Season>(entity =>
        {
            entity.HasKey(season => season.Id);
            entity.HasIndex(season => season.ProviderSeasonId).IsUnique();
            entity.Property(season => season.UncertaintyReason).HasConversion<string>();
        });

        modelBuilder.Entity<ShowFollow>(entity =>
        {
            entity.HasKey(follow => follow.Id);
            entity.HasIndex(follow => new { follow.UserId, follow.ShowId }).IsUnique();
        });

        modelBuilder.Entity<SeasonCompletionEvent>(entity =>
        {
            entity.HasKey(completion => completion.Id);
            entity.HasIndex(completion => completion.SeasonId).IsUnique();
        });

        modelBuilder.Entity<JobLease>().HasKey(lease => lease.Name);
        modelBuilder.Entity<JobExecution>().HasKey(execution => execution.Id);

        modelBuilder.Entity<DigestDelivery>(entity =>
        {
            entity.HasKey(delivery => delivery.Id);
            entity.HasIndex(delivery => new { delivery.UserId, delivery.Channel, delivery.DigestDate }).IsUnique();
            entity.Property(delivery => delivery.Channel).IsRequired();
            entity.Property(delivery => delivery.Status).IsRequired();
            entity.HasMany(delivery => delivery.Items)
                .WithOne(item => item.DigestDelivery)
                .HasForeignKey(item => item.DigestDeliveryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DigestItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.DigestDeliveryId, item.SeasonCompletionEventId }).IsUnique();
        });

    }

    private void EnsureAuditsAreAppendOnly()
    {
        var auditChanged = ChangeTracker.Entries<RoleChangeAudit>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);
        if (auditChanged)
            throw new InvalidOperationException("Role change audits are append-only.");
    }
}
