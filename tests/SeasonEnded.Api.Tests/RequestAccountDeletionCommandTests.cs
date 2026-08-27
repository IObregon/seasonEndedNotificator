using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class RequestAccountDeletionCommandTests
{
    [Fact]
    public async Task Marks_user_pending_and_revokes_ephemeral_credentials()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        var user = new User { Email = "user@example.test", PreferredLanguage = "es" };
        context.Users.AddRange(admin, user);
        context.MagicLinkTokens.Add(new MagicLinkToken
        {
            UserId = user.Id,
            TokenHash = "HASH",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        context.Invitations.Add(new Invitation
        {
            Email = user.Email,
            TokenHash = "INVITE",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await context.SaveChangesAsync();

        var result = await new RequestAccountDeletionCommand(context).ExecuteAsync(user.Id);

        Assert.Equal(RequestAccountDeletionResult.Pending, result);
        Assert.Equal("PendingDeletion", user.Status);
        Assert.Empty(context.MagicLinkTokens);
        Assert.Empty(context.Invitations);
        Assert.NotNull(await context.Users.FindAsync(user.Id));
    }

    [Fact]
    public async Task Last_active_admin_cannot_request_deletion()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var result = await new RequestAccountDeletionCommand(context).ExecuteAsync(admin.Id);

        Assert.Equal(RequestAccountDeletionResult.LastActiveAdmin, result);
        Assert.Equal("Active", admin.Status);
    }

    [Fact]
    public async Task Repeated_request_is_idempotent()
    {
        await using var context = CreateContext();
        var admin = new User { Email = "admin@example.test", Role = UserRole.Admin };
        var user = new User { Email = "user@example.test" };
        context.Users.AddRange(admin, user);
        await context.SaveChangesAsync();
        var command = new RequestAccountDeletionCommand(context);

        await command.ExecuteAsync(user.Id);
        var retry = await command.ExecuteAsync(user.Id);

        Assert.Equal(RequestAccountDeletionResult.AlreadyPending, retry);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
