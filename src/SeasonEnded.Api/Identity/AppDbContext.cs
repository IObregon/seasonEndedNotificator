using Microsoft.EntityFrameworkCore;

namespace SeasonEnded.Api.Identity;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();

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
    }
}
