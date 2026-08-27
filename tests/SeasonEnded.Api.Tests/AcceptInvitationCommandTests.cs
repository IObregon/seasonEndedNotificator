using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SeasonEnded.Api.Identity;

namespace SeasonEnded.Api.Tests;

public sealed class AcceptInvitationCommandTests
{
    [Fact]
    public async Task Valid_unused_token_activates_account_and_marks_invitation_used()
    {
        await using var context = CreateInMemoryContext();
        var rawToken = await SeedInvitationAsync(context, "newuser@example.test");

        var command = new AcceptInvitationCommand(context);
        var result = await command.ExecuteAsync(rawToken);

        Assert.True(result.Succeeded);
        var invitation = await context.Invitations.SingleAsync();
        Assert.Equal("Accepted", invitation.Status);
        var user = await context.Users.SingleAsync();
        Assert.Equal("newuser@example.test", user.Email);
        Assert.Equal("Active", user.Status);
    }

    [Fact]
    public async Task Used_token_is_rejected()
    {
        await using var context = CreateInMemoryContext();
        var rawToken = await SeedInvitationAsync(context, "newuser@example.test");

        var command = new AcceptInvitationCommand(context);
        await command.ExecuteAsync(rawToken);
        var result = await command.ExecuteAsync(rawToken);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        await using var context = CreateInMemoryContext();
        var rawToken = await SeedInvitationAsync(context, "newuser@example.test", expired: true);

        var command = new AcceptInvitationCommand(context);
        var result = await command.ExecuteAsync(rawToken);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Invalid_token_is_rejected()
    {
        await using var context = CreateInMemoryContext();
        await SeedInvitationAsync(context, "newuser@example.test");

        var command = new AcceptInvitationCommand(context);
        var result = await command.ExecuteAsync("invalid-token");

        Assert.False(result.Succeeded);
    }

    private static async Task<string> SeedInvitationAsync(
        AppDbContext context, string email, bool expired = false)
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        context.Invitations.Add(new Invitation
        {
            Email = email,
            TokenHash = tokenHash,
            Role = UserRole.User,
            Status = "Pending",
            ExpiresAt = expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(24)
        });
        await context.SaveChangesAsync();
        return rawToken;
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
