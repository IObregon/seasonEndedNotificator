using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<RoleChangeAudit> RoleChangeAudits => Set<RoleChangeAudit>();

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

    }

    private void EnsureAuditsAreAppendOnly()
    {
        var auditChanged = ChangeTracker.Entries<RoleChangeAudit>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);
        if (auditChanged)
            throw new InvalidOperationException("Role change audits are append-only.");
    }
}
